using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuScreen : MonoBehaviour
{
    [SerializeField] private MissionPreparationCatalog catalog;
    [SerializeField] private bool isHub;
    private MissionMapOption map;
    private MissionDifficultyProfile difficulty;
    private readonly HashSet<EquipmentId> equipment = new HashSet<EquipmentId>();
    private GameObject page;
    private CanvasGroup controls;
    private RectTransform content;
    private Text status;
    private HubPhone phone;
    private int CarriedSlots => map?.CarriedSlots ?? 0;
    private bool LoadoutReady => CarriedSlots == 0 || equipment.Count == CarriedSlots;
    public bool IsOpen => page != null && page.activeSelf;
    private void Start()
    {
        if (!isHub)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ShowHome();
            return;
        }

        if (catalog == null) return;

        var previous = MissionFlow.PreparationDraft ?? MissionFlow.CurrentRequest;
        map = catalog.Maps.FirstOrDefault(option => option.SceneName == previous?.SceneName)
            ?? catalog.Maps.FirstOrDefault();
        difficulty = previous?.Difficulty ?? catalog.DefaultDifficulty;
        var options = catalog.Equipment;
        var selected = previous == null
            ? options.Take(PlayerInventory.SlotCount)
            : options.Where(option => previous.Includes(option.Id));
        equipment.UnionWith(selected.Select(option => option.Id));
        LimitCarriedEquipment();
    }

    public void OpenStation(HubPhone source, HubStation.StationKind station)
    {
        phone = source;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (catalog == null)
        {
            BeginPage("Не назначены настройки подготовки");
            MenuUi.Select(MenuUi.Button(content, "Закрыть", CloseToHub));
        }
        else
        {
            switch (station)
            {
                case HubStation.StationKind.MapBoard: ShowMapBoard(); break;
                case HubStation.StationKind.EquipmentRack: ShowEquipmentRack(); break;
                case HubStation.StationKind.LaunchTerminal: ShowLaunchTerminal(); break;
            }
        }

        StartCoroutine(Reveal(controls));
    }

    public void CloseToHub()
    {
        if (!IsOpen || MissionFlow.IsLoading) return;
        page.SetActive(false);
        Destroy(page);
        page = null;
        phone.ResumeExploration();
    }

    private IEnumerator Reveal(CanvasGroup group)
    {
        const float duration = 0.24f;
        group.alpha = 0f;
        group.interactable = false;
        while (group != null && group.gameObject.activeSelf && group.alpha < 1f)
        {
            yield return null;
            if (group == null || !group.gameObject.activeSelf) yield break;
            group.alpha = Mathf.MoveTowards(group.alpha, 1f, Time.unscaledDeltaTime / duration);
        }
        if (group != null) group.interactable = true;
    }

    private void BeginPage(string title)
    {
        if (page != null) { page.SetActive(false); Destroy(page); }
        page = MenuUi.Create(transform, title, out content, out controls, isHub);
    }
    private void ShowHome()
    {
        BeginPage("Phasma — расследование");
        MenuUi.Label(content, "Найдите улики, определите призрака и вернитесь живым.");
        status = MenuUi.Label(content, "");
        MenuUi.Select(MenuUi.Button(content, "В штаб", () =>
            ReportLoad(MissionFlow.TryEnterHub(out string reason), reason)));
        MenuUi.Button(content, "Настройки", ShowSettings);
        if (!Application.isMobilePlatform) MenuUi.Button(content, "Выйти", Quit);
    }
    private void ShowMapBoard(bool focusDifficulty = false)
    {
        BeginPage("Доска карт");
        MenuUi.Button(content, "Закрыть", CloseToHub);
        string selectedLegend = map != null ? map.Description : "Выберите карту.";
        Text legend = MenuUi.Label(content, selectedLegend, 20);
        Button first = null;
        foreach (var option in catalog.Maps)
        {
            var button = MenuUi.Button(content, (map == option ? "✓ " : "") + option.DisplayName,
                () => { map = option; LimitCarriedEquipment(); ShowMapBoard(); });
            button.gameObject.AddComponent<MapLegendPreview>().Bind(legend, option.Description, selectedLegend);
            if (first == null || map == option) first = button;
        }
        MenuUi.Label(content, "Сложность", 26);
        foreach (var option in catalog.Difficulties)
        {
            if (option == null) continue;
            var chosen = option;
            var button = MenuUi.Button(content, (difficulty == option ? "✓ " : "") + option.DisplayName,
                () => { difficulty = chosen; ShowMapBoard(true); });
            if (focusDifficulty && difficulty == option) first = button;
        }
        if (difficulty != null) MenuUi.Label(content, difficulty.Description, 20);
        MenuUi.Select(first);
    }

    private void ShowEquipmentRack()
    {
        BeginPage("Стенд снаряжения");
        var close = MenuUi.Button(content, "Закрыть", CloseToHub);
        Text count = MenuUi.Label(content, LoadoutMessage(), 20);
        foreach (var option in catalog.Equipment)
        {
            EquipmentId id = option.Id;
            Toggle toggle = null;
            toggle = MenuUi.Toggle(content, option.DisplayName, equipment.Contains(id), value =>
            {
                if (value && CarriedSlots > 0 && equipment.Count >= CarriedSlots)
                {
                    toggle.SetIsOnWithoutNotify(false);
                    return;
                }
                if (value) equipment.Add(id); else equipment.Remove(id);
                count.text = LoadoutMessage();
            });
        }
        MenuUi.Select(close);
    }

    private void ShowLaunchTerminal()
    {
        BeginPage("Терминал выезда");
        MenuUi.Button(content, "Закрыть", CloseToHub);
        MenuUi.Label(content, $"Карта: {map?.DisplayName ?? "—"}");
        MenuUi.Label(content, $"Сложность: {difficulty?.DisplayName ?? "—"}");
        MenuUi.Label(content, $"Предметов к выезду: {equipment.Count}");
        MenuUi.Label(content, "Одиночная игра  ·  Кооператив позже", 20);
        status = MenuUi.Label(content, "");
        var launch = MenuUi.Button(content, "Начать расследование", Launch);
        launch.interactable = map != null && difficulty != null && LoadoutReady;
        if (!LoadoutReady) status.text = $"Выберите {CarriedSlots} предмета на стенде.";
        MenuUi.Button(content, "Настройки", ShowSettings);
        MenuUi.Button(content, "В главное меню", () =>
        {
            MissionFlow.PreparationDraft = CreateRequest();
            ReportLoad(MissionFlow.TryReturnToMenu(out string reason), reason);
        });
        MenuUi.Select(launch.interactable ? launch : null);
    }
    private string LoadoutMessage() => CarriedSlots == 0
        ? "Отмеченные предметы будут в безопасной зоне."
        : $"Взять с собой: {equipment.Count}/{CarriedSlots}. Остальные приборы ждут в доме.";

    private void LimitCarriedEquipment()
    {
        if (CarriedSlots == 0 || equipment.Count <= CarriedSlots) return;
        var carried = catalog.Equipment.Select(option => option.Id)
            .Where(equipment.Contains).Take(CarriedSlots).ToArray();
        equipment.Clear();
        equipment.UnionWith(carried);
    }

    private MissionLaunchRequest CreateRequest() => new MissionLaunchRequest(map?.SceneName, difficulty, equipment);

    private void Launch()
    {
        var request = CreateRequest();
        bool started = MissionFlow.TryStart(request, out string reason);
        if (started) MissionFlow.PreparationDraft = request;
        ReportLoad(started, reason);
    }

    private void ReportLoad(bool started, string reason)
    {
        if (started) controls.interactable = false;
        status.text = started ? "Загрузка…" : reason;
    }

    private void ShowSettings()
    {
        BeginPage("Настройки");
        Text volume = MenuUi.Label(content, "Громкость: " + Mathf.RoundToInt(GamePreferences.Volume * 100) + "%");
        var first = MenuUi.Slider(content, GamePreferences.Volume, 0, 1, value =>
        { GamePreferences.SetVolume(value); volume.text = "Громкость: " + Mathf.RoundToInt(value * 100) + "%"; });
        Text sensitivity = MenuUi.Label(content, "Чувствительность обзора: " + GamePreferences.Sensitivity.ToString("0.00") + "×");
        MenuUi.Slider(content, GamePreferences.Sensitivity, 0.25f, 3, value =>
        { GamePreferences.SetSensitivity(value); sensitivity.text = "Чувствительность обзора: " + value.ToString("0.00") + "×"; });
        MenuUi.Label(content, "Настройки сохраняются на этом устройстве.", 20);
        MenuUi.Button(content, "Назад", () =>
        { GamePreferences.Save(); if (isHub) ShowLaunchTerminal(); else ShowHome(); });
        MenuUi.Select(first);
    }
    private void OnDisable() => GamePreferences.Save();
    private void OnApplicationPause(bool paused) { if (paused) GamePreferences.Save(); }
    private void Quit()
    {
        GamePreferences.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
