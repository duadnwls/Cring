using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildSetup
{
    const string ArenaScenePath = "Assets/Scenes/Arena.unity";
    const string BuildFolder = "Builds/SoulslikeDemo";
    const string ExeName = "SoulslikeDemo.exe";

    [MenuItem("Tools/Configure Build Settings")]
    public static void Configure()
    {
        if (!File.Exists(ArenaScenePath))
        {
            Debug.LogError("Arena 씬을 찾을 수 없습니다: " + ArenaScenePath);
            return;
        }

        // 1) 빌드 씬 목록을 Arena 하나로 교체 (기존 SampleScene 항목은 이미 삭제된 씬을 가리킴)
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ArenaScenePath, true) };

        // 2) 플레이어 설정 — 발표용
        PlayerSettings.companyName = "GraduationProject";
        PlayerSettings.productName = "Soulslike Demo";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow; // 테두리 없는 전체화면 — Alt+Tab이 편함
        PlayerSettings.defaultIsNativeResolution = true;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
            new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });

        // 설정을 디스크에 강제로 기록 (ProjectSettings는 평소 지연 저장되어 확인이 어렵다)
        AssetDatabase.SaveAssets();
        EditorApplication.ExecuteMenuItem("File/Save Project");

        // 3) 빌드에서 Esc로 커서를 풀 수 있게 씬에 컴포넌트 배치
        // 씬을 여는 도중 에디터 내부 예외(Animator 그래프 등)가 나도 위 설정은 이미 저장되도록 분리한다
        try
        {
            var scene = EditorSceneManager.GetActiveScene().path == ArenaScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

            if (Object.FindFirstObjectByType<PauseAndCursor>() == null)
            {
                var gm = GameObject.Find("GameManager");
                if (gm != null) gm.AddComponent<PauseAndCursor>();
                else new GameObject("PauseAndCursor").AddComponent<PauseAndCursor>();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("씬에 PauseAndCursor를 넣지 못했습니다. Arena 씬을 직접 열고 다시 실행하세요.\n" + e);
        }

        // 실제로 반영됐는지 되읽어서 확인
        bool sceneOk = EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ArenaScenePath);
        Debug.Log($"빌드 설정 {(sceneOk ? "완료" : "실패")}:\n" +
                  $"  빌드 씬: {string.Join(", ", EditorBuildSettings.scenes.Select(s => s.path))}\n" +
                  $"  제품명: {PlayerSettings.productName}\n" +
                  $"  해상도: {PlayerSettings.defaultScreenWidth}x{PlayerSettings.defaultScreenHeight}, {PlayerSettings.fullScreenMode}\n" +
                  $"  Esc 커서 해제: {(Object.FindFirstObjectByType<PauseAndCursor>() != null ? "설치됨" : "미설치")}");
    }

    [MenuItem("Tools/Build Windows Player")]
    public static void BuildPlayer()
    {
        if (EditorBuildSettings.scenes.Length == 0 ||
            !EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ArenaScenePath))
        {
            Debug.LogError("빌드 씬 목록이 올바르지 않습니다. 먼저 Tools > Configure Build Settings를 실행하세요.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Directory.CreateDirectory(BuildFolder);

        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = Path.Combine(BuildFolder, ExeName),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"빌드 성공! {summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalSeconds:F0}초\n" +
                      $"위치: {Path.GetFullPath(options.locationPathName)}");
            EditorUtility.RevealInFinder(options.locationPathName);
        }
        else
        {
            Debug.LogError($"빌드 실패: {summary.result}, 에러 {summary.totalErrors}개");
        }
    }
}
