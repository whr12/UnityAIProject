using AIProject.Core;

namespace AIProject.UI
{
    public class UIGameEntryPanel : UIPanel
    {
        public override string PrefabPath => "UI/GameEntryPanel";

        protected internal override UIView CreateView(UIBinding binding)
            => new UIGameEntryPanelView(binding);

        protected override void OnOpen()
        {
            var view = (UIGameEntryPanelView)View;
            view.Confirm.onClick.AddListener(OnConfirmClick);
        }

        protected override void OnClose()
        {
            var view = (UIGameEntryPanelView)View;
            view.Confirm.onClick.RemoveListener(OnConfirmClick);
        }

        private void OnConfirmClick()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}