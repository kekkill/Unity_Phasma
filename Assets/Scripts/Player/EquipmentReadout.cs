using UnityEngine;

// Temporary read-only instrument presentation. State and Input System actions remain elsewhere.
[RequireComponent(typeof(PlayerItemHolder), typeof(PlayerInputReader))]
public class EquipmentReadout : MonoBehaviour
{
    private PlayerItemHolder holder;
    private PlayerInputReader input;
    private GUIStyle style;
    private void Awake()
    {
        holder = GetComponent<PlayerItemHolder>();
        input = GetComponent<PlayerInputReader>();
    }
    private void OnGUI()
    {
        if (input.GameplayBlocked || !holder.Inventory.CanModify || holder.CurrentItem == null) return;
        PickupItem item = holder.CurrentItem;
        if (item.State != PickupItemState.Held) return;
        EMFReaderItem emf = item.GetComponent<EMFReaderItem>();
        ThermometerItem thermometer = item.GetComponent<ThermometerItem>();
        string label;
        if (emf != null)
            label = "EMF: " + emf.CurrentLevel + (emf.HasEmfFiveReading ? " · EMF-5: улика" :
                emf.CurrentLevel == 1 ? " · нет активности" : " · обычная активность");
        else if (thermometer != null)
            label = thermometer.HasReading ? thermometer.LastRoomName + ": " + thermometer.LastTemperature.ToString("F1") +
                " °C · F — измерить снова" : "Термометр · F — измерить";
        else return;
        if (style == null) style = new GUIStyle(GUI.skin.box)
            { fontSize = 17, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        float width = Mathf.Min(540f, Mathf.Max(1f, Screen.width - 20f));
        GUI.Box(new Rect((Screen.width - width) * 0.5f, Screen.height - 220f, width, 48f), label, style);
    }
}
