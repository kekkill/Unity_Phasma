using System.Collections.Generic;

// Copy selections at launch; menu widgets cannot change a running mission.
public sealed class MissionLaunchRequest
{
    private readonly HashSet<EquipmentId> equipment;
    public string SceneName { get; }
    public MissionDifficultyProfile Difficulty { get; }
    public MissionLaunchRequest(string sceneName, MissionDifficultyProfile difficulty, IEnumerable<EquipmentId> selected)
    {
        SceneName = sceneName;
        Difficulty = difficulty;
        equipment = new HashSet<EquipmentId>();
        foreach (var id in selected)
        {
            equipment.Add(id);
            if (equipment.Count == PlayerInventory.SlotCount) break;
        }
    }
    public bool Includes(EquipmentId id) => equipment.Contains(id);
}
