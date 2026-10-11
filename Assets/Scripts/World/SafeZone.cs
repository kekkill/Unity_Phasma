using System.Collections.Generic;
using UnityEngine;

// Safety is spatial state, independent of whether the participant is alive.
[DefaultExecutionOrder(-300)]
[RequireComponent(typeof(BoxCollider))]
public class SafeZone : MonoBehaviour
{
    private static readonly HashSet<SafeZone> zones = new HashSet<SafeZone>();
    private BoxCollider volume;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() { zones.Clear(); }
    private void Awake() { volume = GetComponent<BoxCollider>(); }
    private void OnEnable() { zones.Add(this); }
    private void OnDisable() { zones.Remove(this); }
    public bool Contains(Vector3 position)
    {
        if (!isActiveAndEnabled || volume == null || !volume.enabled) return false;
        Vector3 point = transform.InverseTransformPoint(position) - volume.center;
        Vector3 half = volume.size * 0.5f;
        return Mathf.Abs(point.x) <= half.x && Mathf.Abs(point.y) <= half.y && Mathf.Abs(point.z) <= half.z;
    }
    public static bool ContainsAny(Vector3 position)
    {
        foreach (SafeZone zone in zones)
            if (zone != null && zone.Contains(position)) return true;
        return false;
    }
}
