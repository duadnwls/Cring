using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>메뉴 씬들을 코드로 조립할 때 쓰는 UI 생성 헬퍼.</summary>
public static class UIBuilder
{
    public static Font DefaultFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    public static Canvas CreateCanvas(string name)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    public static Text CreateText(Transform parent, string name, string content, int fontSize,
                                  Color color, Vector2 anchoredPos, Vector2 size,
                                  TextAnchor align = TextAnchor.MiddleCenter)
    {
        var go = new GameObject(name, typeof(Text));
        go.transform.SetParent(parent, false);

        var text = go.GetComponent<Text>();
        text.font = DefaultFont;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = align;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        return text;
    }

    public static Button CreateButton(Transform parent, string name, string label,
                                      Vector2 anchoredPos, Vector2 size, int fontSize = 34)
    {
        var go = new GameObject(name, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = new Color(0.11f, 0.10f, 0.10f, 0.92f);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.72f, 0.62f);
        colors.pressedColor = new Color(0.75f, 0.30f, 0.25f);
        colors.selectedColor = new Color(1f, 0.72f, 0.62f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var text = CreateText(go.transform, "Label", label, fontSize,
                              new Color(0.90f, 0.86f, 0.80f), Vector2.zero, size);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    public static Image CreatePanel(Transform parent, string name, Color color,
                                    Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        return go.GetComponent<Image>();
    }

    /// <summary>메뉴 씬용 단색 배경 카메라.</summary>
    public static Camera CreateUICamera(Color background)
    {
        var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        go.tag = "MainCamera";

        var cam = go.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.orthographic = true;

        return cam;
    }

    public static void CreateEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;

        new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
    }
}
