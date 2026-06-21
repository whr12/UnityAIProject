using System.Collections;
using UnityEngine;

namespace AIProject.Core
{
    /// <summary>
    /// 协程模块。通过内部 GameObject 借 Unity 原生协程能力，
    /// 使纯 C# 模块无需继承 MonoBehaviour 即可使用协程。
    ///
    /// 使用方式：
    ///   CoroutineModule.Instance.StartCoroutine(SomeRoutine());
    /// </summary>
    public class CoroutineModule : GameModule<CoroutineModule>
    {
        private GameObject _hostObject;
        private MonoBehaviour _host;

        public override void Initialize()
        {
            _hostObject = new GameObject("[CoroutineHost]");
            _hostObject.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(_hostObject);
            _host = _hostObject.AddComponent<CoroutineHost>();
        }

        public override void PostInitialize()
        {
            // 协程模块是底层基础设施，不需要连线其他模块
        }

        public override void Release()
        {
            if (_host != null)
                _host.StopAllCoroutines();

            if (_hostObject != null)
                Object.Destroy(_hostObject);

            _host = null;
            _hostObject = null;
        }

        /// <summary>
        /// 启动协程。
        /// </summary>
        public Coroutine StartCoroutine(IEnumerator routine)
        {
            return _host.StartCoroutine(routine);
        }

        /// <summary>
        /// 停止指定协程。
        /// </summary>
        public void StopCoroutine(Coroutine coroutine)
        {
            if (coroutine != null)
                _host.StopCoroutine(coroutine);
        }

        /// <summary>
        /// 停止所有协程。
        /// </summary>
        public void StopAllCoroutines()
        {
            _host.StopAllCoroutines();
        }

        /// <summary>
        /// 内部宿主，只用于挂载协程，不包含任何逻辑。
        /// </summary>
        private class CoroutineHost : MonoBehaviour { }
    }
}
