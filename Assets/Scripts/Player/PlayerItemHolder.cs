using UnityEngine;

public class PlayerItemHolder : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;

    [Header("Throw")]
    [SerializeField] private float throwForce = 4f;
    [SerializeField] private float dropForwardOffset = 0.25f;

    private PickupItem currentItem;

    public bool HasItem => currentItem != null;

    public bool TryPickup(PickupItem item)
    {
        if (HasItem)
            return false;

        currentItem = item;

        currentItem.AttachTo(holdPoint);

        return true;
    }

    public void UseCurrentItem(GameObject user)
    {
        if (currentItem == null)
            return;

        MonoBehaviour[] behaviours =
            currentItem.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IUsableItem usableItem)
            {
                usableItem.Use(user);
                return;
            }
        }
    }

    public void DropCurrentItem()
    {
        if (currentItem == null)
            return;

        PickupItem itemToDrop = currentItem;

        currentItem = null;

        Vector3 throwDirection = holdPoint.forward;

        Vector3 dropPosition =
            holdPoint.position +
            throwDirection * dropForwardOffset;

        itemToDrop.Drop(
            dropPosition,
            throwDirection,
            throwForce
        );
    }
}