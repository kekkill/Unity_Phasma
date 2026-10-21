using System;
using UnityEngine;

[DefaultExecutionOrder(-300)]
public class MissionSceneSetup : MonoBehaviour
{
    [Serializable]
    private class EquipmentSpawn
    {
        public EquipmentId id;
        public GameObject item;
    }
    [SerializeField] private MissionPreparationCatalog catalog;
    [SerializeField] private EquipmentSpawn[] equipment = new EquipmentSpawn[0];
    private MissionLaunchRequest request;
    public MissionDifficultyProfile Difficulty => request != null ? request.Difficulty : null;
    public bool Includes(EquipmentId id) => request == null || request.Includes(id);
    private void Awake()
    {
        MissionLaunchRequest selected = MissionFlow.CurrentRequest;
        request = selected != null && selected.SceneName == gameObject.scene.name ? selected : catalog != null ? catalog.DefaultRequest() : null;
        foreach (var spawn in equipment)
            if (spawn.item != null) spawn.item.SetActive(Includes(spawn.id));
    }
}
