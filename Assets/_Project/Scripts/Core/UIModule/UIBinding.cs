using System;
using System.Collections.Generic;
using UnityEngine;

namespace AIProject.Core
{
    [AddComponentMenu("UI/UI Binding")]
    public class UIBinding : MonoBehaviour
    {
        [SerializeField] public List<Entry> _entries = new();
        [SerializeField] public string _viewScriptPath;

        public T Get<T>(string name) where T : Component
        {
            foreach (var e in _entries)
            {
                if (e.Name != name) continue;
                if (e.Components == null || e.Components.Count == 0) return null;
                return e.Components[0] as T;
            }
            return null;
        }

        public T[] GetAll<T>(string name) where T : Component
        {
            foreach (var e in _entries)
            {
                if (e.Name != name) continue;
                if (e.Components == null) return Array.Empty<T>();
                var r = new List<T>();
                foreach (var c in e.Components) if (c is T t) r.Add(t);
                return r.ToArray();
            }
            return Array.Empty<T>();
        }

        public bool Has(string name)
        {
            foreach (var e in _entries)
                if (e.Name == name) return true;
            return false;
        }

        [Serializable]
        public class Entry
        {
            public string Name;
            public List<Component> Components = new();
        }
    }
}
