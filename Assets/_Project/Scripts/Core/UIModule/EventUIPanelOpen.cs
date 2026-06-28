namespace AIProject.Core
{
    /// <summary>
    /// UI 面板打开事件。由 UIModule 在面板 OnOpen 后发送。
    /// 其他模块可订阅以响应面板开闭。
    /// </summary>
    public struct EventUIPanelOpen
    {
        /// <summary>面板名称（类名）</summary>
        public string PanelName;

        /// <summary>所在层级</summary>
        public UILayer Layer;
    }
}
