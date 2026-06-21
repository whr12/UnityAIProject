using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AIProject.Core
{
    public class ResourcesAssetProvider : IAssetProvider
    {
        public T Load<T>(string path) where T : Object
        {
            var asset = Resources.Load<T>(path);
            if (asset == null)
            {
                Debug.LogError($"[Resources] 加载失败: {path}");
            }
            return asset;
        }

        public IEnumerator LoadAsyncRoutine<T>(string path, Action<T> onComplete) where T : Object
        {
            var request = Resources.LoadAsync<T>(path);
            yield return request;

            if (request.asset == null)
            {
                Debug.LogError($"[Resources] 异步加载失败: {path}");
                onComplete?.Invoke(null);
                yield break;
            }

            onComplete?.Invoke(request.asset as T);
        }

        public void Unload(Object asset)
        {
            if (asset != null)
                Resources.UnloadAsset(asset);
        }

        public string GetScenePath(string sceneName)
        {
            return sceneName;
        }
    }
}
