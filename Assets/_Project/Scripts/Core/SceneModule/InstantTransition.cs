using System.Collections;

namespace AIProject.Core
{
    /// <summary>
    /// 无过渡。切换立即生效。
    /// </summary>
    public class InstantTransition : ISceneTransition
    {
        public IEnumerator OnBeforeSwitch()
        {
            yield break;
        }

        public void OnProgress(float progress) { }

        public IEnumerator OnAfterSwitch()
        {
            yield break;
        }
    }
}
