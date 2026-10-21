using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Keep keyboard/gamepad selection visible inside a long preparation page.
public class MenuFocusReveal : MonoBehaviour, ISelectHandler
{
    public void OnSelect(BaseEventData eventData)
    {
        var scroll = GetComponentInParent<ScrollRect>();
        if (scroll == null || scroll.content == null || scroll.viewport == null) return;
        Canvas.ForceUpdateCanvases();
        float overflow = scroll.content.rect.height - scroll.viewport.rect.height;
        if (overflow <= 0) return;
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, transform);
        Rect viewport = scroll.viewport.rect;
        float delta = bounds.min.y < viewport.yMin ? viewport.yMin - bounds.min.y :
            bounds.max.y > viewport.yMax ? viewport.yMax - bounds.max.y : 0;
        Vector2 position = scroll.content.anchoredPosition;
        position.y = Mathf.Clamp(position.y + delta, 0, overflow);
        scroll.StopMovement();
        scroll.content.anchoredPosition = position;
    }
}
