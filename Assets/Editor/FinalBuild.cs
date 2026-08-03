using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 빌드 전 정리 + 검증. 디버그용 컴포넌트와 로그를 끄고,
/// 씬 4개가 올바른 순서로 등록됐는지 확인한다.
/// </summary>
public static class FinalBuild
{
    const string ArenaScenePath = "Assets/Scenes/Arena.unity";

    static readonly string[] ExpectedScenes =
    {
        "Assets/Scenes/Title.unity",
        "Assets/Scenes/Arena.unity",
        "Assets/Scenes/Victory.unity",
        "Assets/Scenes/Defeat.unity",
    };

    [MenuItem("Tools/Prepare For Build")]
    public static void Prepare()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.GetActiveScene().path == ArenaScenePath
            ? EditorSceneManager.GetActiveScene()
            : EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

        var report = new List<string>();

        // 1) 보스 판정 디버그 로그 끄기
        var boss = GameObject.Find("Boss");
        if (boss != null)
        {
            var ai = boss.GetComponent<BossAI>();
            if (ai != null)
            {
                var so = new SerializedObject(ai);
                var prop = so.FindProperty("debugHits");
                if (prop.boolValue)
                {
                    prop.boolValue = false;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    report.Add("보스 판정 디버그 로그 끔");
                }
            }

            // 2) 접지 측정용 임시 컴포넌트 제거
            var probe = boss.GetComponent<BossGroundingProbe>();
            if (probe != null)
            {
                Object.DestroyImmediate(probe);
                report.Add("BossGroundingProbe 제거 (측정용 임시 컴포넌트)");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // 3) 빌드 씬 검증
        var registered = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var missing = ExpectedScenes.Where(e => !registered.Contains(e)).ToArray();
        var problems = new List<string>();

        if (missing.Length > 0)
            problems.Add("빌드 목록에 없는 씬: " + string.Join(", ", missing.Select(Path.GetFileNameWithoutExtension)));

        if (registered.Length > 0 && registered[0] != ExpectedScenes[0])
            problems.Add($"첫 번째 씬이 Title이 아닙니다: {Path.GetFileNameWithoutExtension(registered[0])}");

        foreach (var path in registered)
        {
            if (!File.Exists(path))
                problems.Add("파일이 없는 씬이 등록돼 있습니다: " + path);
        }

        string summary = "빌드 준비 결과\n";
        summary += "  정리: " + (report.Count > 0 ? string.Join(", ", report) : "변경 없음") + "\n";
        summary += "  씬 순서: " + string.Join(" → ", registered.Select(Path.GetFileNameWithoutExtension)) + "\n";
        summary += $"  제품명: {PlayerSettings.productName} / {PlayerSettings.defaultScreenWidth}x{PlayerSettings.defaultScreenHeight}";

        if (problems.Count > 0)
            Debug.LogError(summary + "\n\n문제:\n  " + string.Join("\n  ", problems));
        else
            Debug.Log(summary + "\n\n문제 없음. Tools > Build Windows Player로 빌드하세요.");
    }
}
