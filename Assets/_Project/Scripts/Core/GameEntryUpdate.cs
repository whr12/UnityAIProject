using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AIProject.Core
{
    /// <summary>
    /// BootstrapScene 更新脚本。模拟资源下载更新，通过 Slider 显示进度。
    /// 更新完成后自动加载下一场景。
    /// </summary>
    public class GameEntryUpdate : MonoBehaviour
    {
        [SerializeField] private Slider _progressBar;
        private const string NextSceneName = "GameEntryScene";

        // 模拟：需要下载的资源包列表（当前为空，未来填入真实列表）
        private static readonly string[] PendingBundles = { };

        private IEnumerator Start()
        {
            yield return null; // 等 GameManager

            if (PendingBundles.Length == 0)
            {
                // 无更新
                _progressBar.value = 0f;
                yield return new WaitForSeconds(0.1f);
                _progressBar.value = 1f;
            }
            else
            {
                // 模拟下载
                var total = PendingBundles.Length;
                for (int i = 0; i < total; i++)
                {
                    var elapsed = 0f;
                    while (elapsed < 1f / total)
                    {
                        elapsed += Time.deltaTime;
                        _progressBar.value = (i + elapsed * total / 1f) / total;
                        yield return null;
                    }
                }
                _progressBar.value = 1f;
            }

            SceneModule.Instance.LoadScene(NextSceneName);
        }
    }
}
