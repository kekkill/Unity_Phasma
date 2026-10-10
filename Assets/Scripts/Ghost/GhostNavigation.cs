using UnityEngine;
using UnityEngine.AI;

// The actor chooses what to do; this component owns local pathfinding and movement.
[RequireComponent(typeof(NavMeshAgent))]
public class GhostNavigation : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float sampleRadius = 1f;
    [SerializeField, Min(1)] private int pointAttempts = 16;
    private NavMeshAgent agent;
    private NavMeshPath candidatePath;
    [Header("Runtime diagnostics")]
    [SerializeField] private bool hasDestination;
    [SerializeField] private Vector3 requestedDestination;
    [SerializeField] private string lastMoveResult = "Not initialized";
    private int pathAssignedFrame;
    private Vector3 lastProgressPosition;
    private float lastProgressTime;
    public bool HasStalled => IsReady && hasDestination && !HasArrived &&
        !agent.pathPending && Time.time - lastProgressTime > 2f;

    public Vector3 RequestedDestination => requestedDestination;
    public string LastMoveResult => lastMoveResult;
    public bool IsReady => agent != null && agent.enabled && agent.isOnNavMesh;
    public bool HasArrived
    {
        get
        {
            if (!IsReady || !hasDestination || agent.pathPending ||
                agent.pathStatus != NavMeshPathStatus.PathComplete) return false;
            // remainingDistance may still be zero immediately after SetPath.
            // Arrival requires the ghost to physically reach the requested point.
            Vector3 delta = transform.position - requestedDestination;
            delta.y = 0f;
            float tolerance = agent.stoppingDistance + 0.05f;
            return delta.sqrMagnitude <= tolerance * tolerance;
        }
    }
    public bool HasFailedPath => !IsReady || (hasDestination &&
        Time.frameCount > pathAssignedFrame + 2 && !agent.pathPending &&
        ((!agent.hasPath && !HasArrived) || agent.pathStatus != NavMeshPathStatus.PathComplete));

    private void Update()
    {
        if (!IsReady || !hasDestination) return;
        Vector3 progress = transform.position - lastProgressPosition;
        progress.y = 0f;
        if (progress.sqrMagnitude > 0.0025f)
        {
            lastProgressPosition = transform.position;
            lastProgressTime = Time.time;
        }
    }

    private void Awake()
    {
        candidatePath = new NavMeshPath();
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();
        agent.enabled = false;
        agent.radius = 0.35f;
        agent.height = 1.8f;
        agent.baseOffset = 0.59f;
        agent.stoppingDistance = 0.1f;
        agent.acceleration = 12f;
        agent.angularSpeed = 360f;
    }

    public bool Initialize(Room room)
    {
        // Room points have arbitrary height. Project them to nearby walkable ground.
        for (int i = 0; i < pointAttempts; i++)
        {
            if (!NavMesh.SamplePosition(room.GetRandomFloorPoint(), out NavMeshHit hit,
                sampleRadius, agent.areaMask) || !room.ContainsPoint(hit.position))
                continue;
            transform.position = hit.position;
            agent.enabled = true;
            if (agent.isOnNavMesh && agent.Warp(hit.position))
            {
                Stop();
                return true;
            }
            agent.enabled = false;
        }
        Debug.LogError("GhostNavigation: no walkable spawn in the ghost room. Check navigation geometry.", this);
        return false;
    }

    public bool TryReachablePoint(Vector3 position, out Vector3 point)
    {
        point = default;
        if (!IsReady || !NavMesh.SamplePosition(position, out NavMeshHit hit,
            sampleRadius, agent.areaMask)) return false;
        // Nonserialized runtime references can be lost after a script/domain reload.
        if (candidatePath == null) candidatePath = new NavMeshPath();
        if (!agent.CalculatePath(hit.position, candidatePath) ||
            candidatePath.status != NavMeshPathStatus.PathComplete) return false;
        point = hit.position;
        return true;
    }

    private bool PathStaysInRoom(Room room)
    {
        // Rooms are convex boxes: if all corners are inside, every path segment is inside.
        foreach (Vector3 corner in candidatePath.corners)
            if (!room.ContainsPoint(corner)) return false;
        return true;
    }

    public bool TryRoomPoint(Room room, bool allowTransition, out Vector3 point)
    {
        point = default;
        if (room == null) return false;
        for (int i = 0; i < pointAttempts; i++)
        {
            if (TryReachablePoint(room.GetRandomFloorPoint(), out point) &&
                room.ContainsPoint(point) && (allowTransition || PathStaysInRoom(room))) return true;
        }
        return false;
    }

    public bool TryRoomDestination(Room room, float speed, bool allowTransition = false)
    {
        if (!TryRoomPoint(room, allowTransition, out Vector3 point))
        {
            lastMoveResult = "No reachable confined room point: " + (room != null ? room.RoomName : "missing room");
            return false;
        }
        if (!MoveTo(point, speed)) return false;
        // A confined roam must not automatically reroute through another room.
        agent.autoRepath = allowTransition;
        return true;
    }

    public bool TrySearchPoint(Vector3 center, float radius, out Vector3 point)
    {
        for (int i = 0; i < pointAttempts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            if (TryReachablePoint(center + new Vector3(offset.x, 0f, offset.y), out point) &&
                Vector3.Distance(point, center) <= radius + sampleRadius) return true;
        }
        point = default;
        return false;
    }

    public bool MoveTo(Vector3 destination, float speed)
    {
        if (!TryReachablePoint(destination, out Vector3 point))
        {
            Stop();
            lastMoveResult = "Destination cannot be reached";
            return false;
        }
        agent.autoRepath = true;
        agent.speed = Mathf.Max(0f, speed);
        agent.isStopped = false;
        // Reuse the path already calculated instead of restarting it every frame.
        hasDestination = agent.SetPath(candidatePath);
        if (hasDestination)
        {
            requestedDestination = point;
            pathAssignedFrame = Time.frameCount;
            lastProgressPosition = transform.position;
            lastProgressTime = Time.time;
            lastMoveResult = "Path accepted";
        }
        else
        {
            Stop();
            lastMoveResult = "Agent rejected path";
        }
        return hasDestination;
    }

    public void Stop()
    {
        hasDestination = false;
        if (!IsReady) return;
        agent.isStopped = true;
        agent.ResetPath();
    }
}
