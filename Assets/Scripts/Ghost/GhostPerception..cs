using UnityEngine;

public class GhostPerception : MonoBehaviour
{
    [SerializeField]
    private GhostPerceptionTarget target;

    [SerializeField] private GameSession session;

    [Header("Vision")]
    [SerializeField] private float eyeHeight = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool canSeeTarget;
    [SerializeField] private bool canHearTarget;
    [SerializeField] private bool hasKnownTargetPosition;

    [SerializeField] private float timeSinceLastDetection;

    private GhostController ghostController;

    public Room LastKnownRoom { get; private set; }
    private Vector3 lastKnownPosition;
    private float lastDetectionTime = float.NegativeInfinity;

    private bool previousCanSeeTarget;
    private bool previousCanHearTarget;

    public bool CanSeeTarget =>
        canSeeTarget;

    public bool CanHearTarget =>
        canHearTarget;

    public bool HasDetectedTarget =>
        canSeeTarget ||
        canHearTarget;

    public bool HasKnownTargetPosition =>
        hasKnownTargetPosition;

    public Vector3 LastKnownPosition =>
        lastKnownPosition;

    public GhostPerceptionTarget Target =>
        target;

    private void Awake()
    {
        ghostController =
            GetComponent<GhostController>();
    }

    private void Update()
    {
        RefreshTarget();
        if (ghostController == null ||
            ghostController.Profile == null ||
            target == null || !target.CanBeDetected)
        {
            canSeeTarget = false;
            canHearTarget = false;
            hasKnownTargetPosition = false;

            return;
        }

        canSeeTarget =
            CheckVision(target);

        canHearTarget =
            CheckHearing(target);

        UpdateTargetMemory();

        WriteDebugMessages();

        previousCanSeeTarget =
            canSeeTarget;

        previousCanHearTarget =
            canHearTarget;
    }

    public void RefreshTarget()
    {
        GhostPerceptionTarget selected = target != null && target.CanBeDetected ? target : null;
        if (session != null && ghostController != null && ghostController.Profile != null)
        {
            selected = null;
            GhostPerceptionTarget visible = null;
            GhostPerceptionTarget audible = null;
            float visibleDistance = float.PositiveInfinity;
            float audibleDistance = float.PositiveInfinity;
            foreach (GhostPerceptionTarget candidate in session.LivingTargets())
            {
                if (!candidate.CanBeDetected) continue;
                if (candidate == target) selected = candidate;
                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (CheckVision(candidate) && distance < visibleDistance)
                { visible = candidate; visibleDistance = distance; }
                else if (CheckHearing(candidate) && distance < audibleDistance)
                { audible = candidate; audibleDistance = distance; }
            }
            if (visible != null || audible != null) selected = visible != null ? visible : audible;
        }
        if (selected == target && selected != null) return;
        target = selected;
        canSeeTarget = false;
        canHearTarget = false;
        hasKnownTargetPosition = false;
        LastKnownRoom = null;
        lastDetectionTime = float.NegativeInfinity;
        lastKnownPosition = default;
        previousCanSeeTarget = false;
        previousCanHearTarget = false;
        if (target != null && ghostController != null && ghostController.Profile != null)
        {
            canSeeTarget = CheckVision(target);
            canHearTarget = CheckHearing(target);
            UpdateTargetMemory();
        }
    }

    private void UpdateTargetMemory()
    {
        if (HasDetectedTarget)
        {
            // Remember only observed information, never query the hidden player during search.
            LastKnownRoom = ghostController.FindRoomAt(target.transform.position);
            lastKnownPosition =
                target.transform.position;

            lastDetectionTime =
                Time.time;

            hasKnownTargetPosition =
                true;

            timeSinceLastDetection =
                0f;

            return;
        }

        timeSinceLastDetection =
            Time.time -
            lastDetectionTime;

        if (timeSinceLastDetection >
            ghostController.Profile.TargetMemoryDuration)
        {
            hasKnownTargetPosition =
                false;
        }
    }

    private bool CheckVision(GhostPerceptionTarget candidate)
    {
        if (candidate == null || !candidate.CanBeDetected) return false;
        Vector3 origin =
            transform.position +
            Vector3.up * eyeHeight;

        Vector3 targetPosition =
            candidate.VisionPosition;

        Vector3 directionToTarget =
            targetPosition - origin;

        float distance =
            directionToTarget.magnitude;

        if (distance >
            ghostController.Profile.VisionRange)
        {
            return false;
        }

        Vector3 flatDirection =
            directionToTarget;

        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        float angle =
            Vector3.Angle(
                transform.forward,
                flatDirection.normalized
            );

        if (angle >
            ghostController.Profile.VisionAngle * 0.5f)
        {
            return false;
        }

        if (!Physics.Raycast(
            origin,
            directionToTarget.normalized,
            out RaycastHit hit,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        GhostPerceptionTarget hitTarget =
            hit.collider
                .GetComponentInParent<GhostPerceptionTarget>();

        return
            hitTarget == candidate;
    }

    private bool CheckHearing(GhostPerceptionTarget candidate)
    {
        if (candidate == null || !candidate.CanBeDetected) return false;
        float noiseLevel =
            candidate.NoiseLevel;

        if (noiseLevel <= 0f)
        {
            return false;
        }

        float effectiveHearingRange =
            ghostController.Profile.HearingRange *
            noiseLevel;

        float distance =
            Vector3.Distance(
                transform.position,
                candidate.transform.position
            );

        return
            distance <= effectiveHearingRange;
    }

    private void WriteDebugMessages()
    {
        if (canSeeTarget &&
            !previousCanSeeTarget)
        {
            Debug.Log(
                "Ghost sees the player."
            );
        }

        if (canHearTarget &&
            !previousCanHearTarget)
        {
            Debug.Log(
                "Ghost hears the player."
            );
        }
    }
}