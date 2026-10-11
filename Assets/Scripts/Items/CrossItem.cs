using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PickupItem))]
public class CrossItem : MonoBehaviour
{
    [SerializeField] private bool protectsWhileHeld = true;
    [SerializeField] private bool protectsWhilePlaced = true;
    [SerializeField, Min(0.1f)] private float protectionRadius = 1.8f;
    [SerializeField] private bool broken;
    private PickupItem item;
    private GameObject upright;
    private GameObject beam;
    private static List<CrossItem> active = new List<CrossItem>();
    public bool IsBroken => broken;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() { active = new List<CrossItem>(); }
    private void Awake()
    {
        item = GetComponent<PickupItem>();
        upright = RuntimeVisuals.Primitive(PrimitiveType.Cube, transform,
            new Vector3(0f, 0.42f, 0f), new Vector3(0.1f, 0.8f, 0.08f), Color.yellow);
        beam = RuntimeVisuals.Primitive(PrimitiveType.Cube, transform,
            new Vector3(0f, 0.57f, 0f), new Vector3(0.55f, 0.1f, 0.08f), Color.yellow);
        UpdateVisual();
    }
    private void OnEnable() { if (!active.Contains(this)) active.Add(this); }
    private void OnDisable() { active.Remove(this); }
    public bool IsActive => !broken && ((protectsWhileHeld && item.State == PickupItemState.Held) ||
        (protectsWhilePlaced && item.State == PickupItemState.Placed));

    public bool TryProtect(GhostProfile profile, float anger)
    {
        if (!IsActive || profile == null || profile.IgnoresCross) return false;
        float chance = profile.GetCrossBreakChance(anger);
        if (chance >= 1f || (chance > 0f && Random.value < chance)) broken = true;
        UpdateVisual();
        Debug.Log(broken ? "Cross saved the player and broke" : "Cross saved the player and remains intact", this);
        return true; // The attack being blocked is still blocked even when the cross breaks.
    }
    public static bool ProtectPlayer(GhostPerceptionTarget player, GhostProfile profile, float anger)
    {
        foreach (CrossItem cross in active)
        {
            if (cross == null || !cross.IsActive ||
                Vector3.Distance(cross.transform.position, player.transform.position) > cross.protectionRadius) continue;
            Vector3 delta = player.VisionPosition - cross.transform.position;
            RaycastHit[] hits = Physics.RaycastAll(cross.transform.position, delta.normalized,
                delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
            bool blocked = false;
            foreach (RaycastHit hit in hits)
                if (hit.collider.GetComponentInParent<PickupItem>() == null &&
                    hit.collider.GetComponentInParent<GhostPerceptionTarget>() != player) blocked = true;
            if (!blocked && cross.TryProtect(profile, anger)) return true;
        }
        return false;
    }
    private void UpdateVisual()
    {
        if (upright == null || beam == null) return;
        Color color = broken ? new Color(0.5f, 0.15f, 0.1f) : Color.yellow;
        upright.GetComponent<Renderer>().sharedMaterial.color = color;
        beam.GetComponent<Renderer>().sharedMaterial.color = color;
        beam.transform.localRotation = Quaternion.Euler(0f, 0f, broken ? 45f : 0f);
    }
}
