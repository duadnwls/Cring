using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 플레이어의 겉모습(메시 + 스켈레톤)만 새 Mixamo 캐릭터로 갈아끼운다.
/// CharacterController, PlayerCombat, 카메라 추적점 등 로직 컴포넌트는 전부 그대로 둔다.
/// </summary>
public static class PlayerModelSwapper
{
    const string ArenaScenePath = "Assets/Scenes/Arena.unity";
    const string ModelFolder = "Assets/Animation/Player/Character";

    [MenuItem("Tools/Inspect Player Hierarchy")]
    public static void Inspect()
    {
        var player = GameObject.Find("PlayerArmature");
        if (player == null)
        {
            Debug.LogError("PlayerArmature를 찾을 수 없습니다.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== PlayerArmature 컴포넌트 ===");
        foreach (var c in player.GetComponents<Component>())
            sb.AppendLine("  " + c.GetType().Name);

        sb.AppendLine("\n=== 자식(1단계) ===");
        foreach (Transform child in player.transform)
            sb.AppendLine($"  {child.name}");

        var animator = player.GetComponent<Animator>();
        if (animator != null)
            sb.AppendLine($"\nAvatar: {(animator.avatar != null ? animator.avatar.name : "없음")}, " +
                          $"isHuman={(animator.avatar != null && animator.avatar.isHuman)}");

        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Swap Player Model")]
    public static void Swap()
    {
        // 1) 새 캐릭터 FBX 찾기
        if (!Directory.Exists(ModelFolder))
        {
            Debug.LogError($"폴더가 없습니다: {ModelFolder}\n" +
                           "Mixamo에서 캐릭터를 받아 이 폴더에 넣어주세요.");
            return;
        }

        var fbxPaths = Directory.GetFiles(ModelFolder, "*.fbx", SearchOption.TopDirectoryOnly);
        if (fbxPaths.Length == 0)
        {
            Debug.LogError($"{ModelFolder} 안에 .fbx가 없습니다.");
            return;
        }
        if (fbxPaths.Length > 1)
        {
            Debug.LogError($"{ModelFolder} 안에 .fbx가 여러 개입니다. 캐릭터 하나만 남겨주세요:\n  " +
                           string.Join("\n  ", fbxPaths.Select(Path.GetFileName)));
            return;
        }

        string modelPath = fbxPaths[0].Replace('\\', '/');

        // 2) Humanoid 리그로 임포트
        var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("모델 임포터를 가져올 수 없습니다: " + modelPath);
            return;
        }
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
        }

        var avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isHuman)
        {
            Debug.LogError($"'{Path.GetFileName(modelPath)}'의 Humanoid 아바타 생성에 실패했습니다.\n" +
                           "Inspector > Rig > Configure에서 본 매핑을 확인하세요. (Mutant 때와 같은 문제)");
            return;
        }

        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelPrefab == null)
        {
            Debug.LogError("모델 프리팹 로드 실패: " + modelPath);
            return;
        }

        // 3) 씬 열기
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.GetActiveScene().path == ArenaScenePath
            ? EditorSceneManager.GetActiveScene()
            : EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

        var player = GameObject.Find("PlayerArmature");
        if (player == null)
        {
            Debug.LogError("PlayerArmature를 찾을 수 없습니다.");
            return;
        }

        // 4) 겉모습에 해당하는 자식만 제거 (PlayerCameraRoot 등 로직용은 보존)
        var toRemove = player.transform.Cast<Transform>()
            .Where(t => t.name != "PlayerCameraRoot")
            .ToList();
        foreach (var t in toRemove)
        {
            Debug.Log($"기존 모델 제거: {t.name}");
            Object.DestroyImmediate(t.gameObject);
        }

        // 5) 새 모델을 자식으로 넣고 프리팹 연결을 끊는다 (본 구조를 직접 다루기 위해)
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, player.transform);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instance.name = "Model";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        // 6) 아바타 교체 — 이게 있어야 기존 Humanoid 애니메이션이 새 몸에 그대로 적용된다
        var animator = player.GetComponent<Animator>();
        animator.avatar = avatar;
        animator.Rebind();

        // 7) 캐릭터 높이에 맞춰 카메라 추적점 위치 보정
        var head = FindDeep(instance.transform, "mixamorig:Head") ?? FindDeep(instance.transform, "Head");
        var cameraRoot = player.transform.Find("PlayerCameraRoot");
        if (head != null && cameraRoot != null)
        {
            float headHeight = head.position.y - player.transform.position.y;
            cameraRoot.localPosition = new Vector3(0f, headHeight + 0.15f, 0f);
            Debug.Log($"카메라 추적점 높이를 {cameraRoot.localPosition.y:F2}로 맞췄습니다.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"플레이어 모델 교체 완료: {Path.GetFileName(modelPath)}\n" +
                  "Play로 이동/공격/구르기가 정상인지 확인하세요.");
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
