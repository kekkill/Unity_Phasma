using System.Collections.Generic;
using UnityEngine;

public class GhostActionRestrictions : MonoBehaviour
{
    private readonly Dictionary<GhostActionType, int>
        blockCounts = new();

    public void BlockAction(
        GhostActionType actionType)
    {
        if (!blockCounts.ContainsKey(actionType))
        {
            blockCounts[actionType] = 0;
        }

        blockCounts[actionType]++;

        Debug.Log(
            "Ghost action blocked: " +
            actionType
        );
    }

    public void UnblockAction(
        GhostActionType actionType)
    {
        if (!blockCounts.ContainsKey(actionType))
        {
            return;
        }

        blockCounts[actionType]--;

        if (blockCounts[actionType] <= 0)
        {
            blockCounts.Remove(actionType);
        }

        Debug.Log(
            "Ghost action unblocked: " +
            actionType
        );
    }

    public bool IsActionBlocked(
        GhostActionType actionType)
    {
        return
            blockCounts.ContainsKey(actionType);
    }
}