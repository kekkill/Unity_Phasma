using System.Text;
using UnityEngine;

[RequireComponent(typeof(PlayerJournalController), typeof(JournalState))]
public class JournalScreen : MonoBehaviour
{
    private PlayerJournalController controller;
    private JournalState journal;
    private Vector2 scroll;
    private GUIStyle heading, section, text, small, activeButton;
    private static readonly string[] evidenceNames = { "Минусовая температура", "EMF 5", "Ультрафиолет", "Записи в книге" };
    private static readonly string[] evidenceHints = {
        "Показание термометра ниже 0 °C.", "Улика — только уровень 5. Уровни 2–4 означают обычную активность.",
        "След, обнаруженный УФ-фонариком.", "Письмо призрака в установленной книге." };
    private static readonly string[] markNames = { "Не проверено", "Есть", "Исключено" };
    private static readonly string[] saltNames = { "Не проверено", "Обходит вне охоты", "Оставляет следы", "Игнорирует" };
    private static readonly string[] crossNames = { "Не проверено", "Отпугнул", "Игнорирует" };
    private void Awake()
    {
        controller = GetComponent<PlayerJournalController>();
        journal = GetComponent<JournalState>();
    }
    private void CreateStyles()
    {
        if (heading != null) return;
        heading = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, wordWrap = true };
        section = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = true };
        text = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
        small = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        activeButton = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
        activeButton.normal = activeButton.active;
    }
    private void OnGUI()
    {
        if (!controller.isActiveAndEnabled || !controller.IsOpen || journal.Catalog == null) return;
        CreateStyles();
        float width = Mathf.Max(1f, Mathf.Min(1000f, Screen.width - 30f));
        float height = Mathf.Max(1f, Mathf.Min(720f, Screen.height - 30f));
        Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUILayout.BeginArea(panel, GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Журнал расследования", heading);
        if (GUILayout.Button("Закрыть · J / Esc", GUILayout.Width(150f), GUILayout.Height(32f))) controller.Close();
        GUILayout.EndHorizontal();
        GUILayout.Label("Отмечайте свои наблюдения. Отсутствие события само по себе не исключает улику.", small);
        scroll = GUILayout.BeginScrollView(scroll);
        bool columns = width >= 780f;
        if (columns) GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUI.skin.box, columns ? GUILayout.Width(width * 0.48f) : GUILayout.ExpandWidth(true));
        DrawNotes();
        GUILayout.EndVertical();
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
        DrawCandidates();
        GUILayout.EndVertical();
        if (columns) GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
        GUILayout.Label("Отметки сохраняются при закрытии журнала. Охота продолжается.", small);
        GUILayout.EndArea();
    }
    private void DrawNotes()
    {
        GUILayout.Label("Улики", section);
        for (int i = 0; i < JournalState.EvidenceCount; i++)
        {
            EvidenceType type = (EvidenceType)i;
            GUILayout.Label(evidenceNames[i], text);
            GUILayout.BeginHorizontal();
            for (int mark = 0; mark < markNames.Length; mark++)
                if (ChoiceButton(markNames[mark], journal.GetMark(type) == (EvidenceMark)mark))
                    journal.SetMark(type, (EvidenceMark)mark);
            GUILayout.EndHorizontal();
            GUILayout.Label(evidenceHints[i], small);
            GUILayout.Space(8f);
        }
        GUILayout.Label("Поведение", section);
        GUILayout.Label("Соль", text);
        for (int i = 0; i < saltNames.Length; i++)
            if (ChoiceButton(saltNames[i], journal.Salt == (SaltObservation)i)) journal.SetSalt((SaltObservation)i);
        GUILayout.Space(6f);
        GUILayout.Label("Исправный крест в зоне защиты", text);
        GUILayout.BeginHorizontal();
        for (int i = 0; i < crossNames.Length; i++)
            if (ChoiceButton(crossNames[i], journal.Cross == (CrossObservation)i)) journal.SetCross((CrossObservation)i);
        GUILayout.EndHorizontal();
        GUILayout.Space(14f);
        if (GUILayout.Button("Очистить журнал")) journal.ResetNotes();
    }
    private bool ChoiceButton(string label, bool chosen)
    {
        Color previous = GUI.backgroundColor;
        if (chosen) GUI.backgroundColor = new Color(0.45f, 0.85f, 0.65f);
        bool pressed = GUILayout.Button(chosen ? "✓ " + label : label, chosen ? activeButton : GUI.skin.button,
            GUILayout.MinHeight(28f));
        GUI.backgroundColor = previous;
        return pressed;
    }
    private void DrawCandidates()
    {
        int count = 0;
        foreach (JournalGhostEntry _ in journal.MatchingEntries()) count++;
        GUILayout.Label("Подходящие типы: " + count + " / " + journal.Catalog.Entries.Count, section);
        if (count == 0) GUILayout.Label("Нет подходящих типов. Проверьте, не противоречат ли отметки друг другу.", text);
        foreach (JournalGhostEntry entry in journal.Catalog.Entries)
        {
            if (entry == null || entry.Profile == null) continue;
            bool matches = journal.IsCompatible(entry.Profile);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(entry.DisplayName, section);
            GUILayout.Label(entry.Description, text);
            GUILayout.Label("Улики: " + EvidenceSummary(entry.Profile), small);
            GUILayout.Label(matches ? "Подходит по вашим отметкам" : "Исключён вашими отметками", small);
            bool previous = GUI.enabled;
            GUI.enabled = previous && matches;
            if (ChoiceButton("Выбрать этот тип", journal.SelectedGhostId == entry.Id)) journal.SelectGhost(entry.Id);
            GUI.enabled = previous;
            GUILayout.EndVertical();
        }
        GUILayout.Space(12f);
        GUILayout.Label("Итоговая версия", section);
        JournalGhostEntry selected = journal.SelectedGhost;
        GUILayout.Label(selected != null ? selected.DisplayName : "Тип пока не выбран", text);
        if (selected != null)
        {
            if (!journal.SelectionMatches) GUILayout.Label("Выбранная версия противоречит текущим отметкам. Пересмотрите выбор.", text);
            if (GUILayout.Button("Снять выбор")) journal.ClearSelection();
        }
        GUILayout.Label("Выбор сохраняет вашу версию до конца раунда.", small);
    }
    private static string EvidenceSummary(GhostProfile profile)
    {
        var result = new StringBuilder();
        for (int i = 0; i < JournalState.EvidenceCount; i++)
        {
            if (!profile.HasEvidence((EvidenceType)i)) continue;
            if (result.Length > 0) result.Append(", ");
            result.Append(evidenceNames[i]);
        }
        return result.Length == 0 ? "нет" : result.ToString();
    }
}
