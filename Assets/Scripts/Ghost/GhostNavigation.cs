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
    private bool hasDestination;

    public bool IsReady => agent != null && agent.enabled && agent.isOnNavMesh;
    public bool HasArrived => IsReady && hasDestination && !agent.pathPending &&
        agent.pathStatus == NavMeshPathStatus.PathComplete &&
        agent.remainingDistance <= agent.stoppingDistance + 0.05f;
    public bool HasFailedPath => !IsReady || (hasDestination && !agent.pathPending &&
        (!agent.hasPath && !HasArrived || agent.pathStatus != NavMeshPathStatus.PathComplete));

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

    public bool TryRoomDestination(Room room, float speed)
    {
        for (int i = 0; i < pointAttempts; i++)
        {
            if (TryReachablePoint(room.GetRandomFloorPoint(), out Vector3 point) &&
                room.ContainsPoint(point) && MoveTo(point, speed)) return true;
        }
        return false;
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
            return false;
        }
        agent.speed = Mathf.Max(0f, speed);
        agent.isStopped = false;
        // Reuse the path already calculated instead of restarting it every frame.
        hasDestination = agent.SetPath(candidatePath);
        if (!hasDestination) Stop();
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
