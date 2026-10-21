using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuScreen : MonoBehaviour
{
    [SerializeField] private MissionPreparationCatalog catalog;
    private MissionDifficultyProfile difficulty;
    private readonly HashSet<EquipmentId> equipment = new HashSet<EquipmentId>();
    private GameObject page;
    private CanvasGroup controls;
    private RectTransform content;
    private Text status;
    private bool settingsFromPreparation;
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        if (catalog == null) { BeginPage("Не назначены настройки подготовки"); return; }
        var previous = MissionFlow.CurrentRequest;
        bool sameMap = previous != null && previous.SceneName == catalog.SceneName;
        difficulty = sameMap ? previous.Difficulty : catalog.DefaultDifficulty;
        foreach (var option in catalog.Equipment)
            if (!sameMap || previous.Includes(option.Id)) equipment.Add(option.Id);
        if (MissionFlow.OpenPreparation) ShowPreparation(); else ShowHome();
    }
    private void BeginPage(string title)
    {
        if (page != null) { page.SetActive(false); Destroy(page); }
        page = MenuUi.Create(transform, title, out content, out controls);
    }
    private void ShowHome()
    {
        BeginPage("Phasma — расследование");
        MenuUi.Label(content, "Найдите улики, определите призрака и вернитесь живым.");
        MenuUi.Select(MenuUi.Button(content, "Подготовить миссию", ShowPreparation));
        MenuUi.Button(content, "Настройки", () => ShowSettings(false));
        if (!Application.isMobilePlatform) MenuUi.Button(content, "Выйти", Quit);
    }
    private void ShowPreparation()
    {
        BeginPage("Подготовка миссии");
        MenuUi.Label(content, "Карта: " + catalog.MapName);
        MenuUi.Label(content, "Сложность", 26);
        Button first = null;
        foreach (var option in catalog.Difficulties)
        {
            if (option == null) continue;
            var chosen = option;
            var button = MenuUi.Button(content, (difficulty == option ? "✓ " : "") + option.DisplayName,
                () => { difficulty = chosen; ShowPreparation(); });
            if (first == null) first = button;
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
        MenuUi.Label(content, "Питание дома выключено. Щиток — в коридоре у входа. Фонарики работают независимо от него.", 20);
        status = MenuUi.Label(content, "");
        MenuUi.Button(content, "Начать расследование", Launch);
        MenuUi.Button(content, "Настройки", () => ShowSettings(true));
        MenuUi.Button(content, "В главное меню", ShowHome);
        MenuUi.Select(first);
    }
    private void Launch()
    {
        if (MissionFlow.TryStart(new MissionLaunchRequest(catalog.SceneName, difficulty, equipment), out string reason))
        { controls.interactable = false; status.text = "Загрузка…"; }
        else status.text = reason;
    }
    private void ShowSettings(bool fromPreparation)
    {
        settingsFromPreparation = fromPreparation;
        BeginPage("Настройки");
        Text volume = MenuUi.Label(content, "Громкость: " + Mathf.RoundToInt(GamePreferences.Volume * 100) + "%");
        var first = MenuUi.Slider(content, GamePreferences.Volume, 0, 1, value =>
        { GamePreferences.SetVolume(value); volume.text = "Громкость: " + Mathf.RoundToInt(value * 100) + "%"; });
        Text sensitivity = MenuUi.Label(content, "Чувствительность обзора: " + GamePreferences.Sensitivity.ToString("0.00") + "×");
        MenuUi.Slider(content, GamePreferences.Sensitivity, 0.25f, 3, value =>
        { GamePreferences.SetSensitivity(value); sensitivity.text = "Чувствительность обзора: " + value.ToString("0.00") + "×"; });
        MenuUi.Label(content, "Настройки сохраняются на этом устройстве.", 20);
        MenuUi.Button(content, "Назад", () =>
        { GamePreferences.Save(); if (settingsFromPreparation) ShowPreparation(); else ShowHome(); });
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
