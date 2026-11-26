using System;
using System.Collections.Generic;
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
    [SerializeField] private PlayerInventory carriedInventory;
    [SerializeField] private Vector3[] reservePositions = Array.Empty<Vector3>();
    private readonly List<EquipmentSpawn> createdEquipment = new();
    private MissionLaunchRequest request;
    public bool UsesCarriedEquipment => carriedInventory != null;
    public MissionDifficultyProfile Difficulty => request != null ? request.Difficulty : null;
    public bool Includes(EquipmentId id) => request == null || request.Includes(id);
    private void Awake()
    {
        MissionLaunchRequest selected = MissionFlow.CurrentRequest;
        request = selected != null && selected.SceneName == gameObject.scene.name ? selected : catalog != null ? catalog.DefaultRequest(gameObject.scene.name) : null;
        if (UsesCarriedEquipment) return;
        foreach (var spawn in equipment)
            if (spawn.item != null) spawn.item.SetActive(Includes(spawn.id));
    }
    public void RegisterCreatedEquipment(EquipmentId id, GameObject item)
    {
        if (UsesCarriedEquipment) createdEquipment.Add(new EquipmentSpawn { id = id, item = item });
    }

    private void Start()
    {
        if (!UsesCarriedEquipment) return;

        int reserveIndex = 0;
        foreach (var spawn in equipment)
            AssignEquipment(spawn, ref reserveIndex);
        foreach (var spawn in createdEquipment)
            AssignEquipment(spawn, ref reserveIndex);
        carriedInventory.SelectSlot(0);
    }

    private void AssignEquipment(EquipmentSpawn spawn, ref int reserveIndex)
    {
        if (spawn.item == null || !spawn.item.TryGetComponent(out PickupItem item)) return;
        if (Includes(spawn.id) && carriedInventory.TryAdd(item)) return;
        if (reserveIndex < reservePositions.Length)
            spawn.item.transform.position = reservePositions[reserveIndex++];
    }
}
