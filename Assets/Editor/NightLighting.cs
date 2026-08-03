using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 밤 분위기 조명. 달빛은 형태만 잡아주는 정도로 약하게 두고,
/// 실제 밝기는 화톳불이 담당하게 해서 소울라이크 특유의 어두운 화면을 만든다.
/// </summary>
public static class NightLighting
{
    const string ProfilePath = "Assets/Settings/ArenaPostProcess.asset";
    const string SkyboxPath = "Assets/Materials/Generated/NightSky.mat";

    [MenuItem("Tools/Apply Night Lighting")]
    public static void ApplyFromMenu()
    {
        Apply();
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("밤 조명 적용 완료. 어두우면 화톳불(Brazier) 조명 intensity를 올려보세요.");
    }

    public static void Apply()
    {
        ApplySky();
        ApplyDirectionalLight();
        ApplyAmbientAndFog();
        ApplyBraziers();
        ApplyPostProcessing();
    }

    static void ApplySky()
    {
        Directory.CreateDirectory("Assets/Materials/Generated");

        var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
        if (sky == null)
        {
            sky = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(sky, SkyboxPath);
        }
        sky.SetFloat("_SunSize", 0.02f);
        sky.SetFloat("_AtmosphereThickness", 0.45f);
        sky.SetColor("_SkyTint", new Color(0.10f, 0.13f, 0.24f));
        sky.SetColor("_GroundColor", new Color(0.02f, 0.02f, 0.03f));
        sky.SetFloat("_Exposure", 0.30f);
        EditorUtility.SetDirty(sky);

        RenderSettings.skybox = sky;
    }

    static void ApplyDirectionalLight()
    {
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type != LightType.Directional) continue;

            // 높은 각도의 창백한 달빛 — 형태만 겨우 드러낼 정도
            light.transform.rotation = Quaternion.Euler(48f, 150f, 0f);
            light.color = new Color(0.60f, 0.70f, 1f);
            light.intensity = 0.32f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
        }
    }

    static void ApplyAmbientAndFog()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.055f, 0.065f, 0.105f);
        RenderSettings.ambientEquatorColor = new Color(0.035f, 0.038f, 0.055f);
        RenderSettings.ambientGroundColor = new Color(0.015f, 0.015f, 0.020f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.040f, 0.045f, 0.065f);
        RenderSettings.fogStartDistance = 14f;
        RenderSettings.fogEndDistance = 58f;
    }

    /// <summary>어두워진 만큼 화톳불이 주광원 역할을 하도록 밝기와 범위를 올린다.</summary>
    static void ApplyBraziers()
    {
        var braziers = GameObject.Find("ArenaDecor/Braziers");
        if (braziers == null) return;

        foreach (var light in braziers.GetComponentsInChildren<Light>(true))
        {
            if (light.type != LightType.Point) continue;
            light.color = new Color(1f, 0.58f, 0.24f);
            light.intensity = 5.2f;
            light.range = 14f;
        }
    }

    static void ApplyPostProcessing()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            Debug.LogWarning("포스트프로세싱 프로파일이 없습니다. Tools > Setup Visuals And Feel을 먼저 실행하세요.");
            return;
        }

        // 불빛이 어둠 속에서 번지도록 블룸을 강하게
        if (!profile.TryGet<Bloom>(out var bloom)) bloom = profile.Add<Bloom>(true);
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 1.1f;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 0.75f;
        bloom.tint.overrideState = true;
        bloom.tint.value = new Color(1f, 0.85f, 0.7f);

        if (!profile.TryGet<Vignette>(out var vignette)) vignette = profile.Add<Vignette>(true);
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.45f;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.45f;

        if (!profile.TryGet<ColorAdjustments>(out var color)) color = profile.Add<ColorAdjustments>(true);
        color.postExposure.overrideState = true;
        color.postExposure.value = -0.35f;
        color.contrast.overrideState = true;
        color.contrast.value = 18f;
        color.saturation.overrideState = true;
        color.saturation.value = -6f;
        color.colorFilter.overrideState = true;
        color.colorFilter.value = new Color(0.92f, 0.95f, 1f); // 살짝 푸른 밤 색조

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
    }
}
