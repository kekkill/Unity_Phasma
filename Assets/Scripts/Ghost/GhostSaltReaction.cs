using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GhostController), typeof(GhostHuntController))]
public class GhostSaltReaction : MonoBehaviour
{
    private GhostController controller;
    private GhostHuntController hunt;
    private HashSet<SaltPile> inside;
    private Vector3 lastFootprint;
    private int footprintsRemaining;
    private void Awake()
    {
        controller = GetComponent<GhostController>();
        hunt = GetComponent<GhostHuntController>();
        inside = new HashSet<SaltPile>();
    }
    private void Update()
    {
        if (controller.Profile == null) return;
        foreach (SaltPile pile in SaltPile.Active)
        {
            if (pile == null) continue;
            if (!pile.Contains(transform.position)) { inside.Remove(pile); continue; }
            bool entered = inside.Add(pile);
            if (!pile.IsIntact) continue;
            if (controller.Profile.SaltResponse == GhostSaltResponse.AvoidUntilHunt && hunt.IsHunting)
                pile.Scatter();
            else if (entered && controller.Profile.SaltResponse == GhostSaltResponse.LeaveFootprints)
            {
                footprintsRemaining = 6;
                lastFootprint = transform.position - transform.forward;
            }
        }
        Vector3 difference = transform.position - lastFootprint;
        difference.y = 0f;
        if (footprintsRemaining > 0 && difference.magnitude > 0.35f)
        {
            Vector3 origin = transform.position + Vector3.up;
            RaycastHit hit = default;
            bool foundFloor = false;
            foreach (RaycastHit candidate in Physics.RaycastAll(origin, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (candidate.collider.GetComponent<RuntimeNavigationWorld>() == null ||
                    (foundFloor && candidate.distance >= hit.distance)) continue;
                hit = candidate;
                foundFloor = true;
            }
            if (foundFloor)
            {
                GameObject print = RuntimeVisuals.Primitive(PrimitiveType.Cube, null,
                    hit.point + Vector3.up * 0.012f, new Vector3(0.12f, 0.015f, 0.25f), Color.gray);
                print.name = "Salt footprint";
                print.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                Destroy(print, 30f);
            }
            lastFootprint = transform.position;
            footprintsRemaining--;
        }
    }
}
