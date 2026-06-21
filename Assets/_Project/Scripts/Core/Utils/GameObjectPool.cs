using System.Collections.Generic;
using UnityEngine;

namespace AIProject.Core
{
    /// <summary>
    /// 泛型 GameObject 对象池。管理预制体实例的复用，Get 直接返回指定组件。
    ///
    /// 使用方式：
    ///   var pool = new GameObjectPool&lt;Bullet&gt;(bulletPrefab, initialSize: 20, parent: transform);
    ///   var bullet = pool.Get();
    ///   pool.Return(bullet);
    /// </summary>
    public class GameObjectPool<T> where T : Component
    {
        private readonly Queue<T> _pool = new();
        private readonly T _prefab;
        private readonly Transform _parent;

        /// <summary>池中空闲对象数</summary>
        public int FreeCount => _pool.Count;

        /// <param name="prefab">预制体上的组件</param>
        /// <param name="initialSize">预热数量</param>
        /// <param name="parent">池对象的父节点</param>
        public GameObjectPool(T prefab, int initialSize = 0, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < initialSize; i++)
                CreateAndEnqueue();
        }

        /// <summary>取出激活的组件</summary>
        public T Get()
        {
            var obj = _pool.Count > 0 ? _pool.Dequeue() : CreateNew();
            obj.gameObject.SetActive(true);
            return obj;
        }

        /// <summary>归还到池中</summary>
        public void Return(T obj)
        {
            if (obj == null)
                return;

            obj.gameObject.SetActive(false);
            obj.transform.SetParent(_parent);
            _pool.Enqueue(obj);
        }

        /// <summary>预热指定数量</summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
                CreateAndEnqueue();
        }

        private T CreateNew()
        {
            return Object.Instantiate(_prefab, _parent);
        }

        private void CreateAndEnqueue()
        {
            var obj = CreateNew();
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    }
}
