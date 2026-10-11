using System;
using UnityEngine;

// Per-participant slots and ownership. Input and hand/world presentation live elsewhere.
public class PlayerInventory : MonoBehaviour
{
    public const int SlotCount = 3;
    private readonly PickupItem[] slots = new PickupItem[SlotCount];
    private PlayerLifeState life;
    private Transform storage;
    public int SelectedSlot { get; private set; }
    public PickupItem SelectedItem => slots[SelectedSlot];
    public event Action Changed;
    public bool CanModify => isActiveAndEnabled && (life == null ||
        (life.IsAlive && (life.Session == null || !life.Session.HasEnded)));
    public int Count
    {
        get
        {
            int count = 0;
            foreach (PickupItem item in slots) if (item != null) count++;
            return count;
        }
    }
    public bool IsFull => Count == SlotCount;

    private void Awake()
    {
        life = GetComponent<PlayerLifeState>();
        storage = new GameObject("Inventory storage").transform;
        storage.SetParent(transform, false);
    }

    public PickupItem GetItem(int slot)
    {
        if (slot < 0 || slot >= SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
        return slots[slot];
    }

    public bool TryAdd(PickupItem item)
    {
        if (!CanModify || item == null || !item.isActiveAndEnabled || item.InventoryOwner != null ||
            item.State == PickupItemState.Held || item.State == PickupItemState.Stored) return false;
        int free = slots[SelectedSlot] == null ? SelectedSlot : Array.FindIndex(slots, value => value == null);
        if (free < 0) return false;
        StoreSelected();
        slots[free] = item;
        item.InventoryOwner = this;
        item.Store(storage);
        SelectedSlot = free;
        Changed?.Invoke();
        return true;
    }

    public bool SelectSlot(int slot)
    {
        if (!CanModify || slot < 0 || slot >= SlotCount || slot == SelectedSlot) return false;
        StoreSelected();
        SelectedSlot = slot;
        Changed?.Invoke();
        return true;
    }

    private void StoreSelected()
    {
        if (SelectedItem != null) SelectedItem.Store(storage);
    }

    public bool TryRemoveSelected(out PickupItem item)
    {
        item = null;
        if (!CanModify || SelectedItem == null) return false;
        item = SelectedItem;
        slots[SelectedSlot] = null;
        item.InventoryOwner = null;
        Changed?.Invoke();
        return true;
    }

    // Death cleanup must work after life state has already become Dead.
    internal PickupItem[] ReleaseAll()
    {
        var released = new PickupItem[SlotCount];
        Array.Copy(slots, released, SlotCount);
        Array.Clear(slots, 0, SlotCount);
        foreach (PickupItem item in released) if (item != null) item.InventoryOwner = null;
        Changed?.Invoke();
        return released;
    }

    internal void ForgetDestroyedItem(PickupItem item)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (!ReferenceEquals(slots[i], item)) continue;
            slots[i] = null;
            item.InventoryOwner = null;
            Changed?.Invoke();
            return;
        }
    }
}
