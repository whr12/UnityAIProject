using UnityEngine;

namespace AIProject.Core
{
    /// <summary>
    /// MonoBehaviour 单例基类。
    /// 自动处理重复检测和跨场景持久化。
    /// </summary>
    /// <typeparam name="T">子类类型</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isDestroyed;

        /// <summary>
        /// 全局访问入口。如果实例已被销毁，返回 null 并打印警告。
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_isDestroyed)
                {
                    Debug.LogWarning($"[Singleton] {typeof(T).Name} 已被销毁，返回 null。");
                    return null;
                }

                if (_instance == null)
                {
                    Debug.LogError($"[Singleton] {typeof(T).Name} 实例为空 —— 对象未创建或 Awake 未正确执行。");
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[Singleton] {typeof(T).Name} 已存在实例，销毁重复对象 {gameObject.name}。");
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _isDestroyed = true;
                _instance = null;
            }
        }

        /// <summary>
        /// 检查实例是否存在且未被销毁
        /// </summary>
        public static bool Exists => _instance != null && !_isDestroyed;
    }
}
