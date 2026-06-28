using System.Collections;
using AIProject.Core;
using AIProject.UI;
using UnityEngine;

namespace AIProject.Test
{
    /// <summary>
    /// 启动测试：场景加载后自动打开 GameEntryPanel。
    /// 挂载到 Startup 场景的任意 GameObject 上。
    /// </summary>
    public class StartupTest : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // 等待一帧，确保 GameManager 已初始化所有模块
            yield return null;
            UIModule.Instance.Show<UIGameEntryPanel>();
        }
    }
}
