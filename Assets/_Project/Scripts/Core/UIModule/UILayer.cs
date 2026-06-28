namespace AIProject.Core
{
    /// <summary>
    /// UI 层级。数值越大渲染越靠前，层级间独立管理。
    /// </summary>
    public enum UILayer
    {
        /// <summary>常驻 HUD：血条、小地图、弹药、任务指引</summary>
        HUD = 0,

        /// <summary>默认界面：主菜单、设置、背包、技能树</summary>
        Default = 1,

        /// <summary>模态对话框：确认框、提示框、输入弹窗</summary>
        Modal = 2,

        /// <summary>引导层：新手教程高亮遮罩、全屏提示</summary>
        Tutorial = 3,
    }
}
