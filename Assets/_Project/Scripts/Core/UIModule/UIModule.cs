using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AIProject.Core
{
    /// <summary>
    /// UI 核心管理器。负责 Canvas 创建、层级管理、面板开闭、栈操作。
    /// 纯 C# 模块，继承 GameModule&lt;UIModule&gt;。
    ///
    /// Prefab 路径由 Panel.PrefabPath 决定（子类必须 override）。
    /// </summary>
    public class UIModule : GameModule<UIModule>, IUpdatable
    {
        private Canvas _rootCanvas;
        private readonly Dictionary<UILayer, Transform> _layers = new();
        private readonly List<UIStackEntry> _stack = new();
        private readonly Dictionary<Type, int> _panelCount = new();

        public Canvas RootCanvas => _rootCanvas;

        // ===== Lifecycle =====

        public override void Initialize()
        {
            CreateCanvas();
            CreateLayers();
            _stack.Clear();
            _panelCount.Clear();
        }

        public override void PostInitialize() { }

        public override void Release()
        {
            // 逆序关闭所有面板
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                var panel = _stack[i].Panel;
                panel?.Close();
                if (panel?.View != null)
                    Object.Destroy(panel.View.GameObject);
            }

            _stack.Clear();
            _panelCount.Clear();

            if (_rootCanvas != null)
                Object.Destroy(_rootCanvas.gameObject);
        }

        // ===== Show =====

        /// <summary>
        /// 打开面板。
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        /// <param name="layer">UI 层级，默认 Default</param>
        /// <param name="data">外部数据，传入 OnSetup</param>
        /// <returns>面板实例，加载失败返回 null</returns>
        public T Show<T>(UILayer layer = UILayer.Default, object data = null) where T : UIPanel, new()
        {
            // 已打开则返回已有实例，刷新 data
            var existing = GetPanel<T>();
            if (existing != null)
            {
                existing.Setup(data);
                return existing;
            }

            // 加载 Prefab
            var panel = new T();
            var path = panel.PrefabPath;

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"[UIModule] {typeof(T).Name}.PrefabPath 为空");
                return null;
            }

            var handle = ResourceModule.Instance.Load<GameObject>(path);
            if (handle == null)
            {
                Debug.LogError($"[UIModule] 未找到 Prefab: {path}");
                return null;
            }

            var go = (GameObject)Object.Instantiate(handle.Asset, _layers[layer]);
            // 拉伸至全屏
            StretchRect(go.GetComponent<RectTransform>());
            ResourceModule.Instance.BindTo(handle, go);

            // 获取 UIBinding 组件
            var binding = go.GetComponent<UIBinding>();
            if (binding == null)
            {
                Debug.LogError($"[UIModule] {path} 根节点缺少 UIBinding 组件");
                Object.Destroy(go);
                return null;
            }

            // 创建 View（Panel 可重写 CreateView 返回派生类）并绑定
            var view = panel.CreateView(binding);
            panel.Bind(view);
            panel.Setup(data);
            panel.Open(layer);

            // 追踪
            _stack.Add(new UIStackEntry { Panel = panel, Layer = layer, PanelName = typeof(T).Name });
            TrackPanel(typeof(T));

            // 事件
            MessageModule.Instance.SendEvent(new EventUIPanelOpen
            {
                PanelName = typeof(T).Name,
                Layer = layer
            });

            // Modal 层阻断下层交互
            if (layer == UILayer.Modal)
                SetLowerLayersInteractable(false);

            return panel;
        }

        // ===== Hide =====

        /// <summary>关闭栈顶面板。</summary>
        public void Hide()
        {
            if (_stack.Count == 0) return;
            CloseEntryAt(_stack.Count - 1);
        }

        /// <summary>关闭指定面板。</summary>
        public void Hide(UIPanel panel)
        {
            if (panel == null || !panel.IsOpen) return;

            var index = _stack.FindIndex(e => e.Panel == panel);
            if (index < 0) return;

            CloseEntryAt(index);
        }

        /// <summary>关闭指定层所有面板。</summary>
        public void HideAll(UILayer layer)
        {
            // 逆序遍历，避免索引错乱
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                if (_stack[i].Layer == layer)
                    CloseEntryAt(i);
            }
        }

        // ===== Query =====

        /// <summary>获取已打开的面板实例。</summary>
        public T GetPanel<T>() where T : UIPanel
        {
            foreach (var entry in _stack)
            {
                if (entry.Panel is T result)
                    return result;
            }
            return null;
        }

        /// <summary>栈中面板数量。</summary>
        public int StackCount => _stack.Count;

        // ===== IUpdatable =====

        public void OnUpdate(float deltaTime)
        {
            // TODO: ESC 键检测等 Input System 阶段再启用
        }

        // ===== Internal =====

        private void CloseEntryAt(int index)
        {
            var entry = _stack[index];
            var panel = entry.Panel;

            panel.Close();

            MessageModule.Instance.SendEvent(new EventUIPanelClose
            {
                PanelName = entry.PanelName,
                Layer = entry.Layer
            });

            // 销毁 GameObject（TODO: 后续改为缓存池）
            if (panel.View != null)
                Object.Destroy(panel.View.GameObject);

            _stack.RemoveAt(index);
            UntrackPanel(panel.GetType());

            // Modal 全部关闭后恢复下层交互
            if (entry.Layer == UILayer.Modal && CountLayer(UILayer.Modal) == 0)
                SetLowerLayersInteractable(true);
        }

        private void CreateCanvas()
        {
            var root = new GameObject("[UIRoot]");
            Object.DontDestroyOnLoad(root);

            _rootCanvas = root.AddComponent<Canvas>();
            _rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            StretchRect(root.GetComponent<RectTransform>());

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            // 确保场景中有 EventSystem
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                Object.DontDestroyOnLoad(es);
            }
        }

        private void CreateLayers()
        {
            // 按枚举顺序创建，Sorting Order 递增
            int orderBase = 0;
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var go = new GameObject(layer.ToString());
                go.transform.SetParent(_rootCanvas.transform, false);

                var canvas = go.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = orderBase;
                orderBase += 10;

                go.AddComponent<GraphicRaycaster>();
                go.AddComponent<CanvasGroup>();

                // AddComponent<Canvas> 会自动添加 RectTransform，放最后拉伸
                StretchRect(go.GetComponent<RectTransform>());

                _layers[layer] = go.transform;
            }
        }

        private void SetLowerLayersInteractable(bool interactable)
        {
            foreach (var kv in _layers)
            {
                if (kv.Key >= UILayer.Modal) continue;
                var cg = kv.Value.GetComponent<CanvasGroup>();
                if (cg != null)
                    cg.interactable = interactable;
            }
        }

        private static void StretchRect(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void TrackPanel(Type type)
        {
            _panelCount.TryGetValue(type, out int count);
            _panelCount[type] = count + 1;
        }

        private void UntrackPanel(Type type)
        {
            if (_panelCount.TryGetValue(type, out int count) && count > 0)
                _panelCount[type] = count - 1;
        }

        private int CountLayer(UILayer layer)
        {
            int count = 0;
            foreach (var entry in _stack)
            {
                if (entry.Layer == layer) count++;
            }
            return count;
        }
    }
}
