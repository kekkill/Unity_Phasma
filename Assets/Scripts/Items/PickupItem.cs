using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PickupItem : MonoBehaviour, IInteractable
{
    private Rigidbody itemRigidbody;
    private Collider itemCollider;

    public string InteractionPrompt => "[E] Поднять";

    private void Awake()
    {
        itemRigidbody =
            GetComponent<Rigidbody>();

        itemCollider =
            GetComponent<Collider>();
    }

    public void Interact(GameObject interactor)
    {
        PlayerItemHolder holder =
            interactor.GetComponent<PlayerItemHolder>();

        if (holder == null)
            return;

        holder.TryPickup(this);
    }

    public void AttachTo(Transform holdPoint)
    {
        itemRigidbody.isKinematic = true;
        itemCollider.enabled = false;

        transform.SetParent(holdPoint);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop(
        Vector3 dropPosition,
        Vector3 throwDirection,
        float throwForce)
    {
        transform.SetParent(null);

        transform.position = dropPosition;

        itemCollider.enabled = true;

        itemRigidbody.isKinematic = false;
        itemRigidbody.useGravity = true;

        itemRigidbody.AddForce(
            throwDirection.normalized * throwForce,
            ForceMode.Impulse
        );
    }
}