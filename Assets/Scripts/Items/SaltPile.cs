using System;
using System.Collections.Generic;
using UnityEngine;

// State and navigation notifications. Contact detection and rendering are separate components.
[RequireComponent(typeof(SaltContactArea), typeof(SaltPileVisual))]
public class SaltPile : MonoBehaviour
{
    private static List<SaltPile> active = new List<SaltPile>();
    public static IReadOnlyList<SaltPile> Active => active;
    public static event Action Changed; // Navigation geometry changed, not just appearance.
    public event Action<SaltPile> StateChanged;
    [SerializeField, Min(0.1f)] private float radius = 0.65f;
    [SerializeField] private bool scattered;
    [SerializeField] private bool wasSteppedOn;
    [SerializeField] private Vector3 imprintLocalPosition;
    [SerializeField] private float imprintLocalYaw;
    public float Radius => radius;
    public bool IsIntact => !scattered;
    public bool WasSteppedOn => wasSteppedOn;
    public Vector3 ImprintLocalPosition => imprintLocalPosition;
    public float ImprintLocalYaw => imprintLocalYaw;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() { active = new List<SaltPile>(); Changed = null; }
    private void Awake()
    {
        // Explicit binding also handles components added dynamically by SaltContainer.
        GetComponent<SaltContactArea>().Configure(this);
        GetComponent<SaltPileVisual>().Bind(this);
    }
    private void OnEnable() { if (!active.Contains(this)) active.Add(this); Changed?.Invoke(); }
    private void OnDisable() { active.Remove(this); Changed?.Invoke(); }
    public bool Contains(Vector3 point)
    {
        // Kept for the existing navigation escape rule when salt is poured under the ghost.
        Vector3 delta = point - transform.position;
        return Mathf.Abs(delta.y) < 2f && new Vector2(delta.x, delta.z).sqrMagnitude <= radius * radius;
    }
    public bool MarkSteppedOn(Vector3 contactPosition, Vector3 forward)
    {
        if (!IsIntact || wasSteppedOn) return false;
        Vector3 local = transform.InverseTransformPoint(contactPosition);
        Vector2 offset = Vector2.ClampMagnitude(new Vector2(local.x, local.z), radius * 0.45f);
        imprintLocalPosition = new Vector3(offset.x, 0.035f, offset.y);
        Vector3 direction = transform.InverseTransformDirection(forward);
        imprintLocalYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        wasSteppedOn = true;
        StateChanged?.Invoke(this);
        // Intact marked salt keeps its NavMesh area; do not request a redundant rebake.
        return true;
    }
    public void Scatter()
    {
        if (scattered) return;
        scattered = true;
        StateChanged?.Invoke(this);
        Changed?.Invoke();
        Debug.Log("Ghost scattered salt during hunt", this);
    }
}
