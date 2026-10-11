using System;
using UnityEngine;

// Append Stored to preserve serialized values in existing scenes/prefabs.
public enum PickupItemState { Dropped, Held, Placed, Stored }

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(GhostThrowableItem))]
public class PickupItem : MonoBehaviour, IInteractable
{
    [SerializeField] private PickupItemState state;
    public PickupItemState State => state;
    public PlayerInventory InventoryOwner { get; internal set; }
    public event Action<PickupItemState> StateChanged;
    private Collider[] colliders;
    private bool[] originalEnabled;
    private Rigidbody itemRigidbody;
    private Collider itemCollider;

    public string InteractionPrompt => GetComponent<CrossItem>() != null && GetComponent<CrossItem>().IsBroken
        ? "[E] Поднять сломанный крест" : "[E] Поднять";

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>(true);
        originalEnabled = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++) originalEnabled[i] = colliders[i].enabled;
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
        SuspendPhysics();
        SetColliders(false);
        SetState(PickupItemState.Held);

        transform.SetParent(holdPoint);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        gameObject.SetActive(true);
    }

    public void Store(Transform storage)
    {
        SuspendPhysics();
        SetColliders(false);
        SetState(PickupItemState.Stored);
        transform.SetParent(storage, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        // Keep state, but suppress lights, sensing, rendering and protection.
        gameObject.SetActive(false);
    }

    private void SuspendPhysics()
    {
        if (!itemRigidbody.isKinematic)
        {
            itemRigidbody.linearVelocity = Vector3.zero;
            itemRigidbody.angularVelocity = Vector3.zero;
        }
        itemRigidbody.isKinematic = true;
        itemRigidbody.useGravity = false;
    }

    private void OnDestroy()
    {
        if (InventoryOwner != null) InventoryOwner.ForgetDestroyedItem(this);
    }

    public void Drop(
        Vector3 dropPosition,
        Vector3 throwDirection,
        float throwForce)
    {
        transform.SetParent(null);

        transform.position = dropPosition;
        gameObject.SetActive(true);
        SetColliders(true);
        SetState(PickupItemState.Dropped);

        itemRigidbody.isKinematic = false;
        itemRigidbody.useGravity = true;

        itemRigidbody.AddForce(
            throwDirection.normalized * throwForce,
            ForceMode.Impulse
        );
    }
    private void SetState(PickupItemState value)
    {
        if (state == value) return;
        state = value;
        StateChanged?.Invoke(state);
    }
    private void SetColliders(bool active)
    {
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = active && originalEnabled[i];
    }
    // Collider.bounds is empty while held (disabled). Use the local shape instead.
    private Bounds PlacementBounds
    {
        get
        {
            Bounds local;
            if (itemCollider is BoxCollider box) local = new Bounds(box.center, box.size);
            else if (itemCollider is SphereCollider sphere)
                local = new Bounds(sphere.center, Vector3.one * sphere.radius * 2f);
            else if (itemCollider is CapsuleCollider capsule)
            {
                Vector3 size = Vector3.one * capsule.radius * 2f;
                size[capsule.direction] = Mathf.Max(capsule.height, capsule.radius * 2f);
                local = new Bounds(capsule.center, size);
            }
            else if (itemCollider is MeshCollider mesh && mesh.sharedMesh != null) local = mesh.sharedMesh.bounds;
            else local = new Bounds(Vector3.zero, Vector3.one * 0.2f);
            Vector3 scale = transform.lossyScale;
            Vector3 absolute = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            return new Bounds(Vector3.Scale(local.center, scale), Vector3.Scale(local.size, absolute));
        }
    }
    public float PlacementLift => Mathf.Max(0f, PlacementBounds.extents.y - PlacementBounds.center.y) + 0.02f;
    public Vector3 PlacementHalfSize => PlacementBounds.extents;
    public Vector3 PlacementCenter => PlacementBounds.center;
    public void Place(Vector3 position, Quaternion rotation)
    {
        transform.SetParent(null);
        transform.SetPositionAndRotation(position, rotation);
        SuspendPhysics();
        gameObject.SetActive(true);
        SetColliders(true);
        SetState(PickupItemState.Placed);
    }
}
