using System.Collections.Generic;
using UnityEngine;

// Per-ghost, temporary knowledge acquired only from a successful sensory observation.
public class GhostHidingKnowledge : MonoBehaviour
{
    private readonly Dictionary<HidingZone, float> knownUntil = new Dictionary<HidingZone, float>();
    public void Observe(HidingZone zone, float duration)
    {
        if (zone != null && zone.isActiveAndEnabled) knownUntil[zone] = Time.time + Mathf.Max(0.1f, duration);
    }
    public bool CanEnter(HidingZone zone) => zone == null || !zone.isActiveAndEnabled ||
        (knownUntil.TryGetValue(zone, out float until) && Time.time < until);
    public bool CanEnterAt(Vector3 point) => CanEnter(HidingZone.FindAt(point));
    public bool HasKnownZone
    {
        get { foreach (var zone in HidingZone.Active) if (CanEnter(zone)) return true; return false; }
    }
    public bool AllowsPath(Vector3 start, Vector3[] corners)
    {
        foreach (var zone in HidingZone.Active)
        {
            if (CanEnter(zone)) continue;
            bool leaving = zone.Contains(start);
            Vector3 previous = start;
            foreach (Vector3 point in corners)
            {
                if (!leaving && zone.CrossesFootprint(previous, point)) return false;
                if (leaving && !zone.ContainsFootprint(point)) leaving = false;
                previous = point;
            }
        }
        return true;
    }
    private void OnDisable() => knownUntil.Clear();
}
