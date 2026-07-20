using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class Day3Setup
{
    const string ArenaScenePath = "Assets/Scenes/Arena.unity";

    [MenuItem("Tools/Setup Stamina And LockOn")]
    public static void Setup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.GetActiveScene().path == ArenaScenePath
            ? EditorSceneManager.GetActiveScene()
            : EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

        var player = GameObject.Find("PlayerArmature");
        if (player == null)
        {
            Debug.LogError("PlayerArmature를 찾을 수 없습니다. Tools > Build Greybox Arena를 먼저 실행하세요.");
            return;
        }

        var stamina = player.GetComponent<Stamina>();
        if (stamina == null) stamina = player.AddComponent<Stamina>();
        if (player.GetComponent<LockOnSystem>() == null) player.AddComponent<LockOnSystem>();

        // HUD (체력/스태미나 바)
        if (GameObject.Find("HUD") == null)
        {
            var hudGO = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = hudGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = hudGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var hpFill = CreateBar(hudGO.transform, "HealthBar", new Vector2(30, -30), new Vector2(360, 22), new Color(0.75f, 0.15f, 0.12f));
            var spFill = CreateBar(hudGO.transform, "StaminaBar", new Vector2(30, -60), new Vector2(300, 15), new Color(0.3f, 0.65f, 0.2f));

            var hud = hudGO.AddComponent<PlayerHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("playerHealth").objectReferenceValue = player.GetComponent<Health>();
            so.FindProperty("playerStamina").objectReferenceValue = stamina;
            so.FindProperty("healthFill").objectReferenceValue = hpFill;
            so.FindProperty("staminaFill").objectReferenceValue = spFill;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Day 3 셋업 완료: 스태미나 + 락온 + HUD. Play로 테스트하세요.");
    }

    static RectTransform CreateBar(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color fillColor)
    {
        var bg = new GameObject(name, typeof(Image));
        bg.transform.SetParent(parent, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = bgRect.anchorMax = new Vector2(0, 1); // 좌상단 기준
        bgRect.pivot = new Vector2(0, 1);
        bgRect.anchoredPosition = anchoredPos;
        bgRect.sizeDelta = size;
        bg.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        var fill = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(bg.transform, false);
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0, 0.5f); // X 스케일 조절 시 왼쪽부터 줄어들도록
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
        fill.GetComponent<Image>().color = fillColor;

        return fillRect;
    }
}
