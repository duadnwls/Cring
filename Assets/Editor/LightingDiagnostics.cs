using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LightingDiagnostics
{
    [MenuItem("Tools/Diagnose Lighting")]
    public static void Run()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"씬: {SceneManager.GetActiveScene().name}");
        if (Lightmapping.TryGetLightingSettings(out var settings) && settings != null)
            sb.AppendLine($"LightingSettings '{settings.name}': bakedGI={settings.bakedGI}, realtimeGI={settings.realtimeGI}");
        else
            sb.AppendLine("LightingSettings 없음 (프로젝트 기본값 사용 중)");
        sb.AppendLine($"현재 베이크 중: {Lightmapping.isRunning}");
        sb.AppendLine($"라이트맵 개수: {LightmapSettings.lightmaps.Length}");
        sb.AppendLine($"LightingDataAsset: {(Lightmapping.lightingDataAsset != null ? Lightmapping.lightingDataAsset.name : "없음")}");
        sb.AppendLine($"라이트 프로브: {(LightmapSettings.lightProbes != null ? LightmapSettings.lightProbes.count.ToString() : "없음")}");
        sb.AppendLine();
        sb.AppendLine($"AmbientMode: {RenderSettings.ambientMode}");
        sb.AppendLine($"AmbientSky: {RenderSettings.ambientSkyColor}");
        sb.AppendLine($"AmbientIntensity: {RenderSettings.ambientIntensity}");
        sb.AppendLine($"Skybox: {(RenderSettings.skybox != null ? RenderSettings.skybox.name : "없음")}");
        sb.AppendLine($"Fog: {RenderSettings.fog} ({RenderSettings.fogStartDistance}~{RenderSettings.fogEndDistance})");
        sb.AppendLine();

        int staticGI = 0;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if ((GameObjectUtility.GetStaticEditorFlags(go) & StaticEditorFlags.ContributeGI) != 0)
                staticGI++;
        }
        sb.AppendLine($"ContributeGI가 켜진 오브젝트 수: {staticGI}");

        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            sb.AppendLine($"조명 '{light.name}': type={light.type}, mode={light.lightmapBakeType}, " +
                          $"intensity={light.intensity}, shadows={light.shadows}");
        }

        File.WriteAllText("Temp/lighting_diag.txt", sb.ToString());
        Debug.Log("조명 진단 완료 → Temp/lighting_diag.txt\n" + sb);
    }
}
