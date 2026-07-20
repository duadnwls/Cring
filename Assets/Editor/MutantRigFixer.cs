using UnityEditor;
using UnityEngine;

/// <summary>
/// Mutant.fbx의 Humanoid 자동 본 매핑 실패(LeftHand not found) 수정.
/// Mutant는 왼손이 갈퀴라 손가락 본이 없어 자동 매핑이 실패하므로, 본을 명시적으로 매핑한다.
/// </summary>
public static class MutantRigFixer
{
    const string MutantPath = "Assets/Animation/Boss/Mutant.fbx";

    // 휴머노이드 본 이름 → Mixamo 본 이름
    static readonly (string human, string bone)[] BoneMap =
    {
        ("Hips", "mixamorig:Hips"),
        ("Spine", "mixamorig:Spine"),
        ("Chest", "mixamorig:Spine1"),
        ("UpperChest", "mixamorig:Spine2"),
        ("Neck", "mixamorig:Neck"),
        ("Head", "mixamorig:Head"),
        ("LeftShoulder", "mixamorig:LeftShoulder"),
        ("LeftUpperArm", "mixamorig:LeftArm"),
        ("LeftLowerArm", "mixamorig:LeftForeArm"),
        ("LeftHand", "mixamorig:LeftHand"),
        ("RightShoulder", "mixamorig:RightShoulder"),
        ("RightUpperArm", "mixamorig:RightArm"),
        ("RightLowerArm", "mixamorig:RightForeArm"),
        ("RightHand", "mixamorig:RightHand"),
        ("LeftUpperLeg", "mixamorig:LeftUpLeg"),
        ("LeftLowerLeg", "mixamorig:LeftLeg"),
        ("LeftFoot", "mixamorig:LeftFoot"),
        ("LeftToes", "mixamorig:LeftToeBase"),
        ("RightUpperLeg", "mixamorig:RightUpLeg"),
        ("RightLowerLeg", "mixamorig:RightLeg"),
        ("RightFoot", "mixamorig:RightFoot"),
        ("RightToes", "mixamorig:RightToeBase"),
    };

    [MenuItem("Tools/Fix Mutant Rig")]
    public static void Fix()
    {
        var importer = AssetImporter.GetAtPath(MutantPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("Mutant.fbx를 찾을 수 없습니다: " + MutantPath);
            return;
        }

        var humanBones = new HumanBone[BoneMap.Length];
        for (int i = 0; i < BoneMap.Length; i++)
        {
            humanBones[i] = new HumanBone
            {
                humanName = BoneMap[i].human,
                boneName = BoneMap[i].bone,
                limit = new HumanLimit { useDefaultValues = true }
            };
        }

        // skeleton 배열을 실제 임포트된 계층에서 완전히 채움 (비워두면 Unity 6에서 본 해석 실패)
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(MutantPath);
        if (model == null)
        {
            Debug.LogError("Mutant.fbx 프리팹 로드 실패");
            return;
        }
        var skeletonBones = new System.Collections.Generic.List<SkeletonBone>();
        CollectSkeleton(model.transform, skeletonBones);

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.humanDescription = new HumanDescription
        {
            human = humanBones,
            skeleton = skeletonBones.ToArray(),
            upperArmTwist = 0.5f,
            lowerArmTwist = 0.5f,
            upperLegTwist = 0.5f,
            lowerLegTwist = 0.5f,
            armStretch = 0.05f,
            legStretch = 0.05f,
            feetSpacing = 0f,
            hasTranslationDoF = false
        };

        importer.SaveAndReimport();

        AssetDatabase.Refresh();

        // 결과 확인
        var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(MutantPath);
        if (avatar != null && avatar.isHuman)
            Debug.Log("Mutant 리그 수정 성공! Avatar가 Humanoid로 전환됨. Play로 보스 애니메이션을 확인하세요.");
        else
            Debug.LogError($"여전히 실패: avatar={(avatar != null ? avatar.name : "없음")}, isHuman={(avatar != null && avatar.isHuman)}. 콘솔의 Rig 에러를 확인하세요.");
    }

    static void CollectSkeleton(Transform t, System.Collections.Generic.List<SkeletonBone> list)
    {
        list.Add(new SkeletonBone
        {
            name = t.name,
            position = t.localPosition,
            rotation = t.localRotation,
            scale = t.localScale
        });
        foreach (Transform child in t)
            CollectSkeleton(child, list);
    }
}
