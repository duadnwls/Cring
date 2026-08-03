using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 보스가 지면에서 떠서 움직이는 문제 수정.
/// 원인: Mixamo 클립의 Root Transform Position Y가 "Original" 기준이라
/// 원본 체형에 맞춰진 높이가 체격이 다른 Mutant에 그대로 적용됨.
/// </summary>
public static class BossGroundingFix
{
    const string BossFolder = "Assets/Animation/Boss";
    const string ArenaScenePath = "Assets/Scenes/Arena.unity";

    /// <summary>
    /// NavMesh는 복셀화 과정 때문에 실제 바닥보다 살짝 위에 생성된다(측정값 8.3cm).
    /// 복셀을 잘게 다시 굽고, 남은 높이만큼 NavMeshAgent.baseOffset으로 상쇄한다.
    /// </summary>
    [MenuItem("Tools/Fix Boss Float (NavMesh Height)")]
    public static void FixNavMeshHeight()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.GetActiveScene().path == ArenaScenePath
            ? EditorSceneManager.GetActiveScene()
            : EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

        var arena = GameObject.Find("Arena");
        var boss = GameObject.Find("Boss");
        if (arena == null || boss == null)
        {
            Debug.LogError("Arena 또는 Boss를 찾을 수 없습니다.");
            return;
        }

        var sb = new StringBuilder();

        // 1) 복셀을 잘게 해서 NavMesh를 바닥에 더 가깝게 굽는다
        var surface = arena.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
        if (surface != null)
        {
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.05f;   // 기본값(반지름/3 ≈ 0.167)보다 훨씬 정밀
            surface.overrideTileSize = true;
            surface.tileSize = 256;
            surface.BuildNavMesh();
            sb.AppendLine("NavMesh를 복셀 0.05로 재베이크했습니다.");
        }
        else
        {
            sb.AppendLine("경고: Arena에 NavMeshSurface가 없어 재베이크를 건너뜁니다.");
        }

        // 2) 새 NavMesh의 실제 높이를 측정
        float navHeight = 0f;
        if (NavMesh.SamplePosition(new Vector3(boss.transform.position.x, 1f, boss.transform.position.z),
                                   out var hit, 10f, NavMesh.AllAreas))
        {
            navHeight = hit.position.y;
            sb.AppendLine($"보스 위치의 NavMesh 높이: {navHeight:F4} (바닥은 0)");
        }
        else
        {
            sb.AppendLine("경고: NavMesh 샘플링에 실패했습니다.");
        }

        // 3) 남은 높이를 baseOffset으로 상쇄 → 루트가 실제 바닥에 놓인다
        var agent = boss.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.baseOffset = -navHeight;
            sb.AppendLine($"NavMeshAgent.baseOffset = {agent.baseOffset:F4}");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine("\nPlay로 확인하세요. 측정 컴포넌트가 붙어 있으면 rootY가 0에 가까워야 합니다.");
        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Add Boss Grounding Probe")]
    public static void AddProbe()
    {
        var boss = GameObject.Find("Boss");
        if (boss == null)
        {
            Debug.LogError("Boss를 찾을 수 없습니다.");
            return;
        }

        if (boss.GetComponent<BossGroundingProbe>() == null)
            boss.AddComponent<BossGroundingProbe>();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("측정 컴포넌트를 보스에 추가했습니다. Play를 눌러 보스에게 다가가 10초 정도 싸워주세요.");
    }

    [MenuItem("Tools/Fix Boss Grounding")]
    public static void Fix()
    {
        // 1) 보스 클립의 루트 Y 기준을 발로 변경
        int fixedClips = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { BossFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            var clips = importer.defaultClipAnimations;
            if (clips.Length == 0) continue;

            foreach (var clip in clips)
            {
                clip.name = Path.GetFileNameWithoutExtension(path);
                clip.lockRootHeightY = true;
                clip.keepOriginalPositionY = false;
                clip.heightFromFeet = true;
                string lower = clip.name.ToLower();
                clip.loopTime = lower.Contains("idle") || lower.Contains("walk") || lower.Contains("run");
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            fixedClips++;
        }

        // 2) 씬에서 NavMeshAgent 오프셋 정리 + 실제 높이 측정
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.GetActiveScene().path == ArenaScenePath
            ? EditorSceneManager.GetActiveScene()
            : EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

        var boss = GameObject.Find("Boss");
        if (boss == null)
        {
            Debug.LogError("Boss를 찾을 수 없습니다.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"보스 클립 {fixedClips}개를 '발 기준' 높이로 재임포트했습니다.");

        var agent = boss.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            if (!Mathf.Approximately(agent.baseOffset, 0f))
            {
                sb.AppendLine($"  NavMeshAgent.baseOffset {agent.baseOffset} → 0 으로 수정");
                agent.baseOffset = 0f;
            }
            else
            {
                sb.AppendLine("  NavMeshAgent.baseOffset 은 이미 0 입니다.");
            }
        }

        // 현재 발 높이 보고 — 0에 가까워야 정상
        var lowestFoot = FindLowestFootY(boss.transform);
        sb.AppendLine($"  보스 루트 Y: {boss.transform.position.y:F3}");
        if (lowestFoot.HasValue)
            sb.AppendLine($"  발(ToeBase) 최저 Y: {lowestFoot.Value:F3}  ← 0에 가까우면 정상");

        var smr = boss.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null)
            sb.AppendLine($"  메시 바운즈 최저 Y: {smr.bounds.min.y:F3}");

        sb.AppendLine("\nPlay를 눌러 보스가 걸을 때 발이 땅에 닿는지 확인하세요.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log(sb.ToString());
    }

    static float? FindLowestFootY(Transform root)
    {
        var feet = root.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.Contains("ToeBase") || t.name.Contains("Foot"))
            .ToArray();
        if (feet.Length == 0) return null;
        return feet.Min(t => t.position.y);
    }
}
