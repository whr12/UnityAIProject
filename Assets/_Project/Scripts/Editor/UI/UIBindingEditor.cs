using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using AIProject.Core;

namespace AIProject.Editor.UI
{
    [CustomEditor(typeof(UIBinding))]
    public class UIBindingEditor : OdinEditor
    {
        private const string DefaultViewDir = "Assets/_Project/Scripts/UI/View";
        private const string DefaultPanelDir = "Assets/_Project/Scripts/UI/Panel";

        private UIBinding _binding;
        private bool HasViewBound =>
            !string.IsNullOrEmpty(_binding._viewScriptPath) && File.Exists(_binding._viewScriptPath);

        protected override void OnEnable()
        {
            base.OnEnable();
            _binding = (UIBinding)target;
        }

        public override void OnInspectorGUI()
        {
            // 条目列表
            DrawEntries();
            EditorGUILayout.Space();

            // UIView 路径
            DrawViewScriptField();
            EditorGUILayout.Space();

            // 按钮
            DrawButtons();
        }

        // ===== 条目列表 =====

        private void DrawEntries()
        {
            EditorGUILayout.LabelField("绑定条目", EditorStyles.boldLabel);

            var entriesProp = serializedObject.FindProperty("_entries");
            if (entriesProp == null) return;

            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                var element = entriesProp.GetArrayElementAtIndex(i);
                DrawEntryElement(element, i, entriesProp);
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 添加条目"))
                entriesProp.arraySize++;
            EditorGUILayout.EndHorizontal();

            // 校验
            var dupNames = FindDuplicateNames(entriesProp);
            foreach (var msg in dupNames)
                EditorGUILayout.HelpBox(msg, MessageType.Error);

            var typeErrors = FindTypeMismatches(entriesProp);
            foreach (var msg in typeErrors)
                EditorGUILayout.HelpBox(msg, MessageType.Error);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEntryElement(SerializedProperty element, int index, SerializedProperty entriesProp)
        {
            var nameProp = element.FindPropertyRelative("Name");
            var compsProp = element.FindPropertyRelative("Components");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 行1: 名字 + 删除
            EditorGUILayout.BeginHorizontal();
            nameProp.stringValue = EditorGUILayout.TextField(nameProp.stringValue, GUILayout.Width(180));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                entriesProp.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            // 行2+: 组件列表
            if (compsProp.isArray)
            {
                for (int j = 0; j < compsProp.arraySize; j++)
                {
                    var comp = compsProp.GetArrayElementAtIndex(j);
                    DrawComponentElement(comp, compsProp, j);
                }
            }

            // 添加组件
            if (GUILayout.Button("+ 组件", GUILayout.Width(60)))
            {
                compsProp.arraySize++;
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawComponentElement(SerializedProperty comp, SerializedProperty compsProp, int j)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(comp, GUIContent.none);

            // 同GameObject切换
            if (comp.objectReferenceValue != null)
            {
                if (GUILayout.Button("▾", GUILayout.Width(24)))
                    ShowSiblingComponentMenu(comp, compsProp, j);
            }

            // 删除
            if (GUILayout.Button("X", GUILayout.Width(24)))
                compsProp.DeleteArrayElementAtIndex(j);

            EditorGUILayout.EndHorizontal();
        }

        private void ShowSiblingComponentMenu(SerializedProperty comp, SerializedProperty compsProp, int index)
        {
            var current = comp.objectReferenceValue as Component;
            if (current == null) return;

            var menu = new GenericMenu();
            foreach (var c in current.gameObject.GetComponents<Component>())
            {
                if (c is Transform || c is CanvasRenderer) continue;
                var captured = c;
                menu.AddItem(new GUIContent(c.GetType().Name), c == current, () =>
                {
                    compsProp.GetArrayElementAtIndex(index).objectReferenceValue = captured;
                    compsProp.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }

        private List<string> FindDuplicateNames(SerializedProperty entriesProp)
        {
            var errors = new List<string>();
            var seen = new Dictionary<string, int>();
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                var n = entriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("Name")?.stringValue;
                if (string.IsNullOrEmpty(n)) continue;
                if (seen.ContainsKey(n))
                    errors.Add($"条目 [{i}] 与 [{seen[n]}] 同名: \"{n}\"");
                else
                    seen[n] = i;
            }
            return errors;
        }

        private List<string> FindTypeMismatches(SerializedProperty entriesProp)
        {
            var errors = new List<string>();
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                var nameProp = entriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("Name");
                var compsProp = entriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("Components");
                if (compsProp == null || !compsProp.isArray || compsProp.arraySize < 2) continue;
                var first = compsProp.GetArrayElementAtIndex(0)?.objectReferenceValue;
                if (first == null) continue;
                var ft = first.GetType();
                for (int j = 1; j < compsProp.arraySize; j++)
                {
                    var other = compsProp.GetArrayElementAtIndex(j)?.objectReferenceValue;
                    if (other != null && other.GetType() != ft)
                    {
                        errors.Add($"条目 [{i}] \"{nameProp?.stringValue}\": 类型不一致 — {ft.Name} / {other.GetType().Name}");
                        break;
                    }
                }
            }
            return errors;
        }

        // ===== UIView 路径 =====

        private void DrawViewScriptField()
        {
            EditorGUILayout.LabelField("UIView", EditorStyles.boldLabel);

            var pathProp = serializedObject.FindProperty("_viewScriptPath");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField(pathProp.stringValue);
            if (GUILayout.Button("定位", GUILayout.Width(40)))
                PingAsset(pathProp.stringValue);
            if (GUILayout.Button("打开", GUILayout.Width(40)))
                OpenAsset(pathProp.stringValue);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var p = EditorUtility.OpenFilePanel("选择 UIView", "Assets", "cs");
                if (!string.IsNullOrEmpty(p) && p.StartsWith(Application.dataPath))
                    pathProp.stringValue = "Assets" + p.Substring(Application.dataPath.Length);
            }
            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
        }

        // ===== 按钮 =====

        private void DrawButtons()
        {
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
            if (GUILayout.Button("生成代码", GUILayout.Height(28)))
                GenerateCode();
            GUI.backgroundColor = Color.white;

            if (HasViewBound)
            {
                if (GUILayout.Button("创建 UIPanel", GUILayout.Height(28)))
                    CreatePanel();
            }
        }

        // ===== 代码生成 =====

        private void GenerateCode()
        {
            var prefabPath = GetPrefabPath();
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError("[UIBinding] 请先保存 Prefab。");
                return;
            }

            var prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            var isFirstTime = string.IsNullOrEmpty(_binding._viewScriptPath);

            // UIView
            var viewClassName = $"UI{prefabName}View";
            var viewPath = GetOrChooseSavePath(viewClassName, _binding._viewScriptPath, "UIView",
                "UIBinding_LastViewDir", DefaultViewDir);
            if (string.IsNullOrEmpty(viewPath)) return;

            File.WriteAllText(viewPath, GenerateViewCode(viewClassName), Encoding.UTF8);
            _binding._viewScriptPath = viewPath;
            EditorUtility.SetDirty(_binding);
            Debug.Log($"[UIBinding] 生成 UIView: {viewPath}");

            // UIPanel — 首次生成时同步创建
            if (isFirstTime)
                CreatePanel();

            AssetDatabase.Refresh();
        }

        private void CreatePanel()
        {
            var prefabPath = GetPrefabPath();
            if (string.IsNullOrEmpty(prefabPath)) return;
            var prefabName = Path.GetFileNameWithoutExtension(prefabPath);

            var panelClassName = GetPanelClassName(prefabName);
            var panelPath = GetOrChooseSavePath(panelClassName, null, "UIPanel",
                "UIBinding_LastPanelDir", DefaultPanelDir);
            if (string.IsNullOrEmpty(panelPath) || File.Exists(panelPath))
            {
                if (File.Exists(panelPath))
                    Debug.LogWarning($"[UIBinding] UIPanel 已存在: {panelPath}");
                return;
            }

            var viewClassName = Path.GetFileNameWithoutExtension(_binding._viewScriptPath);
            File.WriteAllText(panelPath,
                GeneratePanelCode(panelClassName, prefabName, viewClassName), Encoding.UTF8);
            Debug.Log($"[UIBinding] 生成 UIPanel: {panelPath}");
            AssetDatabase.Refresh();
        }

        private string GetOrChooseSavePath(string className, string currentPath, string label,
            string prefsKey, string defaultDir)
        {
            if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
                return currentPath;

            var lastDir = EditorPrefs.GetString(prefsKey, defaultDir);
            EnsureDirectoryExists(lastDir);
            var p = EditorUtility.SaveFilePanelInProject(
                $"保存 {label} 脚本", $"{className}.cs", "cs", $"选择 {label} 保存位置", lastDir);
            if (string.IsNullOrEmpty(p)) return null;

            var dir = Path.GetDirectoryName(p);
            if (!string.IsNullOrEmpty(dir)) EditorPrefs.SetString(prefsKey, dir);
            return p;
        }

        private static void EnsureDirectoryExists(string dirPath)
        {
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
        }

        // ===== 代码模板 =====

        private string GenerateViewCode(string className)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("using UnityEngine; using UnityEngine.UI; using TMPro; using AIProject.Core;");
            sb.AppendLine();
            sb.AppendLine("namespace AIProject.UI");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className} : UIView");
            sb.AppendLine("    {");

