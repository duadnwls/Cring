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
                clip.lockRootHeightY = true;    // 높이 고정 (바닥 뚫림/떠오름 방지)
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionXZ = false;

                // 루트 Y 기준을 "발"로 잡는다. "Original"로 두면 원본 클립이 만들어진
                // 기본 체형 기준 높이가 그대로 쓰여서, 체격이 다른 캐릭터는 공중에 뜬다.
                clip.keepOriginalPositionY = false;
                clip.heightFromFeet = true;
            }
            importer.clipAnimations = clips;

            importer.SaveAndReimport();
            count++;
        }

        Debug.Log($"Mixamo 임포트 설정 완료: {count}개 파일 (Humanoid 리그 + 루프/루트모션 설정)");
    }
}
