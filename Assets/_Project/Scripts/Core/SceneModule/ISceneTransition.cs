using System.Collections;

namespace AIProject.Core
{
    /// <summary>
    /// 场景过渡接口。SceneModule 通过此接口协调过渡动画和进度显示。
    ///
    /// 流程：
    ///   OnBeforeSwitch → [场景加载中，OnProgress 被反复调用] → OnAfterSwitch
    /// </summary>
    public interface ISceneTransition
    {
        /// <summary>入过渡动画。完成后画面应被遮住。</summary>
        IEnumerator OnBeforeSwitch();

        /// <summary>场景加载进度更新。progress 范围 0~1。</summary>
        void OnProgress(float progress);

        /// <summary>出过渡动画。完成后新场景露出。</summary>
        IEnumerator OnAfterSwitch();
    }
}