            foreach (var e in _binding._entries)
            {
                if (e.Components == null || e.Components.Count == 0 || e.Components[0] == null) continue;
                string tn = NormalizeTypeName(e.Components[0].GetType().Name);
                if (e.Components.Count > 1)
                    sb.AppendLine($"        public {tn}[] {e.Name} {{ get; private set; }}");
                else
                    sb.AppendLine($"        public {tn} {e.Name} {{ get; private set; }}");
            }

            sb.AppendLine();
            sb.AppendLine($"        public {className}(UIBinding b) : base(b)");
            sb.AppendLine("        {");
            foreach (var e in _binding._entries)
            {
                if (e.Components == null || e.Components.Count == 0 || e.Components[0] == null) continue;
                string tn = NormalizeTypeName(e.Components[0].GetType().Name);
                if (e.Components.Count > 1)
                    sb.AppendLine($"            {e.Name} = b.GetAll<{tn}>(\"{e.Name}\");");
                else
                    sb.AppendLine($"            {e.Name} = b.Get<{tn}>(\"{e.Name}\");");
            }
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string GetPanelClassName(string prefabName)
        {
            if (prefabName.EndsWith("Panel", StringComparison.OrdinalIgnoreCase))
                prefabName = prefabName.Substring(0, prefabName.Length - 5);
            return $"UI{prefabName}Panel";
        }

