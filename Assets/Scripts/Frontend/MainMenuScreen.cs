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
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        if (catalog == null) { BeginPage("Не назначены настройки подготовки"); return; }
        if (!isHub)
        {
            ShowHome();
            return;
        }

        var previous = MissionFlow.PreparationDraft ?? MissionFlow.CurrentRequest;
        map = catalog.Maps.FirstOrDefault(option => option.SceneName == previous?.SceneName)
            ?? catalog.Maps.FirstOrDefault();
        difficulty = previous?.Difficulty ?? catalog.DefaultDifficulty;
        foreach (var option in catalog.Equipment)
            if (previous == null || previous.Includes(option.Id)) equipment.Add(option.Id);
        ShowPreparation();
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
            ReportLoad(MissionFlow.TryReturnToPreparation(out string reason), reason)));
        MenuUi.Button(content, "Настройки", ShowSettings);
        if (!Application.isMobilePlatform) MenuUi.Button(content, "Выйти", Quit);
    }
    private void ShowPreparation(bool focusDifficulty = false)
    {
        BeginPage("Штаб расследований");
        MenuUi.Label(content, "Режим", 26);
        MenuUi.Label(content, "✓ Одиночная игра");
        MenuUi.Button(content, "Кооператив — позже", () => { }).interactable = false;
        MenuUi.Label(content, "Карта", 26);
        Button first = null;
        foreach (var option in catalog.Maps)
        {
            var button = MenuUi.Button(content, (map == option ? "✓ " : "") + option.DisplayName,
                () => { map = option; ShowPreparation(); });
            if (first == null || map == option) first = button;
        }
        if (map != null) MenuUi.Label(content, map.Description, 20);
        MenuUi.Label(content, "Сложность", 26);
        foreach (var option in catalog.Difficulties)
        {
            if (option == null) continue;
            var chosen = option;
            var button = MenuUi.Button(content, (difficulty == option ? "✓ " : "") + option.DisplayName,
                () => { difficulty = chosen; ShowPreparation(true); });
            if (focusDifficulty && difficulty == option) first = button;
        }
        if (difficulty != null) MenuUi.Label(content, difficulty.Description, 20);
        MenuUi.Label(content, "Снаряжение в безопасной зоне", 26);
        MenuUi.Label(content, "Это стартовый комплект. В инвентаре по-прежнему три слота; покупать предметы пока не нужно.", 20);
        foreach (var option in catalog.Equipment)
        {
            EquipmentId id = option.Id;
            MenuUi.Toggle(content, option.DisplayName, equipment.Contains(id), value =>
            { if (value) equipment.Add(id); else equipment.Remove(id); });
        }
        status = MenuUi.Label(content, "");
        MenuUi.Button(content, "Начать расследование", Launch).interactable = map != null;
        MenuUi.Button(content, "Настройки", ShowSettings);
        MenuUi.Button(content, "В главное меню", () =>
        {
            MissionFlow.PreparationDraft = CreateRequest();
            ReportLoad(MissionFlow.TryReturnToMenu(out string reason), reason);
        });
        MenuUi.Select(first);
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
        { GamePreferences.Save(); if (isHub) ShowPreparation(); else ShowHome(); });
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
