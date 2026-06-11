using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FPT.Editor
{
    public class AssignUssToUIDocument : EditorWindow
    {
        private static readonly string[] UssPaths =
        {
            "Assets/FPT/UI/UI_Toolkit/USS/Main.uss",
            "Assets/FPT/UI/UI_Toolkit/USS/TopBar.uss",
            "Assets/FPT/UI/UI_Toolkit/USS/Panels.uss",
            "Assets/FPT/UI/UI_Toolkit/USS/Widgets.uss",
        };

        [MenuItem("FPT/Assign USS to UIDocument")]
        public static void Fix()
        {
            var mainView = Object.FindObjectOfType<FPT.UI.MainViewController>();
            if (mainView == null)
            {
                Debug.LogError("[AssignUSS] 未找到 MainViewController");
                return;
            }

            var so = new SerializedObject(mainView);
            var arr = so.FindProperty("_styleSheets");
            if (arr == null)
            {
                Debug.LogError("[AssignUSS] 未找到 _styleSheets 字段");
                return;
            }

            arr.ClearArray();
            for (int i = 0; i < UssPaths.Length; i++)
            {
                var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPaths[i]);
                if (sheet != null)
                {
                    arr.InsertArrayElementAtIndex(i);
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = sheet;
                    Debug.Log($"[AssignUSS] {System.IO.Path.GetFileName(UssPaths[i])}");
                }
            }
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            // 清理 UXML 中的 <Style> 标签
            var uxmlPath = "Assets/FPT/UI/UI_Toolkit/UXML/MainLayout.uxml";
            var lines = System.IO.File.ReadAllLines(uxmlPath);
            var sb = new System.Text.StringBuilder();
            foreach (var line in lines)
            {
                if (!line.Contains("<Style src="))
                    sb.AppendLine(line);
            }
            System.IO.File.WriteAllText(uxmlPath, sb.ToString());
            AssetDatabase.ImportAsset(uxmlPath);

            Debug.Log("[AssignUSS] 完成：USS 已序列化到场景 + UXML <Style> 已清除");
        }
    }
}
