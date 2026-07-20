using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PlayerCombatSetup
{
    const string SourceControllerPath = "Assets/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller";
    const string PlayerControllerPath = "Assets/Animation/PlayerAnimator.controller";
    const string ArenaScenePath = "Assets/Scenes/Arena.unity";

    [MenuItem("Tools/Setup Player Combat")]
    public static void Setup()
    {
        // 1) 임포트 설정 재적용 (클립 이름을 파일명으로 통일)
        MixamoImportConfigurator.Configure();

        // 2) 플레이어 전용 애니메이터 컨트롤러 생성 (원본 복사 + 전투 상태 추가)
        AssetDatabase.DeleteAsset(PlayerControllerPath);
        if (!AssetDatabase.CopyAsset(SourceControllerPath, PlayerControllerPath))
        {
            Debug.LogError("애니메이터 컨트롤러 복사 실패: " + SourceControllerPath);
            return;
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);
        var sm = controller.layers[0].stateMachine;

        AddState(sm, "Attack1", "Assets/Animation/Player/Great Sword Slash.fbx", new Vector3(560, 60, 0));
        AddState(sm, "Attack2", "Assets/Animation/Player/Great Sword Slash 2.fbx", new Vector3(560, 120, 0));
        AddState(sm, "Roll", "Assets/Animation/Player/Stand To Roll.fbx", new Vector3(560, 180, 0));
        AddState(sm, "Hit", "Assets/Animation/Player/Standing React Large From Front.fbx", new Vector3(560, 240, 0));
        AddState(sm, "Death", "Assets/Animation/Player/Standing Death Forward 01.fbx", new Vector3(560, 300, 0));

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        // 3) Arena 씬에 배치
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.GetActiveScene().path == ArenaScenePath
            ? EditorSceneManager.GetActiveScene()
            : EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

        var player = GameObject.Find("PlayerArmature");
        if (player == null)
        {
            Debug.LogError("PlayerArmature를 씬에서 찾을 수 없습니다. Tools > Build Greybox Arena를 먼저 실행하세요.");
            return;
        }

        player.GetComponent<Animator>().runtimeAnimatorController = controller;
        if (player.GetComponent<Health>() == null) player.AddComponent<Health>();
        if (player.GetComponent<PlayerCombat>() == null) player.AddComponent<PlayerCombat>();

        // 4) 연습용 더미 생성
        if (GameObject.Find("TrainingDummy") == null)
        {
            var dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = "TrainingDummy";
            dummy.transform.position = new Vector3(2.5f, 1f, -8f);

            var health = dummy.AddComponent<Health>();
            var so = new SerializedObject(health);
            so.FindProperty("maxHealth").floatValue = 60f;
            so.ApplyModifiedPropertiesWithoutUndo();

            dummy.AddComponent<TrainingDummy>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("플레이어 전투 셋업 완료! Play를 눌러 좌클릭 공격, Space 구르기를 테스트하세요.");
    }

    static void AddState(AnimatorStateMachine sm, string stateName, string fbxPath, Vector3 position)
    {
        // 이미 있으면 제거 후 재생성 (재실행 안전)
        var existing = sm.states.FirstOrDefault(s => s.state != null && s.state.name == stateName).state;
        if (existing != null)
            sm.RemoveState(existing);

        var clip = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.Contains("__preview__"));

        if (clip == null)
        {
            Debug.LogError($"클립을 찾을 수 없음: {fbxPath}");
            return;
        }

        var state = sm.AddState(stateName, position);
        state.motion = clip;
    }
}
