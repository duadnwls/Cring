using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 그레이박스 아레나에 석재 질감과 소품을 입힌다.
/// 장식물은 전부 "ArenaDecor" 루트 아래에 두어 Arena의 NavMesh 베이크에 영향을 주지 않는다.
/// </summary>
public static class ArenaDressing
{
    const string ArenaScenePath = "Assets/Scenes/Arena.unity";
    const string DecorRootName = "ArenaDecor";

    [MenuItem("Tools/Dress Up Arena")]
    public static void Dress()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.GetActiveScene().path == ArenaScenePath
            ? EditorSceneManager.GetActiveScene()
            : EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

        var arena = GameObject.Find("Arena");
        if (arena == null)
        {
            Debug.LogError("Arena 오브젝트를 찾을 수 없습니다.");
            return;
        }

        // 1) 석재 재질 생성 및 적용
        var floorMat = ProceduralStoneTextures.CreateStoneMaterial(
            "Stone_Floor", new Color(0.34f, 0.33f, 0.31f), blockScale: 7f, roughness: 0.88f, normalStrength: 6f);
        var wallMat = ProceduralStoneTextures.CreateStoneMaterial(
            "Stone_Wall", new Color(0.42f, 0.41f, 0.39f), blockScale: 4f, roughness: 0.82f, normalStrength: 8f);
        var pillarMat = ProceduralStoneTextures.CreateStoneMaterial(
            "Stone_Pillar", new Color(0.47f, 0.45f, 0.42f), blockScale: 3f, roughness: 0.8f, normalStrength: 7f);

        floorMat.SetTextureScale("_BaseMap", new Vector2(6f, 6f));
        floorMat.SetTextureScale("_BumpMap", new Vector2(6f, 6f));
        wallMat.SetTextureScale("_BaseMap", new Vector2(1.4f, 2.4f));
        wallMat.SetTextureScale("_BumpMap", new Vector2(1.4f, 2.4f));
        pillarMat.SetTextureScale("_BaseMap", new Vector2(1f, 3f));
        pillarMat.SetTextureScale("_BumpMap", new Vector2(1f, 3f));

        var ground = arena.transform.Find("Ground");
        if (ground != null) ground.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

        var walls = arena.transform.Find("Walls");
        if (walls != null)
        {
            foreach (Transform w in walls)
                w.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
        }

        foreach (Transform child in arena.transform)
        {
            if (child.name == "Pillar")
                child.GetComponent<MeshRenderer>().sharedMaterial = pillarMat;
        }

        // 2) 장식 루트 재생성 (NavMesh에 영향 없도록 Arena 바깥에 둔다)
        var oldDecor = GameObject.Find(DecorRootName);
        if (oldDecor != null) Object.DestroyImmediate(oldDecor);
        var decor = new GameObject(DecorRootName);

        Random.InitState(20260803); // 실행할 때마다 같은 배치가 나오도록 고정

        BuildBraziers(decor.transform, pillarMat, GetOrCreateFlameMaterial());
        BuildRubble(decor.transform, pillarMat);
        BuildWallTops(decor.transform, wallMat);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("아레나 장식 완료: 석재 재질 + 화톳불 8개 + 잔해 + 벽 상단 요철");
    }

    /// <summary>불타는 화톳불 — 조명과 불꽃이 분위기의 대부분을 만든다.</summary>
    static Material GetOrCreateFlameMaterial()
    {
        const string path = "Assets/Materials/Generated/FlameParticle.mat";
        System.IO.Directory.CreateDirectory("Assets/Materials/Generated");

        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            mat = new Material(shader) { color = Color.white };
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    static void BuildBraziers(Transform parent, Material stoneMat, Material flameMat)
    {
        const int count = 8;
        const float radius = 17f;

        var root = new GameObject("Braziers");
        root.transform.SetParent(parent, false);

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count + Mathf.PI / count;
            var pos = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);

            var brazier = new GameObject($"Brazier_{i:00}");
            brazier.transform.SetParent(root.transform, false);
            brazier.transform.position = pos;

            // 기둥 받침
            var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            column.name = "Column";
            column.transform.SetParent(brazier.transform, false);
            column.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            column.transform.localScale = new Vector3(0.45f, 0.75f, 0.45f);
            column.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;
            Object.DestroyImmediate(column.GetComponent<CapsuleCollider>());

            // 불이 담기는 그릇
            var bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.name = "Bowl";
            bowl.transform.SetParent(brazier.transform, false);
            bowl.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            bowl.transform.localScale = new Vector3(0.8f, 0.12f, 0.8f);
            bowl.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;
            Object.DestroyImmediate(bowl.GetComponent<CapsuleCollider>());

            // 조명
            var lightGO = new GameObject("Flame Light");
            lightGO.transform.SetParent(brazier.transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.28f);
            light.intensity = 3.4f;
            light.range = 11f;
            light.shadows = LightShadows.None; // 8개 전부 그림자를 켜면 성능이 급락한다
            lightGO.AddComponent<TorchFlicker>();

            BuildFlame(brazier.transform, flameMat);
        }
    }

    static void BuildFlame(Transform parent, Material flameMat)
    {
        var go = new GameObject("Flame");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 1.65f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.75f, 0.25f), new Color(1f, 0.35f, 0.1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.rateOverTime = 22f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.22f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.5f, 0.1f, 0.05f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = flameMat;
    }

    /// <summary>벽 근처에 흩어진 돌무더기 — 빈 바닥이 덜 허전해 보인다.</summary>
    static void BuildRubble(Transform parent, Material stoneMat)
    {
        var root = new GameObject("Rubble");
        root.transform.SetParent(parent, false);

        for (int i = 0; i < 45; i++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float radius = Random.Range(13f, 20f); // 전투 공간(중앙)은 비워둔다
            var pos = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);

            var rock = GameObject.CreatePrimitive(Random.value > 0.35f ? PrimitiveType.Cube : PrimitiveType.Sphere);
            rock.name = "Rock";
            rock.transform.SetParent(root.transform, false);
            float s = Random.Range(0.18f, 0.55f);
            rock.transform.position = pos + new Vector3(0f, s * 0.35f, 0f);
            rock.transform.localScale = new Vector3(s, s * Random.Range(0.5f, 0.9f), s * Random.Range(0.7f, 1.2f));
            rock.transform.rotation = Random.rotation;
            rock.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;
            Object.DestroyImmediate(rock.GetComponent<Collider>()); // 밟고 걸리지 않도록
        }
    }

    /// <summary>벽 위에 높낮이가 다른 성벽 요철을 얹어 실루엣을 만든다.</summary>
    static void BuildWallTops(Transform parent, Material wallMat)
    {
        var root = new GameObject("WallTops");
        root.transform.SetParent(parent, false);

        const int count = 40;
        const float radius = 21f;
        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 1) continue; // 하나 걸러 하나 — 성가퀴 모양

            float angle = i * Mathf.PI * 2f / count;
            var pos = new Vector3(Mathf.Sin(angle) * radius, 6f + 0.6f, Mathf.Cos(angle) * radius);

            var merlon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            merlon.name = $"Merlon_{i:00}";
            merlon.transform.SetParent(root.transform, false);
            merlon.transform.position = pos;
            merlon.transform.rotation = Quaternion.LookRotation(new Vector3(pos.x, 0f, pos.z));
            merlon.transform.localScale = new Vector3(3.4f, Random.Range(1f, 1.6f), 1f);
            merlon.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
            Object.DestroyImmediate(merlon.GetComponent<Collider>());
        }
    }
}
