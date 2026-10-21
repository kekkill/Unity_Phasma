using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MenuSafeArea : MonoBehaviour
{
    private Rect previous;
    private Vector2Int previousSize;
    private void OnEnable() => Apply();
    private void Update()
    {
        if (previous != Screen.safeArea || previousSize.x != Screen.width || previousSize.y != Screen.height) Apply();
    }
    private void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        previous = Screen.safeArea;
        previousSize = new Vector2Int(Screen.width, Screen.height);
        var panel = (RectTransform)transform;
        panel.anchorMin = new Vector2(previous.xMin / Screen.width, previous.yMin / Screen.height);
        panel.anchorMax = new Vector2(previous.xMax / Screen.width, previous.yMax / Screen.height);
        panel.offsetMin = panel.offsetMax = Vector2.zero;
    }
}
