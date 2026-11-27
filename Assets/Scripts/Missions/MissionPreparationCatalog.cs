using System;
using System.Collections.Generic;
using UnityEngine;

// Values are serialized in the catalog and scene; keep existing IDs when adding equipment.
public enum EquipmentId
{
    Flashlight = 0, Thermometer = 1, EmfReader = 2, UltravioletFlashlight = 3,
    WritingBook = 4, MotionSensor = 5, Cross = 6, Salt = 7
}

[Serializable]
public class MissionEquipmentOption
{
    [SerializeField] private EquipmentId id;
    [SerializeField] private string displayName;
    public EquipmentId Id => id;
    public string DisplayName => displayName;
}

[Serializable]
public class MissionMapOption
{
    [SerializeField] private string sceneName;
    [SerializeField] private string displayName;
    [SerializeField] private string description;
    [SerializeField, Min(0)] private int carriedSlots;
    public string SceneName => sceneName;
    public string DisplayName => displayName;
    public string Description => description;
    public int CarriedSlots => carriedSlots > 0 ? Mathf.Min(carriedSlots, PlayerInventory.SlotCount) : PlayerInventory.SlotCount;
}

[CreateAssetMenu(menuName = "Phasma/Mission Preparation Catalog")]
public class MissionPreparationCatalog : ScriptableObject
{
    [SerializeField] private MissionMapOption[] maps = new MissionMapOption[0];
    [SerializeField] private MissionDifficultyProfile defaultDifficulty;
    [SerializeField] private MissionDifficultyProfile[] difficulties = new MissionDifficultyProfile[0];
    [SerializeField] private MissionEquipmentOption[] equipment = new MissionEquipmentOption[0];
    public IReadOnlyList<MissionMapOption> Maps => Array.AsReadOnly(maps);
    public MissionDifficultyProfile DefaultDifficulty => defaultDifficulty;
    public IReadOnlyList<MissionDifficultyProfile> Difficulties => Array.AsReadOnly(difficulties);
    public IReadOnlyList<MissionEquipmentOption> Equipment => Array.AsReadOnly(equipment);
    public MissionLaunchRequest DefaultRequest(string sceneName)
    {
        var selected = new List<EquipmentId>();
        foreach (var option in equipment) selected.Add(option.Id);
        return new MissionLaunchRequest(sceneName, defaultDifficulty, selected);
    }
}
