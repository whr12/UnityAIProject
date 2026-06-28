using UnityEngine;

namespace AIProject.Core
{
    /// <summary>
    /// UI View 基类（纯 C#）。桥接 UIBinding，将字符串查询转为类型化属性。
    ///
    /// 每个 Prefab 对应一个 UIView 派生类，派生类在构造时从 UIBinding 中提取
    /// 所有组件并缓存为属性，外界直接通过属性访问，无需字符串查询。
    ///
    /// 用法（派生类）：
    ///   public class UIMainMenuPanelView : UIView
    ///   {
    ///       public Button BtnStart { get; }
    ///       public UIMainMenuPanelView(UIBinding binding) : base(binding)
    ///       {
    ///           BtnStart = binding.Get&lt;Button&gt;("Btn_Start");
    ///       }
    ///   }
    /// </summary>
    public class UIView
    {
        /// <summary>UIBinding 所在的 GameObject（即 Prefab 根节点）</summary>
        public GameObject GameObject => Binding != null ? Binding.gameObject : null;

        /// <summary>UI 绑定组件（构造时传入，不可修改）</summary>
        public UIBinding Binding { get; }

        public UIView(UIBinding binding)
        {
            Binding = binding;
        }
    }
}
