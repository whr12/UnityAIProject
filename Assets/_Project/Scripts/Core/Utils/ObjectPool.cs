using System;
using System.Collections.Generic;

namespace AIProject.Core
{
    /// <summary>
    /// 纯 C# 通用对象池。不依赖 Unity API，可独立测试。
    ///
    /// 使用方式：
    ///   var pool = new ObjectPool&lt;MyClass&gt;(initialSize: 10, onGet: obj => obj.Reset());
    ///   var obj = pool.Get();
    ///   pool.Return(obj);
    /// </summary>
    public class ObjectPool<T> where T : class, new()
    {
        private readonly Queue<T> _pool = new();
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;

        /// <summary>池中空闲对象数</summary>
        public int FreeCount => _pool.Count;

        /// <summary>池创建过的对象总数（包括已取出的）</summary>
        public int TotalCount { get; private set; }

        /// <param name="initialSize">预热数量</param>
        /// <param name="onGet">取出时回调（如重置状态）</param>
        /// <param name="onReturn">归还时回调（如清理引用）</param>
        public ObjectPool(int initialSize = 0, Action<T> onGet = null, Action<T> onReturn = null)
        {
            _onGet = onGet;
            _onReturn = onReturn;

            for (int i = 0; i < initialSize; i++)
            {
                var obj = new T();
                _onReturn?.Invoke(obj);
                _pool.Enqueue(obj);
            }

            TotalCount = initialSize;
        }

        /// <summary>从池中取出一个对象</summary>
        public T Get()
        {
            T obj;
            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else
            {
                obj = new T();
                TotalCount++;
            }

            _onGet?.Invoke(obj);
            return obj;
        }

        /// <summary>归还对象到池中</summary>
        public void Return(T obj)
        {
            if (obj == null)
                return;

            _onReturn?.Invoke(obj);
            _pool.Enqueue(obj);
        }

        /// <summary>预热指定数量</summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = new T();
                _onReturn?.Invoke(obj);
                _pool.Enqueue(obj);
                TotalCount++;
            }
        }
    }
}
