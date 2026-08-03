using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class DefeatSceneSetup
{
    public const string DefeatScenePath = "Assets/Scenes/Defeat.unity";

    [MenuItem("Tools/Setup Defeat Scene")]
    public static void Setup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        UIBuilder.CreateUICamera(new Color(0.02f, 0.01f, 0.01f));
        UIBuilder.CreateEventSystem();

        var canvas = UIBuilder.CreateCanvas("DefeatCanvas");
        var root = canvas.transform;

        // 패배 문구 — 소울라이크 특유의 검붉은 색
        var title = UIBuilder.CreateText(root, "Title", "YOU DIED", 120,
            new Color(0.62f, 0.09f, 0.06f), new Vector2(0f, 180f), new Vector2(1600f, 220f));
        title.fontStyle = FontStyle.Bold;
        var outline = title.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(3f, -3f);

        UIBuilder.CreateText(root, "Subtitle", "변이체에게 쓰러졌다", 28,
            new Color(0.50f, 0.45f, 0.43f), new Vector2(0f, 80f), new Vector2(1200f, 50f));

        // 버튼
        var retryButton = UIBuilder.CreateButton(root, "RetryButton", "다시하기",
            new Vector2(0f, -80f), new Vector2(340f, 78f));
        var titleButton = UIBuilder.CreateButton(root, "TitleButton", "타이틀로",
            new Vector2(0f, -175f), new Vector2(340f, 78f));

        // 스크립트 연결 (패배 화면에는 클리어 타임을 표시하지 않으므로 비워둔다)
        var menuGO = new GameObject("ResultMenu");
        var menu = menuGO.AddComponent<ResultMenu>();

        UnityEventTools.AddPersistentListener(retryButton.onClick, menu.Retry);
        UnityEventTools.AddPersistentListener(titleButton.onClick, menu.GoToTitle);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, DefeatScenePath);

        RegisterScene();

        Debug.Log("패배 결산 씬 생성 완료: " + DefeatScenePath);
    }

    static void RegisterScene()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.All(s => s.path != DefeatScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(DefeatScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }

        Debug.Log("빌드 씬: " + string.Join(" → ",
            EditorBuildSettings.scenes.Select(s => Path.GetFileNameWithoutExtension(s.path))));
    }
}
