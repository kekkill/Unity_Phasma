using UnityEngine;

// Reach a discovered hiding place's door before normal chase/search can reach its interior.
[RequireComponent(typeof(GhostNavigation), typeof(GhostHidingKnowledge))]
public class GhostHidingInvestigation : MonoBehaviour
{
    private float nextMove;
    private HidingZone approaching;
    public bool TryApproach(HidingZone zone, float speed)
    {
        if (!isActiveAndEnabled || zone == null || !GetComponent<GhostHidingKnowledge>().CanEnter(zone) ||
            zone.Door == null) { approaching = null; return false; }
        GhostNavigation navigation = GetComponent<GhostNavigation>();
        if (zone.Door.IsFullyOpen && navigation.TryReachablePoint(zone.InvestigationPosition, out _))
        { approaching = null; return false; }
        if (zone != approaching || Time.time >= nextMove)
        {
            approaching = zone; nextMove = Time.time + 0.3f;
            if (!navigation.MoveTo(zone.ApproachPosition, speed)) return false;
        }
        // Hunt object-interaction tick opens the authorized door only when physically nearby.
        return true;
    }
}
