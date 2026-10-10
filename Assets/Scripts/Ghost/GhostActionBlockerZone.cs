using System.Collections.Generic;
using UnityEngine;

public class GhostActionBlockerZone : MonoBehaviour
{
    [SerializeField]
    private GhostActionType actionToBlock =
        GhostActionType.Interaction;

    private readonly HashSet<GhostActionRestrictions>
        affectedGhosts = new();

    private void OnTriggerEnter(
        Collider other)
    {
        GhostActionRestrictions restrictions =
            other.GetComponentInParent<GhostActionRestrictions>();

        if (restrictions == null)
        {
            return;
        }

        if (!affectedGhosts.Add(restrictions))
        {
            return;
        }

        restrictions.BlockAction(
            actionToBlock
        );

        Debug.Log(
            "Protection zone blocked: " +
            actionToBlock
        );
    }

    private void OnTriggerExit(
        Collider other)
    {
        GhostActionRestrictions restrictions =
            other.GetComponentInParent<GhostActionRestrictions>();

        if (restrictions == null)
        {
            return;
        }

        if (!affectedGhosts.Remove(restrictions))
        {
            return;
        }

        restrictions.UnblockAction(
            actionToBlock
        );

        Debug.Log(
            "Protection zone unblocked: " +
            actionToBlock
        );
    }

    private void OnDisable()
    {
        foreach (
            GhostActionRestrictions restrictions
            in affectedGhosts)
        {
            if (restrictions != null)
            {
                restrictions.UnblockAction(
                    actionToBlock
                );
            }
        }

        affectedGhosts.Clear();
    }
}