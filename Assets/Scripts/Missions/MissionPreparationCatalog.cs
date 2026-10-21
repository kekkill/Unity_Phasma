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

[CreateAssetMenu(menuName = "Phasma/Mission Preparation Catalog")]
public class MissionPreparationCatalog : ScriptableObject
{
    [SerializeField] private string sceneName = "RoomSearchPrototype";
    [SerializeField] private string mapName = "Небольшой дом";
    [SerializeField] private MissionDifficultyProfile defaultDifficulty;
    [SerializeField] private MissionDifficultyProfile[] difficulties = new MissionDifficultyProfile[0];
    [SerializeField] private MissionEquipmentOption[] equipment = new MissionEquipmentOption[0];
    public string SceneName => sceneName;
    public string MapName => mapName;
    public MissionDifficultyProfile DefaultDifficulty => defaultDifficulty;
    public IReadOnlyList<MissionDifficultyProfile> Difficulties => Array.AsReadOnly(difficulties);
    public IReadOnlyList<MissionEquipmentOption> Equipment => Array.AsReadOnly(equipment);
    public MissionLaunchRequest DefaultRequest()
    {
        var selected = new List<EquipmentId>();
        foreach (var option in equipment) selected.Add(option.Id);
        return new MissionLaunchRequest(sceneName, defaultDifficulty, selected);
    }
}
