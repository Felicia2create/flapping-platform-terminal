using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FPT.Editor
{
    /// <summary>
    /// 场景光照搭建 — 三灯布光（主光 + 补光 + 天光）
    /// </summary>
    public class SetupSceneLighting : EditorWindow
    {
        [MenuItem("FPT/Setup Scene Lighting")]
        public static void Setup()
        {
            // 清理已有灯光
            var existing = Object.FindObjectsOfType<Light>();
            foreach (var l in existing)
                Object.DestroyImmediate(l.gameObject);

            // === 1. 主光 (Key Light) — 暖白，右前上方 ===
            var key = CreateDirectional("KeyLight",
                new Vector3(3, 5, 2), new Vector3(50, 330, 0),
                Color.white * 1.1f, 2.5f, LightShadows.Soft);

            // === 2. 补光 (Fill Light) — 冷白，左前下方，消除暗面 ===
            var fill = CreateDirectional("FillLight",
                new Vector3(-2, 2, -1), new Vector3(35, 140, 0),
                new Color(0.6f, 0.7f, 0.9f), 1.2f, LightShadows.None);

            // === 3. 环境光提升 ===
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.25f, 0.28f, 0.35f);
            RenderSettings.ambientEquatorColor = new Color(0.15f, 0.17f, 0.22f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.09f, 0.12f);

            // === 4. 场景反射 ===
            RenderSettings.reflectionIntensity = 0.7f;
            RenderSettings.reflectionBounces = 2;

            Debug.Log("[SetupLighting] 主光 + 补光 + 三色环境光已配置。若需提亮，在 Global Volume 中调 PostExposure +0.5");
        }

        private static Light CreateDirectional(string name,
            Vector3 pos, Vector3 rot, Color color, float intensity, LightShadows shadows)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(rot);

            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = shadows;
            light.shadowStrength = 0.8f;

            if (shadows != LightShadows.None)
            {
                light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;
                light.shadowBias = 0.05f;
                light.shadowNormalBias = 0.4f;
            }

            // UniversalAdditionalLightData 由 Unity 自动添加，无需手动

            Debug.Log($"  {name}: {color}, intensity={intensity}, shadows={shadows}");
            return light;
        }
    }
}
