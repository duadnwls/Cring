using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GreyboxArenaBuilder
{
    const string ScenePath = "Assets/Scenes/Arena.unity";
    const string PlayerPrefabPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/NestedParentArmature_Unpack.prefab";

    [MenuItem("Tools/Build Greybox Arena")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 기본 씬의 Main Camera는 제거 (플레이어 프리팹에 포함된 카메라와 태그가 겹침)
        var defaultCam = GameObject.Find("Main Camera");
        if (defaultCam != null)
            Object.DestroyImmediate(defaultCam);

        var groundMat = GetOrCreateMaterial("Assets/Materials/Grey_Ground.mat", new Color(0.35f, 0.35f, 0.37f));
        var wallMat = GetOrCreateMaterial("Assets/Materials/Grey_Wall.mat", new Color(0.55f, 0.55f, 0.58f));

        var arenaRoot = new GameObject("Arena");

        // 원형 바닥 (반지름 22m, 윗면이 y=0)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ground.name = "Ground";
        Object.DestroyImmediate(ground.GetComponent<CapsuleCollider>());
        ground.AddComponent<MeshCollider>();
        ground.transform.SetParent(arenaRoot.transform);
        ground.transform.localScale = new Vector3(44f, 0.5f, 44f);
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

        // 외곽 벽 (큐브 링)
        const int segments = 40;
        const float radius = 21f;
        var wallRoot = new GameObject("Walls");
        wallRoot.transform.SetParent(arenaRoot.transform);
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            var pos = new Vector3(Mathf.Sin(angle) * radius, 3f, Mathf.Cos(angle) * radius);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = $"Wall_{i:00}";
            wall.transform.SetParent(wallRoot.transform);
            wall.transform.position = pos;
            wall.transform.rotation = Quaternion.LookRotation(new Vector3(pos.x, 0f, pos.z));
            wall.transform.localScale = new Vector3(3.6f, 6f, 1f);
            wall.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
        }

        // 기둥 4개 (구르기로 보스 시야를 끊는 용도)
        Vector3[] pillarPositions =
        {
            new Vector3(8f, 2.5f, 8f),
            new Vector3(-8f, 2.5f, 8f),
            new Vector3(8f, 2.5f, -8f),
            new Vector3(-8f, 2.5f, -8f),
        };
        foreach (var p in pillarPositions)
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Pillar";
            pillar.transform.SetParent(arenaRoot.transform);
            pillar.transform.position = p;
            pillar.transform.localScale = new Vector3(1.5f, 5f, 1.5f);
            pillar.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
        }

        // NavMesh 베이크와 라이팅을 위해 아레나 전체를 Static으로
        foreach (var t in arenaRoot.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, (StaticEditorFlags)~0);

        // 보스 스폰 위치 마커 (Day 4에서 사용)
        var bossSpawn = new GameObject("BossSpawnPoint");
        bossSpawn.transform.position = new Vector3(0f, 0f, 8f);

        // 플레이어 + 카메라 세트
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("플레이어 프리팹을 찾을 수 없습니다: " + PlayerPrefabPath);
        }
        else
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            var player = GameObject.Find("PlayerArmature");
            if (player != null)
                player.transform.position = new Vector3(0f, 0.1f, -14f);

            // PC 게임이므로 모바일용 조이스틱 UI 캔버스는 제거 (EventSystem은 나중에 UI용으로 유지)
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.gameObject.name.StartsWith("UI_Canvas_StarterAssetsInputs"))
                    Object.DestroyImmediate(canvas.gameObject);
            }
        }

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("그레이박스 아레나 생성 완료: " + ScenePath);
    }

    static Material GetOrCreateMaterial(string path, Color color)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            System.IO.Directory.CreateDirectory("Assets/Materials");
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        return mat;
    }
}
