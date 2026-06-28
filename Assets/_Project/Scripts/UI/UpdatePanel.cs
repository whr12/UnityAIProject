using System.Collections;
using AIProject.Core;
using UnityEngine;

namespace AIProject.UI
{
    public class UIUpdatePanel : UIPanel
    {
        public override string PrefabPath => "UI/UpdatePanel";

        protected internal override UIView CreateView(UIBinding binding)
            => new UpdatePanelView(binding);

        protected override void OnOpen()
        {
            CoroutineModule.Instance.StartCoroutine(FakeUpdate());
        }

        protected override void OnClose()
        {
        }

        private IEnumerator FakeUpdate()
        {
            var view = (UpdatePanelView)View;
            var elapsed = 0f;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                view.ProgressBar.value = elapsed;

                if (elapsed < 0.4f)
                    view.StatusText.text = "检查更新...";
                else if (elapsed < 0.8f)
                    view.StatusText.text = "下载中...";
                else
                    view.StatusText.text = "完成";

                yield return null;
            }

            view.ProgressBar.value = 1f;
            view.StatusText.text = "完成";
            UIModule.Instance.Hide(this);
        }
    }
}
