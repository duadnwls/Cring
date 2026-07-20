using System.IO;
using UnityEditor;
using UnityEngine;

public static class MixamoImportConfigurator
{
    [MenuItem("Tools/Configure Mixamo Imports")]
    public static void Configure()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Animation" });
        int count = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            // Idle/Walk 같은 반복 동작은 루프 설정
            string name = Path.GetFileNameWithoutExtension(path).ToLower();
            bool loop = name.Contains("idle") || name.Contains("walk") || name.Contains("run");

            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = Path.GetFileNameWithoutExtension(path); // "mixamo.com" → 파일명으로
                clip.loopTime = loop;
                clip.lockRootRotation = true;   // 회전은 코드가 담당
                clip.lockRootHeightY = true;    // 높이 고정 (바닥 뚫힘 방지)
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = false;
            }
            importer.clipAnimations = clips;

            importer.SaveAndReimport();
            count++;
        }

        Debug.Log($"Mixamo 임포트 설정 완료: {count}개 파일 (Humanoid 리그 + 루프/루트모션 설정)");
    }
}
