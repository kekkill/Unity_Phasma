using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public readonly struct SaltContact
{
    public SaltPile Pile { get; }
    public Vector3 Position { get; }
    public bool IsEntry { get; }
    public SaltContact(SaltPile pile, Vector3 position, bool isEntry)
    { Pile = pile; Position = position; IsEntry = isEntry; }
}

// Physical queries at the feet, independent of profile rules and cosmetic meshes.
[DefaultExecutionOrder(50)]
public class GhostSaltContactSensor : MonoBehaviour
{
    [SerializeField] private Transform footPoint;
    [SerializeField, Min(0.01f)] private float footRadius = 0.10f;
    [SerializeField, Min(0f)] private float footHeight = 0.05f;
    [SerializeField, Min(0.1f)] private float maxSweepDistance = 2f;
    private NavMeshAgent agent;
    private GhostActor actor;
    private HashSet<SaltPile> inside = new HashSet<SaltPile>();
    private HashSet<SaltPile> current = new HashSet<SaltPile>();
    private readonly HashSet<SaltPile> emitted = new HashSet<SaltPile>();
    private Vector3 previousFeet;
    private bool hasPreviousFeet;
    public event Action<SaltContact> Contacted;
    public Vector3 FeetPosition => footPoint != null ? footPoint.position :
        transform.position + Vector3.up * (footHeight - (agent != null ? agent.baseOffset : 0f));
    private void Awake() { agent = GetComponent<NavMeshAgent>(); actor = GetComponent<GhostActor>(); }
    private void OnDisable() { inside.Clear(); current.Clear(); hasPreviousFeet = false; }
    private void Update() { SampleContacts(); }
    public void SampleContacts()
    {
        if (!isActiveAndEnabled || (actor != null && !actor.enabled))
        { inside.Clear(); hasPreviousFeet = false; return; }
        Vector3 feet = FeetPosition;
        current.Clear(); emitted.Clear();
        foreach (Collider hit in Physics.OverlapSphere(feet, footRadius, ~0, QueryTriggerInteraction.Collide))
        {
            if (!hit.TryGetComponent(out SaltContactArea area) ||
                !area.IntersectsFootPath(feet, feet, footRadius, out Vector3 contact)) continue;
            current.Add(area.Pile);
            Emit(area.Pile, contact);
        }
        Vector3 delta = feet - previousFeet;
        if (hasPreviousFeet && delta.sqrMagnitude > 0.000001f && delta.magnitude <= maxSweepDistance)
        {
            // A fast step may cross a pile between two updates. Large teleports do not count.
            foreach (RaycastHit hit in Physics.SphereCastAll(previousFeet, footRadius, delta.normalized,
                delta.magnitude, ~0, QueryTriggerInteraction.Collide))
                if (hit.collider.TryGetComponent(out SaltContactArea area) &&
                    area.IntersectsFootPath(previousFeet, feet, footRadius, out Vector3 contact))
                    Emit(area.Pile, contact);
        }
        HashSet<SaltPile> old = inside; inside = current; current = old;
        previousFeet = feet; hasPreviousFeet = true;
    }
    private void Emit(SaltPile pile, Vector3 position)
    {
        if (emitted.Add(pile)) Contacted?.Invoke(new SaltContact(pile, position, !inside.Contains(pile)));
    }
}
