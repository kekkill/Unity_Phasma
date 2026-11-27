using System;
using UnityEngine;

public sealed class ArchiveMapBoard : MonoBehaviour, IInteractable
{
    [Serializable]
    private class Floor
    {
        public string title;
        public Texture2D plan;
        public Vector2 minimum;
        public Vector2 maximum;
    }

    [Serializable]
    private class Hint
    {
        public int reserveIndex;
        public int floor;
        public Collider room;
        public Vector2 marker;
    }

    [SerializeField] private MissionSceneSetup mission;
    [SerializeField] private Transform breaker;
    [SerializeField] private Texture2D icons;
    [SerializeField] private Floor[] floors;
    [SerializeField] private Hint[] hints;

    private const int MainFloor = 1;
    private const int BreakerIcon = 8;
    private const float IconCount = 9f;
    private PlayerInputReader viewer;
    private int selectedFloor = MainFloor;
    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle noteStyle;

    public string InteractionPrompt => "[E] Посмотреть план дома";

    public void Interact(GameObject interactor)
    {
        if (viewer != null || !interactor.TryGetComponent(out PlayerInputReader input)) return;
        viewer = input;
        selectedFloor = MainFloor;
        viewer.SetGameplayBlocked(this, true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (viewer != null && viewer.CloseJournalPressedThisFrame) Close();
    }

    private void Close()
    {
        if (viewer == null) return;
        viewer.SetGameplayBlocked(this, false);
        viewer = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable() => Close();

    private void OnGUI()
    {
        if (viewer == null) return;
        titleStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
        textStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
        noteStyle ??= new GUIStyle(textStyle) { fontSize = 15 };
        Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.025f, 0.04f, 0.056f, 0.88f));

        const float width = 890f;
        const float height = 552f;
        float scale = Mathf.Min(1.35f, (Screen.width - 20f) / width, (Screen.height - 20f) / height);
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(
            (Screen.width - width * scale) * 0.5f, (Screen.height - height * scale) * 0.5f),
            Quaternion.identity, new Vector3(scale, scale, 1f));
        Fill(new Rect(0, 0, width, height), new Color(0.065f, 0.104f, 0.135f));
        GUI.Label(new Rect(25, 18, 410, 40), "План архивного дома", titleStyle);

        if (FloorButton(new Rect(536, 22, 115, 32), "↓  Подвал", 0))
            selectedFloor = 0;
        if (FloorButton(new Rect(659, 22, 128, 32), "↑  Первый этаж", MainFloor))
            selectedFloor = MainFloor;
        if (GUI.Button(new Rect(802, 22, 64, 32), "Закрыть"))
            Close();

        Floor floor = floors[selectedFloor];
        GUI.Label(new Rect(26, 62, 320, 30), floor.title, textStyle);
        var mapRect = new Rect(24, 100, 624, 416);
        GUI.DrawTexture(mapRect, floor.plan);
        DrawLegend(mapRect, floor);
        GUI.matrix = previous;
    }

    private bool FloorButton(Rect at, string label, int floor)
    {
        bool enabled = GUI.enabled;
        GUI.enabled = selectedFloor != floor;
        bool clicked = GUI.Button(at, label);
        GUI.enabled = enabled;
        if (clicked) WorldAudio.Play(SoundCue.MapPageClick, transform.position, 0.24f, spatial: false);
        return clicked;
    }

    private void DrawLegend(Rect mapRect, Floor floor)
    {
        float y = 111f;
        GUI.Label(new Rect(668, y, 196, 29), "На этом этаже", textStyle);
        y += 40f;

        if (selectedFloor == MainFloor)
        {
            Vector3 at = breaker.position;
            DrawMarker(mapRect, floor, new Vector2(at.x, at.z), BreakerIcon);
            LegendRow(BreakerIcon, "Щиток", ref y);
        }

        int limit = Mathf.Min(hints.Length, mission.Difficulty != null ? mission.Difficulty.EquipmentMapHints : 2);
        bool hasHint = false;
        for (int i = 0; i < limit; i++)
        {
            Hint hint = hints[i];
            if (hint.floor != selectedFloor || !mission.TryGetReserve(hint.reserveIndex, out var id, out var item) ||
                !item.isActiveAndEnabled || item.InventoryOwner != null || !hint.room.bounds.Contains(item.transform.position))
                continue;
            DrawMarker(mapRect, floor, hint.marker, (int)id);
            LegendRow((int)id, mission.EquipmentName(id), ref y);
            hasHint = true;
        }
        if (!hasHint) GUI.Label(new Rect(668, y, 188, 64), "Нет отметок снаряжения на этом этаже.", noteStyle);
        GUI.Label(new Rect(668, 450, 188, 66), $"Подсказки по сложности: {limit} на оба этажа.", noteStyle);
    }

    private void DrawMarker(Rect mapRect, Floor floor, Vector2 at, int icon)
    {
        float px = Mathf.InverseLerp(floor.minimum.x, floor.maximum.x, at.x);
        float py = Mathf.InverseLerp(floor.maximum.y, floor.minimum.y, at.y);
        DrawIcon(new Rect(mapRect.x + px * mapRect.width - 17f, mapRect.y + py * mapRect.height - 17f, 34, 34), icon);
    }

    private void LegendRow(int icon, string name, ref float y)
    {
        DrawIcon(new Rect(668, y, 30, 30), icon);
        GUI.Label(new Rect(705, y + 1f, 155, 45), name, noteStyle);
        y += 48f;
    }

    private void DrawIcon(Rect at, int index) =>
        GUI.DrawTextureWithTexCoords(at, icons, new Rect(index / IconCount, 0f, 1f / IconCount, 1f));

    private static void Fill(Rect at, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(at, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
