using UnityEngine;

// Presents the selected inventory item in the hand and releases items into the world.
[RequireComponent(typeof(PlayerInventory))]
public class PlayerItemHolder : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;
    [Header("Throw")]
    [SerializeField] private float throwForce = 4f;
    [SerializeField] private float dropForwardOffset = 0.25f;
    private PlayerInventory inventory;
    private PlayerInputReader input;
    public PlayerInventory Inventory => inventory;
    public PickupItem CurrentItem => inventory != null ? inventory.SelectedItem : null;
    public bool HasItem => CurrentItem != null;
    private bool CanManageItems => inventory != null && inventory.CanModify && isActiveAndEnabled &&
        (input == null || !input.GameplayBlocked);

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        input = GetComponent<PlayerInputReader>();
    }
    private void OnEnable()
    {
        inventory.Changed += RefreshHand;
        RefreshHand();
    }
    private void OnDisable() { if (inventory != null) inventory.Changed -= RefreshHand; }
    private void RefreshHand()
    {
        if (CurrentItem != null && holdPoint != null) CurrentItem.AttachTo(holdPoint);
    }

    public bool TryPickup(PickupItem item)
    {
        return CanManageItems && holdPoint != null && inventory.TryAdd(item);
    }

    public void UseCurrentItem(GameObject user)
    {
        if (!CanManageItems || CurrentItem == null || CurrentItem.State != PickupItemState.Held) return;
        foreach (MonoBehaviour behaviour in CurrentItem.GetComponents<MonoBehaviour>())
            if (behaviour is IUsableItem usableItem) { usableItem.Use(user); return; }
    }

    public bool PlaceCurrentItem(Vector3 position, Quaternion rotation)
    {
        if (!CanManageItems || !inventory.TryRemoveSelected(out PickupItem item)) return false;
        item.Place(position, rotation);
        return true;
    }

    public void DropCurrentItem()
    {
        if (!CanManageItems || holdPoint == null || !inventory.TryRemoveSelected(out PickupItem item)) return;
        Vector3 forward = holdPoint.forward;
        item.Drop(holdPoint.position + forward * dropForwardOffset, forward, throwForce);
    }

    public void DropAllItems()
    {
        if (inventory == null) return;
        Vector3 origin = holdPoint != null ? holdPoint.position : transform.position + Vector3.up;
        Vector3 forward = holdPoint != null ? holdPoint.forward : transform.forward;
        Vector3 right = holdPoint != null ? holdPoint.right : transform.right;
        PickupItem[] items = inventory.ReleaseAll();
        for (int i = 0; i < items.Length; i++)
            if (items[i] != null)
                items[i].Drop(origin + forward * dropForwardOffset + right * ((i - 1) * 0.25f), forward, 0f);
    }
}
