using UnityEngine;

// Adapter for world equipment; inventory and placement retain ownership of item state.
public class GhostThrowableItem : MonoBehaviour, IGhostInteractionTarget
{
    public Vector3 GhostInteractionPosition => transform.position;
    public bool CanGhostInteract(GhostInteractionContext context)
    {
        PickupItem item = GetComponent<PickupItem>();
        Rigidbody body = GetComponent<Rigidbody>();
        return isActiveAndEnabled && context.Profile != null && item != null &&
            item.State == PickupItemState.Dropped && item.InventoryOwner == null &&
            body != null && !body.isKinematic;
    }
    public bool TryGhostInteract(GhostInteractionContext context)
    {
        if (!CanGhostInteract(context)) return false;
        Transform source = context.Actor != null ? context.Actor.transform :
            context.Controller != null ? context.Controller.transform : null;
        if (source == null) return false;
        Vector3 direction = Vector3.ProjectOnPlane(transform.position - source.position, Vector3.up);
        if (direction.sqrMagnitude < 0.001f) direction = Vector3.ProjectOnPlane(source.forward, Vector3.up);
        direction = (direction.normalized + Vector3.up * 0.35f).normalized;
        float impulse = context.IsHunting ? context.Profile.HuntThrowImpulse : context.Profile.NormalThrowImpulse;
        if (impulse <= 0f) return false;
        Rigidbody body = GetComponent<Rigidbody>();
        GetComponent<ThrownItemNoise>()?.Disarm();
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.AddForce(direction * impulse, ForceMode.Impulse);
        return true;
    }
    public void GhostInteract(GhostInteractionContext context) => TryGhostInteract(context);
}
