using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class VictorySceneSetup
{
    public const string VictoryScenePath = "Assets/Scenes/Victory.unity";

    [MenuItem("Tools/Setup Victory Scene")]
    public static void Setup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        UIBuilder.CreateUICamera(new Color(0.03f, 0.03f, 0.04f));
        UIBuilder.CreateEventSystem();

        var canvas = UIBuilder.CreateCanvas("VictoryCanvas");
        var root = canvas.transform;

        // 승리 문구 — 금색
        var title = UIBuilder.CreateText(root, "Title", "VICTORY", 110,
            new Color(0.87f, 0.76f, 0.40f), new Vector2(0f, 260f), new Vector2(1600f, 200f));
        title.fontStyle = FontStyle.Bold;
        var outline = title.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        outline.effectDistance = new Vector2(3f, -3f);

        UIBuilder.CreateText(root, "Subtitle", "변이체를 쓰러뜨렸다", 28,
            new Color(0.58f, 0.55f, 0.50f), new Vector2(0f, 170f), new Vector2(1200f, 50f));

        // 클리어 타임
        UIBuilder.CreateText(root, "ClearTimeLabel", "CLEAR TIME", 26,
            new Color(0.60f, 0.57f, 0.52f), new Vector2(0f, 60f), new Vector2(800f, 40f));
        var timeText = UIBuilder.CreateText(root, "ClearTimeValue", "00:00.00", 72,
            new Color(0.92f, 0.89f, 0.83f), new Vector2(0f, -5f), new Vector2(800f, 100f));
        timeText.fontStyle = FontStyle.Bold;

        // 버튼
        var retryButton = UIBuilder.CreateButton(root, "RetryButton", "다시하기",
            new Vector2(0f, -160f), new Vector2(340f, 78f));
        var titleButton = UIBuilder.CreateButton(root, "TitleButton", "타이틀로",
            new Vector2(0f, -255f), new Vector2(340f, 78f));

        // 스크립트 연결
        var menuGO = new GameObject("ResultMenu");
        var menu = menuGO.AddComponent<ResultMenu>();
        var so = new SerializedObject(menu);
        so.FindProperty("clearTimeText").objectReferenceValue = timeText;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(retryButton.onClick, menu.Retry);
        UnityEventTools.AddPersistentListener(titleButton.onClick, menu.GoToTitle);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, VictoryScenePath);

        RegisterScene();

        Debug.Log("승리 결산 씬 생성 완료: " + VictoryScenePath);
    }

    static void RegisterScene()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.All(s => s.path != VictoryScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(VictoryScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }

        Debug.Log("빌드 씬: " + string.Join(" → ",
            EditorBuildSettings.scenes.Select(s => Path.GetFileNameWithoutExtension(s.path))));
    }
}
