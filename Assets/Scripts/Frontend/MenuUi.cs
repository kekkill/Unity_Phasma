using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Presentation helpers only; these controls do not own mission or inventory state.
public static class MenuUi
{
    private static readonly Color TextColor = new Color(0.91f, 0.94f, 0.97f);
    private static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    public static GameObject Create(Transform owner, string title, out RectTransform content, out CanvasGroup controls)
    {
        EnsureEventSystem();
        var root = new GameObject(title, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        root.transform.SetParent(owner, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 200;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        controls = root.GetComponent<CanvasGroup>();
        Image background = Image(root.transform, "Background", new Color(0.025f, 0.035f, 0.055f, 0.97f));
        Stretch(background.rectTransform);
        RectTransform safe = Rect(root.transform, "Safe area");
        safe.gameObject.AddComponent<MenuSafeArea>();
        RectTransform frame = Rect(safe, "Panel");
        frame.anchorMin = new Vector2(0.08f, 0.04f);
        frame.anchorMax = new Vector2(0.92f, 0.96f);
        frame.offsetMin = frame.offsetMax = Vector2.zero;
        var viewportImage = Image(frame, "Viewport", new Color(0.07f, 0.09f, 0.13f));
        Stretch(viewportImage.rectTransform);
        viewportImage.rectTransform.offsetMax = new Vector2(-20, 0);
        viewportImage.gameObject.AddComponent<RectMask2D>();
        content = Rect(viewportImage.transform, "Content");
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 20);
        layout.spacing = 12; layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = frame.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewportImage.rectTransform; scroll.content = content;
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        Image bar = Image(frame, "Scroll bar", new Color(0.10f, 0.14f, 0.20f));
        bar.rectTransform.anchorMin = new Vector2(1, 0); bar.rectTransform.anchorMax = Vector2.one;
        bar.rectTransform.sizeDelta = new Vector2(12, -16); bar.rectTransform.anchoredPosition = new Vector2(-8, 0);
        Image thumb = Image(bar.transform, "Handle", new Color(0.35f, 0.48f, 0.60f));
        Stretch(thumb.rectTransform);
        var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = thumb.rectTransform; scrollbar.targetGraphic = thumb;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        Label(content, title, 30);
        return root;
    }
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var events = new GameObject("Menu EventSystem", typeof(EventSystem));
        events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }
    public static Text Label(Transform parent, string value, int size = 22)
    {
        var rect = Rect(parent, "Text");
        var text = rect.gameObject.AddComponent<Text>();
        text.font = Font; text.text = value; text.fontSize = size; text.color = TextColor;
        text.supportRichText = false; text.raycastTarget = false;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
    public static Button Button(Transform parent, string label, UnityAction action)
    {
        var image = Image(parent, label, new Color(0.16f, 0.23f, 0.33f));
        Height(image.gameObject, 54);
        var button = image.gameObject.AddComponent<Button>();
        button.gameObject.AddComponent<MenuFocusReveal>();
        button.targetGraphic = image; button.onClick.AddListener(action);
        Text text = Label(image.transform, label);
        Stretch(text.rectTransform, 12); text.alignment = TextAnchor.MiddleCenter;
        return button;
    }
    public static Toggle Toggle(Transform parent, string label, bool selected, UnityAction<bool> action)
    {
        var row = Image(parent, label, new Color(0.10f, 0.14f, 0.20f));
        Height(row.gameObject, 54);
        var toggle = row.gameObject.AddComponent<Toggle>();
        toggle.gameObject.AddComponent<MenuFocusReveal>();
        toggle.targetGraphic = row;
        Image box = Image(row.transform, "Box", new Color(0.25f, 0.32f, 0.4f));
        box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(0, 0.5f);
        box.rectTransform.sizeDelta = new Vector2(26, 26); box.rectTransform.anchoredPosition = new Vector2(24, 0);
        Image check = Image(box.transform, "Selected", new Color(0.35f, 0.85f, 0.78f));
        Stretch(check.rectTransform, 5); toggle.graphic = check;
        Text text = Label(row.transform, label); Stretch(text.rectTransform, 8);
        text.rectTransform.offsetMin = new Vector2(50, 8);
        toggle.SetIsOnWithoutNotify(selected); toggle.onValueChanged.AddListener(action);
        return toggle;
    }
    public static Slider Slider(Transform parent, float value, float min, float max, UnityAction<float> action)
    {
        RectTransform row = Rect(parent, "Slider"); Height(row.gameObject, 54);
        Image track = Image(row, "Track", new Color(0.22f, 0.28f, 0.36f));
        Stretch(track.rectTransform, 12);
        RectTransform handleArea = Rect(row, "Handle area"); Stretch(handleArea, 12);
        Image handle = Image(handleArea, "Handle", new Color(0.35f, 0.85f, 0.78f));
        handle.rectTransform.sizeDelta = new Vector2(28, 0);
        var slider = row.gameObject.AddComponent<Slider>();
        slider.gameObject.AddComponent<MenuFocusReveal>();
        slider.targetGraphic = handle; slider.handleRect = handle.rectTransform;
        slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.minValue = min; slider.maxValue = max; slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(action);
        return slider;
    }
    public static void Select(Selectable control)
    {
        if (EventSystem.current != null && control != null) EventSystem.current.SetSelectedGameObject(control.gameObject);
    }
    private static RectTransform Rect(Transform parent, string name)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); return rect;
    }
    private static Image Image(Transform parent, string name, Color color)
    {
        Image image = Rect(parent, name).gameObject.AddComponent<Image>(); image.color = color; return image;
    }
    private static void Stretch(RectTransform rect, float inset = 0)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset); rect.offsetMax = new Vector2(-inset, -inset);
    }
    private static void Height(GameObject element, float height)
    {
        var layout = element.AddComponent<LayoutElement>(); layout.minHeight = height; layout.preferredHeight = height;
    }
}
