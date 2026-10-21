using UnityEngine;

[RequireComponent(typeof(GhostHearing), typeof(GhostHidingKnowledge))]
public class GhostPerception : MonoBehaviour
{
    [SerializeField] private GhostPerceptionTarget target;
    [SerializeField] private GameSession session;
    [Header("Vision")]
    [SerializeField] private float eyeHeight = 1.5f;
    [Header("Debug")]
    [SerializeField] private bool canSeeTarget;
    [SerializeField] private bool canHearTarget;
    [SerializeField] private bool hasKnownTargetPosition;
    [SerializeField] private float timeSinceLastDetection;
    [SerializeField] private Vector3 lastHeardPosition;
    private GhostController ghostController;
    private GhostHearing hearing;
    private GhostHidingKnowledge hiding;
    private Vector3 lastKnownPosition;
    private float lastDetectionTime = float.NegativeInfinity;
    private bool previousCanSeeTarget, previousCanHearTarget;
    public Room LastKnownRoom { get; private set; }
    public HidingZone LastKnownHidingZone { get; private set; }
    public bool CanSeeTarget => canSeeTarget;
    public bool CanHearTarget => canHearTarget;
    public bool HasDetectedTarget => canSeeTarget || canHearTarget;
    public bool HasKnownTargetPosition => hasKnownTargetPosition;
    public Vector3 LastKnownPosition => lastKnownPosition;
    public Vector3 LastHeardPosition => lastHeardPosition;
    public GhostPerceptionTarget Target => target;
    private void Awake()
    {
        ghostController = GetComponent<GhostController>();
        hearing = GetComponent<GhostHearing>(); hiding = GetComponent<GhostHidingKnowledge>();
    }
    private void Update()
    {
        RefreshTarget();
        if (canSeeTarget && !previousCanSeeTarget) Debug.Log("Ghost sees the player.");
        if (canHearTarget && !previousCanHearTarget) Debug.Log("Ghost heard noise at " + lastHeardPosition, this);
        previousCanSeeTarget = canSeeTarget; previousCanHearTarget = canHearTarget;
    }
    private bool Eligible(GhostPerceptionTarget candidate) => candidate != null && candidate.CanBeDetected &&
        (session == null ? candidate == target : candidate.LifeState != null && candidate.LifeState.Session == session && !session.HasEnded);
    public void RefreshTarget()
    {
        if (ghostController == null || ghostController.Profile == null || (session != null && session.HasEnded))
        { ClearMemory(); target = null; return; }
        GhostPerceptionTarget visible = null, audible = null;
        float nearestVisible = float.PositiveInfinity, bestSound = -1f;
        Vector3 heardPosition = default;
        HidingZone heardZone = null;
        if (session != null)
        {
            foreach (var candidate in session.LivingTargets())
                SampleCandidate(candidate, ref visible, ref nearestVisible, ref audible, ref bestSound, ref heardPosition, ref heardZone);
        }
        else if (target != null)
            SampleCandidate(target, ref visible, ref nearestVisible, ref audible, ref bestSound, ref heardPosition, ref heardZone);
        foreach (WorldNoiseEvent sound in WorldNoise.Recent)
        {
            if (!Eligible(sound.Source) || !hearing.TryHear(HearingPosition, sound.Position, sound.Source, sound.Loudness,
                ghostController.Profile.HearingRange, out float strength) || strength <= bestSound) continue;
            audible = sound.Source; bestSound = strength; heardPosition = sound.Position; heardZone = sound.OriginHidingZone;
        }
        GhostPerceptionTarget selected = visible != null ? visible : audible != null ? audible : Eligible(target) ? target : null;
        if (selected != target) ClearMemory();
        target = selected;
        canSeeTarget = visible != null && visible == target;
        canHearTarget = audible != null && audible == target;
        if (canHearTarget) lastHeardPosition = heardPosition;
        if (target == null) { ClearMemory(); return; }
        if (canSeeTarget)
            Remember(target.transform.position, HidingZone.FindAt(target.transform.position));
        else if (canHearTarget)
        {
            Remember(heardPosition, heardZone);
        }
        else
        {
            timeSinceLastDetection = Time.time - lastDetectionTime;
            if (timeSinceLastDetection > ghostController.Profile.TargetMemoryDuration)
            { hasKnownTargetPosition = false; LastKnownHidingZone = null; }
        }
    }
    private Vector3 HearingPosition => transform.position + Vector3.up * 0.4f;
    private void SampleCandidate(GhostPerceptionTarget candidate, ref GhostPerceptionTarget visible, ref float nearestVisible,
        ref GhostPerceptionTarget audible, ref float bestSound, ref Vector3 position, ref HidingZone zone)
    {
        if (!Eligible(candidate)) return;
        float distance = (candidate.transform.position - transform.position).sqrMagnitude;
        if (CheckVision(candidate) && distance < nearestVisible) { visible = candidate; nearestVisible = distance; }
        Vector3 source = candidate.transform.position + Vector3.up * 0.4f;
        if (hearing.TryHear(HearingPosition, source, candidate, candidate.NoiseLevel, ghostController.Profile.HearingRange,
            out float strength) && strength > bestSound)
        { audible = candidate; bestSound = strength; position = candidate.transform.position; zone = HidingZone.FindAt(position); }
    }
    private void Remember(Vector3 position, HidingZone zone)
    {
        lastKnownPosition = position; LastKnownRoom = ghostController.FindRoomAt(position); LastKnownHidingZone = zone;
        lastDetectionTime = Time.time; timeSinceLastDetection = 0f; hasKnownTargetPosition = true;
        hiding.Observe(zone, ghostController.Profile.TargetMemoryDuration);
    }
    private void ClearMemory()
    {
        canSeeTarget = false; canHearTarget = false; hasKnownTargetPosition = false;
        LastKnownRoom = null; LastKnownHidingZone = null;
        lastDetectionTime = float.NegativeInfinity; lastKnownPosition = default;
        previousCanSeeTarget = false; previousCanHearTarget = false;
    }
    private bool CheckVision(GhostPerceptionTarget candidate)
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 delta = candidate.VisionPosition - origin;
        if (delta.magnitude > ghostController.Profile.VisionRange) return false;
        Vector3 flat = Vector3.ProjectOnPlane(delta, Vector3.up);
        if (flat.sqrMagnitude > 0.001f && Vector3.Angle(transform.forward, flat.normalized) > ghostController.Profile.VisionAngle * 0.5f) return false;
        // Even vertically aligned targets must pass actual occlusion (e.g. the wardrobe roof).
        foreach (var hit in SortedVisionHits(origin, delta))
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<PickupItem>() != null) continue;
            return hit.collider.GetComponentInParent<GhostPerceptionTarget>() == candidate;
        }
        return false;
    }
    private static RaycastHit[] SortedVisionHits(Vector3 origin, Vector3 delta)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, delta.normalized, delta.magnitude + 0.03f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        return hits;
    }
}
