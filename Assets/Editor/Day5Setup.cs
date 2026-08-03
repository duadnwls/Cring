using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class Day5Setup
{
    const string ArenaScenePath = "Assets/Scenes/Arena.unity";

    [MenuItem("Tools/Setup Game Loop")]
    public static void Setup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.GetActiveScene().path == ArenaScenePath
            ? EditorSceneManager.GetActiveScene()
            : EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

        var player = GameObject.Find("PlayerArmature");
        var bossGO = GameObject.Find("Boss");
        if (player == null || bossGO == null)
        {
            Debug.LogError("PlayerArmature 또는 Boss를 찾을 수 없습니다. 이전 셋업 메뉴를 먼저 실행하세요.");
            return;
        }

        var playerHealth = player.GetComponent<Health>();
        var boss = bossGO.GetComponent<BossAI>();
        var bossHealth = bossGO.GetComponent<Health>();

        // 역할이 끝난 연습용 더미 제거
        var dummy = GameObject.Find("TrainingDummy");
        if (dummy != null) Object.DestroyImmediate(dummy);

        var hud = GameObject.Find("HUD");
        if (hud == null)
        {
            Debug.LogError("HUD를 찾을 수 없습니다. Tools > Setup Stamina And LockOn을 먼저 실행하세요.");
            return;
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ── 보스 체력바 (화면 하단 중앙) ──────────────────────────────
        var oldBar = GameObject.Find("BossHealthBar");
        if (oldBar != null) Object.DestroyImmediate(oldBar);

        var barRoot = new GameObject("BossHealthBar", typeof(RectTransform), typeof(CanvasGroup));
        barRoot.transform.SetParent(hud.transform, false);
        var barRootRect = barRoot.GetComponent<RectTransform>();
        barRootRect.anchorMin = barRootRect.anchorMax = new Vector2(0.5f, 0f);
        barRootRect.pivot = new Vector2(0.5f, 0f);
        barRootRect.anchoredPosition = new Vector2(0f, 70f);
        barRootRect.sizeDelta = new Vector2(900f, 60f);

        var barBg = CreateImage(barRoot.transform, "Background", new Color(0.05f, 0.05f, 0.05f, 0.85f));
        Stretch(barBg, new Vector2(0f, 0f), new Vector2(900f, 24f), new Vector2(0.5f, 0f));

        var delayed = CreateImage(barBg.transform, "DelayedFill", new Color(0.9f, 0.85f, 0.8f, 0.7f));
        FillParent(delayed, 2f, leftPivot: true);
        var fill = CreateImage(barBg.transform, "Fill", new Color(0.66f, 0.11f, 0.09f));
        FillParent(fill, 2f, leftPivot: true);

        var nameGO = new GameObject("Name", typeof(Text));
        nameGO.transform.SetParent(barRoot.transform, false);
        var nameText = nameGO.GetComponent<Text>();
        nameText.font = font;
        nameText.fontSize = 22;
        nameText.color = new Color(0.85f, 0.82f, 0.75f);
        nameText.alignment = TextAnchor.LowerCenter;
        var nameRect = nameGO.GetComponent<RectTransform>();
        nameRect.anchorMin = nameRect.anchorMax = new Vector2(0.5f, 0f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.anchoredPosition = new Vector2(0f, 30f);
        nameRect.sizeDelta = new Vector2(900f, 30f);

        var barScript = barRoot.AddComponent<BossHealthBar>();
        var barSO = new SerializedObject(barScript);
        barSO.FindProperty("boss").objectReferenceValue = boss;
        barSO.FindProperty("bossHealth").objectReferenceValue = bossHealth;
        barSO.FindProperty("group").objectReferenceValue = barRoot.GetComponent<CanvasGroup>();
        barSO.FindProperty("fill").objectReferenceValue = fill;
        barSO.FindProperty("delayedFill").objectReferenceValue = delayed;
        barSO.FindProperty("nameText").objectReferenceValue = nameText;
        barSO.ApplyModifiedPropertiesWithoutUndo();

        // ── YOU DIED / VICTORY 화면 ─────────────────────────────────
        var oldEnd = GameObject.Find("EndScreen");
        if (oldEnd != null) Object.DestroyImmediate(oldEnd);

        var endRoot = new GameObject("EndScreen", typeof(RectTransform));
        endRoot.transform.SetParent(hud.transform, false);
        FillParent(endRoot.GetComponent<RectTransform>(), 0f);

        var backdrop = CreateImage(endRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0f));
        FillParent(backdrop, 0f);
        backdrop.GetComponent<Image>().raycastTarget = false;

        var msgGO = new GameObject("Message", typeof(Text), typeof(Outline));
        msgGO.transform.SetParent(endRoot.transform, false);
        var msgText = msgGO.GetComponent<Text>();
        msgText.font = font;
        msgText.fontSize = 96;
        msgText.fontStyle = FontStyle.Bold;
        msgText.alignment = TextAnchor.MiddleCenter;
        msgText.color = new Color(0.62f, 0.09f, 0.06f, 0f);
        msgText.raycastTarget = false;
        msgGO.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
        var msgRect = msgGO.GetComponent<RectTransform>();
        msgRect.anchorMin = msgRect.anchorMax = new Vector2(0.5f, 0.5f);
        msgRect.pivot = new Vector2(0.5f, 0.5f);
        msgRect.anchoredPosition = Vector2.zero;
        msgRect.sizeDelta = new Vector2(1400f, 200f);

        var endScreen = endRoot.AddComponent<GameEndScreen>();
        var endSO = new SerializedObject(endScreen);
        endSO.FindProperty("backdrop").objectReferenceValue = backdrop.GetComponent<Image>();
        endSO.FindProperty("messageText").objectReferenceValue = msgText;
        endSO.ApplyModifiedPropertiesWithoutUndo();

        // ── GameManager ─────────────────────────────────────────────
        var gmGO = GameObject.Find("GameManager");
        if (gmGO == null) gmGO = new GameObject("GameManager");
        var gm = gmGO.GetComponent<GameManager>();
        if (gm == null) gm = gmGO.AddComponent<GameManager>();

        var gmSO = new SerializedObject(gm);
        gmSO.FindProperty("playerHealth").objectReferenceValue = playerHealth;
        gmSO.FindProperty("boss").objectReferenceValue = boss;
        gmSO.FindProperty("bossHealth").objectReferenceValue = bossHealth;
        gmSO.FindProperty("endScreen").objectReferenceValue = endScreen;
        gmSO.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("게임 루프 셋업 완료! 죽으면 YOU DIED 후 리스폰, 보스 처치 시 VICTORY.");
    }

    static RectTransform CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    /// <summary>부모를 가득 채운다. 체력바 Fill은 X 스케일로 줄어들어야 하므로 pivot을 왼쪽(0)으로 둔다.</summary>
    static void FillParent(RectTransform rect, float padding, bool leftPivot = false)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = leftPivot ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    static void Stretch(RectTransform rect, Vector2 anchoredPos, Vector2 size, Vector2 anchor)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
    }
}
