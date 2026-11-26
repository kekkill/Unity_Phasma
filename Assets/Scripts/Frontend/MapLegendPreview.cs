using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MapLegendPreview : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Text legend;
    private string preview;
    private string selected;

    public void Bind(Text label, string previewText, string selectedText)
    {
        legend = label;
        preview = previewText;
        selected = selectedText;
    }

    public void OnPointerEnter(PointerEventData eventData) => legend.text = preview;
    public void OnPointerExit(PointerEventData eventData) => legend.text = selected;
}
