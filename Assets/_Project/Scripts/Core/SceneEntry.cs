using System;
using System.Collections;
using UnityEngine;

namespace AIProject.Core
{
    /// <summary>
    /// 场景入口脚本。支持两种模式：
    /// 1. 显示面板（_panelTypeName 非空）→ 面板关闭后加载 _nextScene
    /// 2. 直接加载场景（_panelTypeName 为空）→ 直接 LoadScene
    /// </summary>
    public class SceneEntry : MonoBehaviour
    {
        [SerializeField] private string _panelTypeName;
        [SerializeField] private string _nextSceneName;

        private IEnumerator Start()
        {
            yield return null; // 等 GameManager 初始化完毕

            if (!string.IsNullOrEmpty(_panelTypeName))
            {
                // 显示面板 → 等关闭 → 切场景
                ShowPanel(_panelTypeName);
                while (IsPanelOpen(_panelTypeName))
                    yield return null;
            }

            if (!string.IsNullOrEmpty(_nextSceneName))
            {
                SceneModule.Instance.LoadScene(_nextSceneName);
            }
        }

        private void ShowPanel(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type == null)
            {
                Debug.LogError($"[SceneEntry] 找不到类型: {typeName}");
                return;
            }
            var method = typeof(UIModule).GetMethod("Show").MakeGenericMethod(type);
            method.Invoke(UIModule.Instance, new object[] { UILayer.Default, null });
        }

        private bool IsPanelOpen(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type == null) return false;
            var method = typeof(UIModule).GetMethod("GetPanel").MakeGenericMethod(type);
            return method.Invoke(UIModule.Instance, null) != null;
        }
    }
}
