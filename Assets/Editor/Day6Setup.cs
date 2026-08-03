using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class Day6Setup
{
    const string ArenaScenePath = "Assets/Scenes/Arena.unity";
    const string MutantPath = "Assets/Animation/Boss/Mutant.fbx";
    const string TextureFolder = "Assets/Animation/Boss/Textures";
    const string ProfilePath = "Assets/Settings/ArenaPostProcess.asset";

    [MenuItem("Tools/Setup Visuals And Feel")]
    public static void Setup()
    {
        ExtractMutantTextures();

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.GetActiveScene().path == ArenaScenePath
            ? EditorSceneManager.GetActiveScene()
            : EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

        SetupFeedback();
        SetupPostProcessing();   // 프로파일과 볼륨을 만들고
        NightLighting.Apply();   // 밤 값으로 덮어쓴다 (이 메뉴를 재실행해도 낮으로 되돌아가지 않도록)

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Day 6 셋업 완료: 보스 텍스처 + 타격감 + 분위기 조명/포스트프로세싱");
    }

    static void ExtractMutantTextures()
    {
        var importer = AssetImporter.GetAtPath(MutantPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("Mutant.fbx를 찾을 수 없어 텍스처 추출을 건너뜁니다.");
            return;
        }

        Directory.CreateDirectory(TextureFolder);
        if (importer.ExtractTextures(TextureFolder))
            AssetDatabase.Refresh();

        // 노멀맵으로 임포트되도록 설정
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.ToLower().Contains("normal")) continue;

            var texImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (texImporter != null && texImporter.textureType != TextureImporterType.NormalMap)
            {
                texImporter.textureType = TextureImporterType.NormalMap;
                texImporter.SaveAndReimport();
            }
        }

        // 재질을 FBX 밖으로 빼내 추출된 텍스처를 물게 한다
        importer.materialLocation = ModelImporterMaterialLocation.External;
        importer.SaveAndReimport();
        AssetDatabase.Refresh();
        Debug.Log("Mutant 텍스처/재질 추출 완료 → " + TextureFolder);
    }

    static void SetupFeedback()
    {
        var boss = GameObject.Find("Boss");
        if (boss != null && boss.GetComponent<HitFlash>() == null)
            boss.AddComponent<HitFlash>();

        var player = GameObject.Find("PlayerArmature");
        if (player != null && player.GetComponent<HitFlash>() == null)
            player.AddComponent<HitFlash>();

        if (Object.FindFirstObjectByType<CombatFeedback>() == null)
            new GameObject("CombatFeedback").AddComponent<CombatFeedback>();
    }

    static void SetupPostProcessing()
    {
        Directory.CreateDirectory("Assets/Settings");

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        if (!profile.TryGet<Tonemapping>(out var tonemap))
            tonemap = profile.Add<Tonemapping>(true);
        tonemap.mode.overrideState = true;
        tonemap.mode.value = TonemappingMode.ACES;

        if (!profile.TryGet<Bloom>(out var bloom))
            bloom = profile.Add<Bloom>(true);
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0.55f;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 1.05f;

        if (!profile.TryGet<Vignette>(out var vignette))
            vignette = profile.Add<Vignette>(true);
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.34f;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.42f;

        if (!profile.TryGet<ColorAdjustments>(out var color))
            color = profile.Add<ColorAdjustments>(true);
        color.contrast.overrideState = true;
        color.contrast.value = 14f;
        color.saturation.overrideState = true;
        color.saturation.value = -12f;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        var volumeGO = GameObject.Find("PostProcessVolume");
        if (volumeGO == null) volumeGO = new GameObject("PostProcessVolume");
        var volume = volumeGO.GetComponent<Volume>();
        if (volume == null) volume = volumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;
        volume.sharedProfile = profile;

        // 카메라에서 포스트프로세싱 켜기
        var cam = Camera.main;
        if (cam != null)
        {
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }
        else
        {
            Debug.LogWarning("Main Camera를 찾지 못해 포스트프로세싱을 켜지 못했습니다.");
        }
    }
}
