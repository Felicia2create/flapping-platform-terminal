using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FPT.Editor
{
    /// <summary>
    /// 一键修复 flapping_platform 预制体材质：
    /// 创建 URP Lit 漆面材质 → 扫描所有 MeshRenderer → 按层级批量赋材
    /// 底座 (platform_base/link → link)→ 深蓝白 (#2A3A5C)
    /// 机械臂 (arm1_link → arm1_link6) → 白蓝 (#D4DCE8)
    /// </summary>
    public class FixPlatformMaterials : EditorWindow
    {
        private static readonly string PrefabPath =
            "Assets/FPT/Visualization/Runtime/flapping_platform_prefabs/flapping_platform.prefab";
        private static readonly string MaterialDir =
            "Assets/FPT/Visualization/Runtime/flapping_platform_prefabs/Materials";

        [MenuItem("FPT/Fix Platform Materials")]
        public static void Fix()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[FixMaterials] 预制体未找到: {PrefabPath}");
                return;
            }

            // 1. 创建材质（如果不存在则创建，存在则更新颜色）
            var baseMat = CreateOrGetMaterial("PlatformBase.mat",
                new Color(0.165f, 0.227f, 0.361f), // #2A3A5C 深蓝白
                0.3f, 0.5f);
            var armMat = CreateOrGetMaterial("ArmWhite.mat",
                new Color(0.831f, 0.863f, 0.910f), // #D4DCE8 白蓝
                0.15f, 0.8f);

            // 2. 打开预制体编辑
            var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            var allRenderers = contents.GetComponentsInChildren<MeshRenderer>(true);
            var allSkinned = contents.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            int baseCount = 0, armCount = 0, skipped = 0;

            foreach (var r in allRenderers)
            {
                var path = GetPath(r.transform, contents.transform);
                var mat = PickMaterial(path);
                if (mat != null)
                {
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                    r.sharedMaterials = mats;

                    if (mat == baseMat) baseCount++;
                    else armCount++;
                }
                else skipped++;
            }

            foreach (var r in allSkinned)
            {
                var path = GetPath(r.transform, contents.transform);
                var mat = PickMaterial(path);
                if (mat != null)
                {
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                    r.sharedMaterials = mats;

                    if (mat == baseMat) baseCount++;
                    else armCount++;
                }
                else skipped++;
            }

            // 3. 保存
            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[FixMaterials] 底座: {baseCount}, 机械臂: {armCount}, 跳过: {skipped}");

            Material PickMaterial(string p)
            {
                var lower = p.ToLower();
                if (lower.Contains("platform_base") || lower.Contains("plate") ||
                    lower.Contains("arm1_base") || lower.Contains("world"))
                    return baseMat;
                if (lower.Contains("arm1_link") || lower.Contains("link"))
                    return armMat;
                return null; // Eagle etc.
            }
        }

        private static Material CreateOrGetMaterial(string name, Color color,
            float metallic, float smoothness)
        {
            var assetPath = $"{MaterialDir}/{name}";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            var created = false;

            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    Debug.LogError("[FixMaterials] URP Lit shader not found!");
                    return null;
                }
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, assetPath);
                created = true;
            }

            // URP Lit 属性
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Surface", 0); // Opaque
            mat.SetFloat("_WorkflowMode", 1); // Specular workflow for paint-like finish
            mat.SetColor("_SpecColor", Color.white * 0.15f);
            mat.enableInstancing = true;

            EditorUtility.SetDirty(mat);

            if (created)
                Debug.Log($"[FixMaterials] 创建: {assetPath} ({ColorUtility.ToHtmlStringRGB(color)})");

            return mat;
        }

        private static string GetPath(Transform t, Transform root)
        {
            var path = t.name;
            while (t.parent != null && t.parent != root)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
