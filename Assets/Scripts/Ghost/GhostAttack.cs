using System;
using UnityEngine;

[RequireComponent(typeof(GhostActionRestrictions))]
public class GhostAttack : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float attackRange = 1.2f;
    [SerializeField] private float attackOriginHeight = 0.9f;
    private GhostActionRestrictions restrictions;
    public bool WasRepelled { get; private set; }
    private void Awake() { restrictions = GetComponent<GhostActionRestrictions>(); }

    public bool TryAttack(GhostPerceptionTarget target)
    {
        WasRepelled = false;
        if (target == null || !target.CanBeDetected || target.LifeState == null ||
            restrictions.IsActionBlocked(GhostActionType.Attack)) return false;
        if (Vector3.Distance(transform.position, target.transform.position) > attackRange) return false;
        HidingZone zone = HidingZone.FindAt(target.transform.position);
        GhostHidingKnowledge knowledge = GetComponent<GhostHidingKnowledge>();
        if (zone != null && (knowledge == null || !knowledge.CanEnter(zone))) return false;
        Vector3 origin = transform.position + Vector3.up * attackOriginHeight;
        Vector3 delta = target.VisionPosition - origin;
        RaycastHit[] hits = Physics.RaycastAll(origin, delta.normalized, delta.magnitude + 0.05f,
            ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<PickupItem>() != null) continue;
            // The first solid object must be this player; a wall/door blocks the attack.
            if (hit.collider.GetComponentInParent<GhostPerceptionTarget>() != target) return false;
            GhostController controller = GetComponent<GhostController>();
            GhostAggression aggression = GetComponent<GhostAggression>();
            if (controller != null && CrossItem.ProtectPlayer(target, controller.Profile,
                aggression != null ? aggression.CurrentAnger : 0f))
            {
                WasRepelled = true;
                return false;
            }
            return target.LifeState.TryDie();
        }
        return false;
    }
}
