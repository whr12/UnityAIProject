using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIProject.Core
{
    public class SceneModule : GameModule<SceneModule>
    {
        private const string BridgeScene = "EmptyTransition";

        private string _currentScene;
        private ISceneTransition _defaultTransition = new InstantTransition();

        public override void Initialize() { }

        public override void PostInitialize()
        {
            _currentScene = SceneManager.GetActiveScene().name;
        }

        public override void Release() { }

        // ===== 切换场景 =====

        /// <summary>
        /// 切换场景。通过 EmptyTransition 桥场景避免新老场景同时占用内存。
        /// </summary>
        /// <param name="sceneName">目标场景名</param>
        /// <param name="transition">过渡效果，null 则无过渡</param>
        public void LoadScene(string sceneName, ISceneTransition transition = null)
        {
            CoroutineModule.Instance.StartCoroutine(
                LoadSceneRoutine(sceneName, transition ?? _defaultTransition));
        }

        private IEnumerator LoadSceneRoutine(string sceneName, ISceneTransition transition)
        {
            var oldScene = _currentScene;

            MessageModule.Instance.SendEvent(new SceneLoadStartEvent { SceneName = sceneName });

            // Step 1: 加载桥场景。过渡期间的动画等临时资源放入此场景，
            //         后续卸载桥场景时 Unity 自动清理，无需 ResourceModule 追踪。
            yield return SceneManager.LoadSceneAsync(BridgeScene, LoadSceneMode.Additive);
            var bridgeScene = SceneManager.GetSceneByName(BridgeScene);
            SceneManager.SetActiveScene(bridgeScene);
            ResourceModule.Instance.MarkScene(BridgeScene);

            // Step 2: 卸载旧场景。桥兜底，不会 0 场景。
            if (!string.IsNullOrEmpty(oldScene))
            {
                var unloadOp = SceneManager.UnloadSceneAsync(oldScene);
                if (unloadOp != null)
                {
                    while (!unloadOp.isDone)
                        yield return null;
                }
            }

            // Step 3: 过渡入。此时只有桥场景，过渡资源可放入。
            yield return transition.OnBeforeSwitch();

            // Step 4: 清理旧场景资源
            ResourceModule.Instance.FlushCache();
            if (!string.IsNullOrEmpty(oldScene))
                ResourceModule.Instance.ForceReleaseScene(oldScene);

            // Step 5: 加载新场景
            ResourceModule.Instance.MarkScene(sceneName);

            var loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (loadOp == null)
            {
                Debug.LogError($"[Scene] 加载场景失败: {sceneName}");
                yield break;
            }

            while (!loadOp.isDone)
            {
                transition.OnProgress(loadOp.progress);
                MessageModule.Instance.SendEvent(new SceneLoadProgressEvent
                {
                    SceneName = sceneName,
                    Progress = loadOp.progress
                });
                yield return null;
            }

            var newScene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(newScene);

            // Step 6: 过渡出
            transition.OnProgress(1f);
            yield return transition.OnAfterSwitch();

            // Step 7: 清理并卸载桥场景。过渡期间的临时资源统一释放。
            ResourceModule.Instance.ForceReleaseScene(BridgeScene);
            yield return SceneManager.UnloadSceneAsync(BridgeScene);

            _currentScene = sceneName;

            ResourceModule.Instance.FlushCache();
            MessageModule.Instance.SendEvent(new SceneLoadCompleteEvent { SceneName = sceneName });
        }

        // ===== 叠加场景 =====

        public void LoadSceneAdditive(string sceneName)
        {
            CoroutineModule.Instance.StartCoroutine(LoadSceneAdditiveRoutine(sceneName));
        }

        private IEnumerator LoadSceneAdditiveRoutine(string sceneName)
        {
            ResourceModule.Instance.MarkScene(sceneName);

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[Scene] 叠加加载失败: {sceneName}");
                yield break;
            }

            while (!op.isDone)
                yield return null;
        }

        // ===== 卸载场景 =====

        public void UnloadScene(string sceneName)
        {
            CoroutineModule.Instance.StartCoroutine(UnloadSceneRoutine(sceneName));
        }

        private IEnumerator UnloadSceneRoutine(string sceneName)
        {
            ResourceModule.Instance.ForceReleaseScene(sceneName);

            var op = SceneManager.UnloadSceneAsync(sceneName);
            if (op != null)
            {
                while (!op.isDone)
                    yield return null;
            }
        }
    }
}
