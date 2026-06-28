namespace AIProject.Core
{
    /// <summary>
    /// UI 面板抽象基类（纯 C#，非 MonoBehaviour）。
    /// 负责面板逻辑和生命周期，由 UIModule 管理。
    /// 关联的 UI 组件通过 View 属性访问。
    ///
    /// 生命周期：
    ///   Instantiate Prefab → 获取 UIView → 创建/获取 Panel → Panel.Bind(view)
    ///   → OnSetup(data) → Open(layer) → OnOpen()
    ///
    /// 使用方式：
    ///   UIModule.Instance.Show&lt;MyPanel&gt;();
    ///   UIModule.Instance.Show&lt;MyPanel&gt;(data: someData);
    /// </summary>
    public abstract class UIPanel
    {
        /// <summary>关联的 UI View（MonoBehaviour，挂在 Prefab 根节点上）</summary>
        public UIView View { get; private set; }

        /// <summary>所在 UI 层级</summary>
        public UILayer Layer { get; private set; }

        /// <summary>是否处于打开状态</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// 关联的 Prefab 路径（Resources 加载路径，不含扩展名）。
        /// 子类必须 override，未实现则编译报错。
        /// 示例: "UI/MainMenuPanel"
        /// </summary>
        public abstract string PrefabPath { get; }

        // ===== 内部：由 UIModule 调用 =====

        /// <summary>
        /// 绑定 View。UIModule 在 Instantiate 后调用。
        /// </summary>
        internal void Bind(UIView view)
        {
            View = view;
        }

        /// <summary>
        /// 接收外部传入的数据。在 OnOpen 之前调用。
        /// </summary>
        internal void Setup(object data)
        {
            OnSetup(data);
        }

        internal void Open(UILayer layer)
        {
            Layer = layer;
            IsOpen = true;
            OnOpen();
        }

        internal void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            OnClose();
        }

        // ===== 子类重写 =====

        /// <summary>
        /// 创建关联的 View（UIModule 调用）。子类可重写以创建派生 UIView，在其中提前缓存 UIBinding 中的组件。
        /// 默认返回普通 UIView 实例。
        /// </summary>
        protected internal virtual UIView CreateView(UIBinding binding) => new UIView(binding);

        /// <summary>接收外部数据。data 为 null 表示无参数。</summary>
        protected virtual void OnSetup(object data) { }

        /// <summary>面板打开。此时 View 已绑定、data 已接收，可安全访问 UI 组件。</summary>
        protected virtual void OnOpen() { }

        /// <summary>面板关闭。清理订阅、释放资源。</summary>
        protected virtual void OnClose() { }

        /// <summary>用户按 ESC 时回调（UIModule 调用）。默认关闭自身。</summary>
        internal virtual void OnEscape()
        {
            UIModule.Instance.Hide(this);
        }
    }
}