        private static string GeneratePanelCode(string className, string prefabName, string viewClassName)
        {
            return $@"using AIProject.Core;

namespace AIProject.UI
{{
    public class {className} : UIPanel
    {{
        public override string PrefabPath => ""UI/{prefabName}"";

        protected internal override UIView CreateView(UIBinding binding)
            => new {viewClassName}(binding);

        protected override void OnOpen()
        {{
            var view = ({viewClassName})View;
            // TODO: 在此绑定 UI 组件
        }}

        protected override void OnClose()
        {{
        }}
    }}
}}";
        }

        private static string NormalizeTypeName(string n)
        {
            if (n == "TMP_Text" || n == "TextMeshProUGUI") return "TMP_Text";
            if (n == "TMP_InputField") return "TMP_InputField";
            return n;
        }

        // ===== 辅助 =====

        private string GetPrefabPath()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null) return stage.assetPath;
            var source = PrefabUtility.GetCorrespondingObjectFromSource(_binding.gameObject);
            if (source != null) return AssetDatabase.GetAssetPath(source);
            return null;
        }

        private static void PingAsset(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var a = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (a != null) EditorGUIUtility.PingObject(a);
        }

        private static void OpenAsset(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var a = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (a != null) AssetDatabase.OpenAsset(a);
        }
    }
}
