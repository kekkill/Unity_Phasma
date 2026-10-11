using UnityEngine;

// Temporary ground marks after contact, independent of the permanent SaltPile imprint.
[DefaultExecutionOrder(60)]
[RequireComponent(typeof(GhostSaltContactSensor))]
public class SaltFootprintTrail : MonoBehaviour
{
    [SerializeField, Min(1)] private int stepsPerContact = 6;
    [SerializeField, Min(0.05f)] private float stepDistance = 0.35f;
    [SerializeField, Min(0.1f)] private float lifetime = 30f;
    private GhostSaltContactSensor sensor;
    private GhostActor actor;
    private Vector3 lastFootprint;
    private int remaining;
    private int emittedCount;
    private GameObject trailRoot;
    public int RemainingSteps => remaining;
    public int EmittedCount => emittedCount;
    public int ActiveFootprints => trailRoot != null ? trailRoot.transform.childCount : 0;
    private void Awake() { sensor = GetComponent<GhostSaltContactSensor>(); actor = GetComponent<GhostActor>(); }
    public void BeginTrail(Vector3 feet)
    {
        if (!isActiveAndEnabled) return;
        remaining = stepsPerContact;
        lastFootprint = feet - transform.forward * (stepDistance + 0.05f);
    }
    private void OnDisable() { remaining = 0; }
    private void OnDestroy() { if (trailRoot != null) Destroy(trailRoot); }
    private void Update() { Tick(); }
    public void Tick()
    {
        if (!isActiveAndEnabled || remaining <= 0) return;
        if (actor != null && !actor.enabled) { remaining = 0; return; }
        Vector3 feet = sensor.FeetPosition;
        Vector3 delta = feet - lastFootprint; delta.y = 0f;
        if (delta.magnitude <= stepDistance) return;
        RaycastHit floor = default;
        bool foundFloor = false;
        foreach (RaycastHit candidate in Physics.RaycastAll(feet + Vector3.up, Vector3.down,
            2f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (candidate.collider.GetComponent<RuntimeNavigationWorld>() == null ||
                (foundFloor && candidate.distance >= floor.distance)) continue;
            floor = candidate; foundFloor = true;
        }
        lastFootprint = feet;
        if (!foundFloor) return;
        if (trailRoot == null) trailRoot = new GameObject("Temporary salt footprint trail");
        Vector3 side = transform.right * (emittedCount % 2 == 0 ? -0.08f : 0.08f);
        GameObject print = SaltFootprintVisual.Create(trailRoot.transform,
            floor.point + side + Vector3.up * 0.015f, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), Color.gray);
        Destroy(print, lifetime);
        emittedCount++; remaining--;
    }
}
