using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AIProject.Core
{
    public class ResourceModule : GameModule<ResourceModule>
    {
        private IAssetProvider _provider;

        // 路径 → Handle（所有已加载资源）
        private readonly Dictionary<string, ResourceHandle> _loaded = new();

        // 场景 → 路径列表
        private readonly Dictionary<string, List<string>> _sceneAssets = new();
        private string _currentScene;

        // 缓存列表
        private readonly HashSet<ResourceHandle> _cachedHandles = new();

        // InstanceID → Handle 列表
        private readonly Dictionary<int, List<ResourceHandle>> _bindings = new();

        // Handle → 绑定的 token 列表
        private readonly Dictionary<ResourceHandle, List<int>> _handleToTokens = new();

        public override void Initialize()
        {
            _provider = new ResourcesAssetProvider();
            _loaded.Clear();
            _sceneAssets.Clear();
            _cachedHandles.Clear();
            _bindings.Clear();
            _handleToTokens.Clear();
        }

        public override void PostInitialize() { }

        public override void Release()
        {
            // 全部清理
            foreach (var sceneName in new List<string>(_sceneAssets.Keys))
                ForceReleaseScene(sceneName);
            FlushCache();

            _loaded.Clear();
        }

        // ===== 加载 =====

        /// <summary>
        /// 加载资源。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="sceneBound">是否绑定到当前场景。true：受 ForceReleaseScene 管理；false：独立资源，仅靠引用计数</param>
        public ResourceHandle Load<T>(string path, bool sceneBound = true) where T : Object
        {
            if (_loaded.TryGetValue(path, out var existing))
            {
                existing.AddRef();
                return existing;
            }

            var asset = _provider.Load<T>(path);
            if (asset == null) return null;

            var handle = new ResourceHandle(path, asset, this);
            _loaded[path] = handle;
            if (sceneBound) TrackAsset(path);
            return handle;
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="onComplete">完成回调</param>
        /// <param name="sceneBound">是否绑定到当前场景</param>
        public void LoadAsync<T>(string path, Action<ResourceHandle> onComplete, bool sceneBound = true) where T : Object
        {
            if (_loaded.TryGetValue(path, out var existing))
            {
                existing.AddRef();
                onComplete?.Invoke(existing);
                return;
            }

            CoroutineModule.Instance.StartCoroutine(LoadAsyncRoutine<T>(path, onComplete, sceneBound));
        }

        private IEnumerator LoadAsyncRoutine<T>(string path, Action<ResourceHandle> onComplete, bool sceneBound) where T : Object
        {
            T asset = null;
            bool done = false;

            var inner = _provider.LoadAsyncRoutine<T>(path, result =>
            {
                asset = result;
                done = true;
            });
            CoroutineModule.Instance.StartCoroutine(inner);

            while (!done)
                yield return null;

            if (asset == null)
            {
                onComplete?.Invoke(null);
                yield break;
            }

            // 双重检查：等待期间可能已被同步加载
            if (_loaded.TryGetValue(path, out var existing))
            {
                existing.AddRef();
                onComplete?.Invoke(existing);
                yield break;
            }

            var handle = new ResourceHandle(path, asset, this);
            _loaded[path] = handle;
            if (sceneBound) TrackAsset(path);
            onComplete?.Invoke(handle);
        }

        // ===== 卸载 =====

        internal void UnloadHandle(ResourceHandle handle)
        {
            if (handle.RefCount > 0) return;

            _cachedHandles.Remove(handle);
            _provider.Unload(handle.Asset);
            _loaded.Remove(handle.Path);

            // 从场景追踪中移除
            foreach (var list in _sceneAssets.Values)
                list.Remove(handle.Path);

            // 清理绑定
            if (_handleToTokens.TryGetValue(handle, out var tokens))
            {
                foreach (var token in tokens)
                {
                    if (_bindings.TryGetValue(token, out var handles))
                        handles.Remove(handle);
                }
                _handleToTokens.Remove(handle);
            }
        }

        // ===== 绑定 =====

        public void BindTo(ResourceHandle handle, GameObject owner)
        {
            if (handle == null || owner == null) return;

            var tracker = owner.GetComponent<ResourceTracker>();
            if (tracker == null)
                tracker = owner.AddComponent<ResourceTracker>();

            var token = owner.GetInstanceID();

            if (!_bindings.ContainsKey(token))
                _bindings[token] = new List<ResourceHandle>();

            _bindings[token].Add(handle);

            if (!_handleToTokens.ContainsKey(handle))
                _handleToTokens[handle] = new List<int>();

            _handleToTokens[handle].Add(token);
        }

        internal void ReleaseByToken(int token)
        {
            if (!_bindings.TryGetValue(token, out var handles)) return;

            // 每个 handle Release 一次
            foreach (var h in handles)
                h.Release();

            _bindings.Remove(token);
        }

        // ===== 缓存 =====

        public void Cache(ResourceHandle handle)
        {
            if (handle == null) return;
            handle.MarkCached();
            _cachedHandles.Add(handle);
        }

        public void FlushCache()
        {
            var toUnload = new List<ResourceHandle>(_cachedHandles);
            _cachedHandles.Clear();

            foreach (var h in toUnload)
                UnloadHandle(h);

            Debug.Log($"[Resource] FlushCache: 卸载 {toUnload.Count} 个缓存资源");
        }

        // ===== 场景 =====

        public void MarkScene(string sceneName)
        {
            _currentScene = sceneName;
            if (!_sceneAssets.ContainsKey(sceneName))
                _sceneAssets[sceneName] = new List<string>();
        }

        public void ForceReleaseScene(string sceneName)
        {
            if (!_sceneAssets.TryGetValue(sceneName, out var paths)) return;

            foreach (var path in paths)
            {
                if (_loaded.TryGetValue(path, out var handle))
                    handle.ForceUnload();
            }

            _sceneAssets.Remove(sceneName);
            Debug.Log($"[Resource] ForceReleaseScene: {sceneName}, {paths.Count} 个资源");
        }

        // ===== 内部 =====

        private void TrackAsset(string path)
        {
            var bucket = string.IsNullOrEmpty(_currentScene) ? "_global" : _currentScene;
            if (!_sceneAssets.ContainsKey(bucket))
                _sceneAssets[bucket] = new List<string>();
            if (!_sceneAssets[bucket].Contains(path))
                _sceneAssets[bucket].Add(path);
        }

        /// <summary>
        /// 内部组件：OnDestroy 时通知 ResourceModule 释放该 GameObject 绑定的所有 Handle。
        /// Instantiate 副本有新的 InstanceID，和原件互不干扰。
        /// </summary>
        private class ResourceTracker : MonoBehaviour
        {
            private void OnDestroy()
            {
                ResourceModule.Instance?.ReleaseByToken(gameObject.GetInstanceID());
            }
        }
    }
}
