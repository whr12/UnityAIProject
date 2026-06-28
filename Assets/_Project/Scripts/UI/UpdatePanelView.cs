using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AIProject.Core;

namespace AIProject.UI
{
    public class UpdatePanelView : UIView
    {
        public Slider ProgressBar { get; }
        public TMP_Text StatusText { get; }

        public UpdatePanelView(UIBinding binding) : base(binding)
        {
            ProgressBar = binding.Get<Slider>("ProgressBar");
            StatusText = binding.Get<TMP_Text>("StatusText");
        }
    }
}
