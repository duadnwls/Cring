using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class Day4Setup
{
    const string ArenaScenePath = "Assets/Scenes/Arena.unity";
    const string MutantModelPath = "Assets/Animation/Boss/Mutant.fbx";
    const string BossControllerPath = "Assets/Animation/BossAnimator.controller";

    [MenuItem("Tools/Setup Boss")]
    public static void Setup()
    {
        // 0) Mutant 모델 확인
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(MutantModelPath);
        if (model == null)
        {
            Debug.LogError($"Mutant 모델이 없습니다: {MutantModelPath}\n" +
                           "Mixamo > Characters 탭에서 Mutant를 T-Pose로 다운로드해 Boss 폴더에 넣어주세요.");
            return;
        }

        // 1) 임포트 설정 (Mutant.fbx 포함 Humanoid 전환)
        MixamoImportConfigurator.Configure();

        // 2) 보스 애니메이터 컨트롤러 생성
        AssetDatabase.DeleteAsset(BossControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(BossControllerPath);
        var sm = controller.layers[0].stateMachine;

        var idle = AddState(sm, "Idle", "Assets/Animation/Boss/Mutant Idle.fbx", new Vector3(300, 0, 0));
        AddState(sm, "Walk", "Assets/Animation/Boss/Mutant Walking.fbx", new Vector3(300, 60, 0));
        AddState(sm, "Swipe", "Assets/Animation/Boss/Mutant Swiping.fbx", new Vector3(300, 120, 0));
        AddState(sm, "Punch", "Assets/Animation/Boss/Mutant Punch.fbx", new Vector3(300, 180, 0));
        AddState(sm, "Roar", "Assets/Animation/Boss/Mutant Roaring.fbx", new Vector3(300, 240, 0));
        AddState(sm, "Die", "Assets/Animation/Boss/Mutant Dying.fbx", new Vector3(300, 300, 0));
        if (idle != null) sm.defaultState = idle;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        // 3) 씬 열기
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.GetActiveScene().path == ArenaScenePath
            ? EditorSceneManager.GetActiveScene()
            : EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

        // 4) NavMesh 베이크 (Arena의 자식들만 대상)
        var arena = GameObject.Find("Arena");
        if (arena != null)
        {
            var surface = arena.GetComponent<NavMeshSurface>();
            if (surface == null) surface = arena.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();
        }
        else
        {
            Debug.LogWarning("Arena 오브젝트를 찾지 못해 NavMesh 베이크를 건너뜀");
        }

        // 5) 보스 배치
        if (GameObject.Find("Boss") == null)
        {
            var spawn = GameObject.Find("BossSpawnPoint");
            Vector3 pos = spawn != null ? spawn.transform.position : new Vector3(0f, 0f, 8f);

            var boss = (GameObject)PrefabUtility.InstantiatePrefab(model);
            boss.name = "Boss";
            boss.transform.position = pos;
            boss.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // 플레이어 입장 방향을 바라봄

            var animator = boss.GetComponent<Animator>();
            if (animator == null) animator = boss.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            var agent = boss.AddComponent<NavMeshAgent>();
            agent.speed = 2.5f;
            agent.angularSpeed = 540f;
            agent.acceleration = 16f;
            agent.stoppingDistance = 1.8f;
            agent.radius = 0.6f;
            agent.height = 2.4f;

            var col = boss.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1.2f, 0f);
            col.radius = 0.6f;
            col.height = 2.4f;

            var health = boss.AddComponent<Health>();
            var so = new SerializedObject(health);
            so.FindProperty("maxHealth").floatValue = 200f;
            so.ApplyModifiedPropertiesWithoutUndo();

            boss.AddComponent<BossAI>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("보스 셋업 완료! Play를 눌러 보스전을 테스트하세요.");
    }

    static AnimatorState AddState(AnimatorStateMachine sm, string stateName, string fbxPath, Vector3 position)
    {
        var existing = sm.states.FirstOrDefault(s => s.state != null && s.state.name == stateName).state;
        if (existing != null)
            sm.RemoveState(existing);

        var clip = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.Contains("__preview__"));

        if (clip == null)
        {
            Debug.LogError($"클립을 찾을 수 없음: {fbxPath}");
            return null;
        }

        var state = sm.AddState(stateName, position);
        state.motion = clip;
        return state;
    }
}
