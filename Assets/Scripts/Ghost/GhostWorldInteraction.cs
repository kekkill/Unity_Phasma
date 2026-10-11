using System.Collections.Generic;
using UnityEngine;

// Shared physical eligibility/execution for ordinary activity and moving hunts.
public class GhostWorldInteraction : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float huntInteractionRadius = 2.5f;
    [SerializeField, Min(0.1f)] private float targetCooldown = 1.5f;
    private float nextHuntInteraction;
    private readonly Dictionary<MonoBehaviour, float> cooldowns = new Dictionary<MonoBehaviour, float>();
    private readonly List<MonoBehaviour> staleTargets = new List<MonoBehaviour>();
    private readonly List<IGhostInteractionTarget> targets = new List<IGhostInteractionTarget>();
    public void ResetHuntTimer() => nextHuntInteraction = 0f;
    public void TickHunt()
    {
        GhostController owner = GetComponent<GhostController>();
        if (owner == null || !owner.IsHunting || owner.Profile == null || Time.time < nextHuntInteraction) return;
        nextHuntInteraction = Time.time + owner.Profile.HuntInteractionInterval;
        TryInteractNearby(huntInteractionRadius);
    }
    public bool TryInteractNearby(float radius)
    {
        GhostController owner = GetComponent<GhostController>();
        GhostActor actor = GetComponent<GhostActor>();
        GhostActionRestrictions restrictions = GetComponent<GhostActionRestrictions>();
        if (!isActiveAndEnabled || owner == null || owner.Profile == null || (actor != null && !actor.isActiveAndEnabled) ||
            (restrictions != null && restrictions.IsActionBlocked(GhostActionType.Interaction))) return false;
        var context = new GhostInteractionContext(actor, owner, owner.Profile);
        Door nearestDoor = null;
        float nearestDoorDistance = float.PositiveInfinity;
        targets.Clear(); staleTargets.Clear();
        foreach (var pair in cooldowns)
            if (pair.Key == null || Time.time >= pair.Value) staleTargets.Add(pair.Key);
        foreach (var target in staleTargets) cooldowns.Remove(target);
        Collider[] hits = Physics.OverlapSphere(transform.position, Mathf.Max(0f, radius), ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (SafeZone.ContainsAny(hit.bounds.center)) continue;
            foreach (MonoBehaviour behaviour in hit.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is not IGhostInteractionTarget target || targets.Contains(target) ||
                    !target.CanGhostInteract(context) || cooldowns.ContainsKey(behaviour) ||
                    SafeZone.ContainsAny(target.GhostInteractionPosition) || !CanReachCollider(hit, behaviour)) continue;
                // Hunt policy is enforced here too, so future targets cannot accidentally add new hunt evidence.
                if (context.IsHunting && behaviour is not Door && behaviour is not GhostThrowableItem) continue;
                targets.Add(target);
                if (context.IsHunting && behaviour is Door door)
                {
                    float distance = (hit.ClosestPoint(transform.position) - transform.position).sqrMagnitude;
                    if (distance < nearestDoorDistance) { nearestDoor = door; nearestDoorDistance = distance; }
                }
            }
        }
        if (targets.Count == 0) return false;
        IGhostInteractionTarget chosen = targets[Random.Range(0, targets.Count)];
        if (nearestDoor != null) chosen = nearestDoor;
        Vector3 activityPosition = chosen.GhostInteractionPosition;
        if (!chosen.TryGhostInteract(context)) return false;
        cooldowns[(MonoBehaviour)chosen] = Time.time + Mathf.Max(0.1f, targetCooldown);
        owner.CreateInteractionEvidenceAt(activityPosition);
        return true;
    }
    private bool CanReachCollider(Collider target, MonoBehaviour owner)
    {
        Vector3 origin = transform.position + Vector3.up * 0.3f;
        Vector3 destination = target.ClosestPoint(origin);
        Vector3 delta = destination - origin;
        if (delta.sqrMagnitude < 0.0001f) return true;
        RaycastHit[] hits = Physics.RaycastAll(origin, delta.normalized, delta.magnitude + 0.03f, ~0, QueryTriggerInteraction.Ignore);
        float closest = float.PositiveInfinity;
        Collider first = null;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.distance >= closest) continue;
            closest = hit.distance; first = hit.collider;
        }
        return first == null || first.transform.IsChildOf(owner.transform);
    }
}
