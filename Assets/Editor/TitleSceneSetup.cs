using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class TitleSceneSetup
{
    public const string TitleScenePath = "Assets/Scenes/Title.unity";
    const string ArenaScenePath = "Assets/Scenes/Arena.unity";

    const string GameTitle = "SOULSLIKE DEMO";

    const string Description =
        "<조작>\n" +
        "이동  W A S D          시점  마우스\n" +
        "공격  마우스 좌클릭     구르기  Space\n" +
        "락온  Q                달리기  Shift\n" +
        "점프  F                커서 해제  Esc\n\n" +
        "<게임 설명>\n" +
        "어둠에 잠긴 아레나에서 변이체와 맞선다.\n" +
        "구르기에는 무적 시간이 있다. 적의 공격을 읽고 굴러 흘린 뒤 반격하라.\n" +
        "공격과 구르기는 스태미나를 소모한다. 다 쓰면 아무것도 할 수 없다.\n" +
        "무리하게 덤비지 말고, 한 대 치고 빠지는 것을 반복하라.";

    [MenuItem("Tools/Setup Title Scene")]
    public static void Setup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        UIBuilder.CreateUICamera(new Color(0.03f, 0.03f, 0.04f));
        UIBuilder.CreateEventSystem();

        var canvas = UIBuilder.CreateCanvas("TitleCanvas");
        var root = canvas.transform;

        // 타이틀 — 빨간 글씨
        var title = UIBuilder.CreateText(root, "Title", GameTitle, 110,
            new Color(0.72f, 0.07f, 0.05f), new Vector2(0f, 250f), new Vector2(1600f, 200f));
        title.fontStyle = FontStyle.Bold;
        var outline = title.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        outline.effectDistance = new Vector2(3f, -3f);

        UIBuilder.CreateText(root, "Subtitle", "졸업작품 · 3인칭 소울라이크", 26,
            new Color(0.55f, 0.52f, 0.48f), new Vector2(0f, 150f), new Vector2(1200f, 50f));

        // 버튼
        var startButton = UIBuilder.CreateButton(root, "StartButton", "시작하기",
            new Vector2(0f, -30f), new Vector2(340f, 78f));
        var descButton = UIBuilder.CreateButton(root, "DescriptionButton", "게임 설명",
            new Vector2(0f, -125f), new Vector2(340f, 78f));

        // 설명 패널 (기본 숨김)
        var panel = UIBuilder.CreatePanel(root, "DescriptionPanel",
            new Color(0.05f, 0.05f, 0.06f, 0.96f), new Vector2(0f, 0f), new Vector2(1000f, 620f));
        var panelText = UIBuilder.CreateText(panel.transform, "Text", Description, 26,
            new Color(0.85f, 0.82f, 0.76f), Vector2.zero, new Vector2(920f, 560f),
            TextAnchor.UpperLeft);
        panelText.lineSpacing = 1.25f;

        var closeButton = UIBuilder.CreateButton(panel.transform, "CloseButton", "닫기",
            new Vector2(0f, -260f), new Vector2(200f, 56f), 26);

        // 스크립트 연결
        var menuGO = new GameObject("TitleMenu");
        var menu = menuGO.AddComponent<TitleMenu>();
        var so = new SerializedObject(menu);
        so.FindProperty("descriptionPanel").objectReferenceValue = panel.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(startButton.onClick, menu.StartGame);
        UnityEventTools.AddPersistentListener(descButton.onClick, menu.ToggleDescription);
        UnityEventTools.AddPersistentListener(closeButton.onClick, menu.ToggleDescription);

        panel.gameObject.SetActive(false);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, TitleScenePath);

        RegisterScenes();

        Debug.Log($"타이틀 씬 생성 완료: {TitleScenePath}\n" +
                  $"제목은 '{GameTitle}' — 바꾸려면 씬에서 Title 오브젝트의 Text를 수정하세요.");
    }

    /// <summary>빌드 씬 목록에 Title(0번) → Arena(1번) 순서로 등록.</summary>
    public static void RegisterScenes()
    {
        var scenes = EditorBuildSettings.scenes.ToList();

        // 중복 제거 후 순서대로 다시 넣는다
        scenes.RemoveAll(s => s.path == TitleScenePath || s.path == ArenaScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(TitleScenePath, true));
        scenes.Insert(1, new EditorBuildSettingsScene(ArenaScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();

        Debug.Log("빌드 씬 등록: " + string.Join(" → ", EditorBuildSettings.scenes.Select(s => Path.GetFileNameWithoutExtension(s.path))));
    }
}
