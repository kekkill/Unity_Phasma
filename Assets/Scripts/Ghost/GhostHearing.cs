using System.Collections.Generic;
using UnityEngine;

// Physics-based attenuation, independent of target choice and memory.
public class GhostHearing : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float unconfiguredBarrierTransmission = 0.25f;
    private readonly HashSet<UnityEngine.Object> barriers = new HashSet<UnityEngine.Object>();
    public float Transmission(Vector3 listener, Vector3 origin, GhostPerceptionTarget source)
    {
        barriers.Clear();
        Vector3 delta = origin - listener;
        if (delta.magnitude < 0.05f) return 1f;
        float transmission = 1f;
        foreach (var hit in Physics.RaycastAll(listener, delta.normalized, delta.magnitude - 0.03f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<PickupItem>() != null ||
                hit.collider.GetComponentInParent<GhostPerceptionTarget>() != null) continue;
            AcousticSurface surface = hit.collider.GetComponentInParent<AcousticSurface>();
            UnityEngine.Object barrier = surface != null ? (UnityEngine.Object)surface : hit.collider;
            if (!barriers.Add(barrier)) continue;
            transmission *= surface != null ? surface.Transmission : Mathf.Clamp01(unconfiguredBarrierTransmission);
        }
        return transmission;
    }
    public bool TryHear(Vector3 listener, Vector3 origin, GhostPerceptionTarget source, float loudness,
        float hearingRange, out float strength)
    {
        strength = 0f;
        if (source == null || !source.CanBeDetected || loudness <= 0f) return false;
        float distance = Vector3.Distance(listener, origin);
        if (distance > Mathf.Max(0f, hearingRange) * Mathf.Clamp01(loudness)) return false;
        float effective = Mathf.Clamp01(loudness) * Transmission(listener, origin, source);
        if (effective <= 0f || distance > Mathf.Max(0f, hearingRange) * effective) return false;
        strength = effective / Mathf.Max(0.1f, distance);
        return true;
    }
}
