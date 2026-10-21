using System;
using System.Collections.Generic;
using UnityEngine;

// Spatial hiding boundary, deliberately separate from the invulnerable SafeZone.
[DefaultExecutionOrder(-250)]
[RequireComponent(typeof(BoxCollider))]
public class HidingZone : MonoBehaviour
{
    private static readonly HashSet<HidingZone> active = new HashSet<HidingZone>();
    public static IReadOnlyCollection<HidingZone> Active => active;
    public static event Action Changed;
    [SerializeField] private Door door;
    [SerializeField] private Transform approachPoint;
    private BoxCollider volume;
    public Door Door => door;
    public Vector3 ApproachPosition => approachPoint != null ? approachPoint.position : transform.position;
    public Vector3 InvestigationPosition => transform.TransformPoint(Volume.center - Vector3.up * (Volume.size.y * 0.5f - 0.6f));
    public BoxCollider Volume => volume != null ? volume : volume = GetComponent<BoxCollider>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { active.Clear(); Changed = null; }
    private void Awake() { volume = GetComponent<BoxCollider>(); volume.isTrigger = true; }
    private void OnEnable() { active.Add(this); Changed?.Invoke(); }
    private void OnDisable() { active.Remove(this); Changed?.Invoke(); }
    public bool Contains(Vector3 point)
    {
        if (!isActiveAndEnabled || !Volume.enabled) return false;
        Vector3 local = transform.InverseTransformPoint(point) - Volume.center;
        Vector3 half = Volume.size * 0.5f;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.z) <= half.z && Mathf.Abs(local.y) <= half.y;
    }
    public static HidingZone FindAt(Vector3 point)
    {
        foreach (var zone in Active) if (zone != null && zone.Contains(point)) return zone;
        return null;
    }
    // Ground paths use the horizontal footprint (NavMesh surface height can differ slightly from the trigger bottom).
    public bool CrossesFootprint(Vector3 start, Vector3 end)
    {
        start = transform.InverseTransformPoint(start) - Volume.center;
        end = transform.InverseTransformPoint(end) - Volume.center;
        Vector3 half = Volume.size * 0.5f;
        float near = 0f, far = 1f;
        for (int axis = 0; axis < 3; axis += 2)
        {
            float delta = end[axis] - start[axis];
            if (Mathf.Abs(delta) < 0.0001f) { if (Mathf.Abs(start[axis]) > half[axis]) return false; }
            else
            {
                float a = (-half[axis] - start[axis]) / delta, b = (half[axis] - start[axis]) / delta;
                near = Mathf.Max(near, Mathf.Min(a, b)); far = Mathf.Min(far, Mathf.Max(a, b));
                if (near > far) return false;
            }
        }
        return true;
    }
    public bool ContainsFootprint(Vector3 point)
    {
        Vector3 local = transform.InverseTransformPoint(point) - Volume.center;
        return Mathf.Abs(local.x) <= Volume.size.x * 0.5f && Mathf.Abs(local.z) <= Volume.size.z * 0.5f;
    }
}
