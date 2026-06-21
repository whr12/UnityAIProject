using UnityEngine;
using Object = UnityEngine.Object;

namespace AIProject.Core
{
    /// <summary>
    /// 资源所有权凭证。Load 返回此对象，通过 AddRef/Release 管理生命周期。
    /// 不要直接持有 Asset 引用太久 —— 优先通过 Handle 访问。
    /// </summary>
    public class ResourceHandle
    {
        public string Path { get; }
        public Object Asset { get; }
        public int RefCount => _refCount;

        private int _refCount;
        private bool _isCached;
        private ResourceModule _owner;

        internal ResourceHandle(string path, Object asset, ResourceModule owner)
        {
            Path = path;
            Asset = asset;
            _refCount = 1;    // Load 时默认引用 +1
            _owner = owner;
        }

        /// <summary>引用 +1</summary>
        public void AddRef()
        {
            _refCount++;
        }

        /// <summary>
        /// 引用 -1。归零时：若缓存则保留，否则通过 owner 卸载。
        /// </summary>
        public void Release()
        {
            _refCount--;
            if (_refCount <= 0)
            {
                _refCount = 0;
                if (!_isCached)
                {
                    _owner.UnloadHandle(this);
                }
            }
        }

        /// <summary>无视引用计数，立即卸载</summary>
        public void ForceUnload()
        {
            _refCount = 0;
            _isCached = false;
            _owner.UnloadHandle(this);
        }

        /// <summary>标记为缓存。引用归零后保留，FlushCache 时才卸载。</summary>
        internal void MarkCached()
        {
            _isCached = true;
        }

        internal bool IsCached => _isCached;

        /// <summary>
        /// 创建 GameObject 实例并自动绑定此资源的生命周期。
        /// GameObject 被 Destroy 时自动 Release。
        /// </summary>
        public GameObject CreateGameObject(Vector3 position, Quaternion rotation)
        {
            var prefab = Asset as GameObject;
            if (prefab == null)
            {
                Debug.LogError($"[ResourceHandle] {Path} 不是 GameObject，无法 Instantiate");
                return null;
            }

            var go = Object.Instantiate(prefab, position, rotation);
            _owner.BindTo(this, go);
            return go;
        }

        /// <summary>创建 GameObject 实例，使用默认位置和旋转</summary>
        public GameObject CreateGameObject()
        {
            return CreateGameObject(Vector3.zero, Quaternion.identity);
        }
    }
}
