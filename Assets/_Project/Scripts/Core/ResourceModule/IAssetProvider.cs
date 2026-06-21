using System;
using System.Collections;
using Object = UnityEngine.Object;

namespace AIProject.Core
{
    public interface IAssetProvider
    {
        T Load<T>(string path) where T : Object;
        IEnumerator LoadAsyncRoutine<T>(string path, Action<T> onComplete) where T : Object;
        void Unload(Object asset);
        string GetScenePath(string sceneName);
    }
}
