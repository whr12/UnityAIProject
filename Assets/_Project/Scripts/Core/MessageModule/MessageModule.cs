using System;
using System.Collections.Generic;
using UnityEngine;

namespace AIProject.Core
{
    /// <summary>
    /// 消息模块。框架通信底座，所有跨模块通信通过此模块。
    /// 纯 C# 类，不继承 MonoBehaviour。零依赖，第一个被初始化。
    ///
    /// 使用方式：
    ///   MessageModule.Instance.Subscribe&lt;SomeMessage&gt;(OnSomeMessage);
    ///   MessageModule.Instance.Publish(new SomeMessage());
    /// </summary>
    public class MessageModule : GameModule<MessageModule>
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        public override void Initialize()
        {
            _subscribers.Clear();
        }

        public override void PostInitialize()
        {
            // MessageModule 是底层基础设施，不需要连线其他模块
        }

        public override void Release()
        {
            _subscribers.Clear();
        }

        // ===== 核心 API =====

        /// <summary>
        /// 注册事件。
        /// </summary>
        public void AddEvent<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
            {
                _subscribers[type] = new List<Delegate>();
            }

            if (!_subscribers[type].Contains(handler))
            {
                _subscribers[type].Add(handler);
            }
        }

        /// <summary>
        /// 注销事件。
        /// </summary>
        public void RemoveEvent<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    _subscribers.Remove(type);
                }
            }
        }

        /// <summary>
        /// 发送事件。同步调用所有注册的处理器。
        /// </summary>
        public void SendEvent<T>(T message) where T : struct
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list) || list.Count == 0)
            {
                return;
            }

            // 拷贝一份再遍历，防止订阅者在回调中修改列表
            var handlers = new Delegate[list.Count];
            list.CopyTo(handlers);

            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<T>)handler)?.Invoke(message);
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"[Message] {type.Name} 处理器抛出异常: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }
}
