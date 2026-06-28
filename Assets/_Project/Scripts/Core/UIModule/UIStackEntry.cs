namespace AIProject.Core
{
    /// <summary>
    /// UI 栈条目。UIModule 内部使用，记录栈中每个面板的信息。
    /// </summary>
    internal struct UIStackEntry
    {
        /// <summary>面板实例</summary>
        public UIPanel Panel;

        /// <summary>所在层级</summary>
        public UILayer Layer;

        /// <summary>面板名称（用于日志/调试）</summary>
        public string PanelName;
    }
}
