namespace AIProject.Core
{
    /// <summary>
    /// UI 面板关闭事件。由 UIModule 在面板 OnClose 后发送。
    /// </summary>
    public struct EventUIPanelClose
    {
        /// <summary>面板名称（类名）</summary>
        public string PanelName;

        /// <summary>所在层级</summary>
        public UILayer Layer;
    }
}
