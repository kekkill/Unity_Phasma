using UnityEngine;

// Read-only local HUD. Does not handle input or change inventory/item state.
[RequireComponent(typeof(PlayerInventory), typeof(PlayerInputReader))]
public class InventoryScreen : MonoBehaviour
{
    private PlayerInventory inventory;
    private PlayerInputReader input;
    private GUIStyle slotStyle, selectedStyle, hintStyle;
    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        input = GetComponent<PlayerInputReader>();
    }
    private static string ItemLabel(PickupItem item)
    {
        if (item == null) return "Пусто";
        CrossItem cross = item.GetComponent<CrossItem>();
        if (cross != null) return cross.IsBroken ? "Крест · сломан" : "Крест";
        SaltContainer salt = item.GetComponent<SaltContainer>();
        if (salt != null) return "Соль · " + salt.Portions;
        if (item.GetComponent<UVFlashlightItem>() != null) return "УФ-фонарик";
        if (item.GetComponent<FlashlightItem>() != null) return "Фонарик";
        if (item.GetComponent<ThermometerItem>() != null) return "Термометр";
        if (item.GetComponent<EMFReaderItem>() != null) return "EMF-прибор";
        if (item.GetComponent<GhostWritingBook>() != null) return "Книга";
        if (item.GetComponent<MotionSensorItem>() != null) return "Датчик движения";
        return item.name;
    }
    private void OnGUI()
    {
        if (input.GameplayBlocked || !inventory.CanModify) return;
        if (slotStyle == null)
        {
            slotStyle = new GUIStyle(GUI.skin.box) { fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            selectedStyle = new GUIStyle(slotStyle) { fontStyle = FontStyle.Bold };
            selectedStyle.normal.textColor = Color.yellow;
            hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        }
        float width = Mathf.Min(540f, Mathf.Max(1f, Screen.width - 20f));
        float slotWidth = (width - 12f) / PlayerInventory.SlotCount;
        float left = (Screen.width - width) * 0.5f;
        float top = Screen.height - 160f;
        for (int i = 0; i < PlayerInventory.SlotCount; i++)
            GUI.Box(new Rect(left + i * (slotWidth + 6f), top, slotWidth, 48f),
                (i + 1) + " · " + ItemLabel(inventory.GetItem(i)), i == inventory.SelectedSlot ? selectedStyle : slotStyle);
        GUI.Label(new Rect(left, top + 50f, width, 25f), "1 / 2 / 3 — выбрать     E — подобрать     Q — выбросить", hintStyle);
    }
}
