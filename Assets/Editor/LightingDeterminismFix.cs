using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 플레이할 때마다 조명이 달라 보이는 문제 해결.
/// 원인: 아레나 오브젝트에 ContributeGI가 켜져 있어 자동 라이트 베이크가 백그라운드로 돌았음.
/// 조명은 전부 실시간이라 베이크 결과가 필요 없으므로, GI 참여를 끄고 자동 베이크를 비활성화한다.
/// </summary>
public static class LightingDeterminismFix
{
    const string SettingsPath = "Assets/Settings/ArenaLightingSettings.lighting";

    [MenuItem("Tools/Fix Lighting Determinism")]
    public static void Fix()
    {
        var scene = EditorSceneManager.GetActiveScene();

        // 1) 모든 오브젝트에서 GI 관련 Static 플래그 제거 (NavMesh/배칭 플래그는 유지)
        int changed = 0;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            var cleaned = flags & ~(StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
            if (cleaned != flags)
            {
                GameObjectUtility.SetStaticEditorFlags(go, cleaned);
                changed++;
            }
        }

        // 2) 자동 베이크를 끈 LightingSettings를 씬에 지정
        Directory.CreateDirectory("Assets/Settings");
        var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
        if (settings == null)
        {
            settings = new LightingSettings { name = "ArenaLightingSettings" };
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }
        // Unity 6에는 autoGenerate가 없다 — bakedGI/realtimeGI를 끄면 자동 베이크 자체가 돌지 않는다
        settings.bakedGI = false;
        settings.realtimeGI = false;
        EditorUtility.SetDirty(settings);
        Lightmapping.lightingSettings = settings;

        // 3) 기존 베이크 결과 제거
        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"조명 결정성 수정 완료: {changed}개 오브젝트에서 ContributeGI 해제, 자동 베이크 off, 베이크 데이터 삭제.\n" +
                  "이제 몇 번을 플레이해도 같은 화면이 나옵니다.");
    }
}
