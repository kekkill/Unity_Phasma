using System.Collections.Generic;
using UnityEngine;

// Chooses room search goals; GhostNavigation remains the sole owner of movement.
[RequireComponent(typeof(GhostNavigation), typeof(GhostController))]
public class GhostRoomSearch : MonoBehaviour
{
    [SerializeField, Min(1)] private int pointsPerRoom = 2;
    [SerializeField, Min(0.5f)] private float secondsPerRoom = 3f;
    [SerializeField] private Room searchRoom;
    [SerializeField] private bool transitioning;
    private Room previousRoom;
    private Room blockedRoom;
    private float blockedUntil;
    private GhostNavigation navigation;
    private GhostController controller;
    private bool hasGoal;
    private int pointsVisited;
    private float roomDeadline;
    private float nextRetry;
    private List<Room> candidates;

    private void Awake()
    {
        candidates = new List<Room>();
        navigation = GetComponent<GhostNavigation>();
        controller = GetComponent<GhostController>();
    }

    public void Begin(Vector3 lastKnownPosition, Room lastKnownRoom, float speed)
    {
        previousRoom = null;
        blockedRoom = null;
        searchRoom = lastKnownRoom != null ? lastKnownRoom : controller.FindRoomAt(lastKnownPosition);
        if (searchRoom == null) searchRoom = controller.FindRoomAt(transform.position);
        transitioning = searchRoom != null && !searchRoom.ContainsPoint(transform.position);
        pointsVisited = 0;
        roomDeadline = Time.time + secondsPerRoom;
        nextRetry = Time.time;
        hasGoal = navigation.MoveTo(lastKnownPosition, speed);
    }

    public void Tick(float speed)
    {
        if (searchRoom == null || Time.time < nextRetry) return;
        if (transitioning)
        {
            if (searchRoom.ContainsPoint(transform.position))
            {
                transitioning = false;
                roomDeadline = Time.time + secondsPerRoom;
                pointsVisited = 0;
            }
            else if (!hasGoal || navigation.HasFailedPath || navigation.HasStalled || navigation.HasArrived)
            {
                // A door can invalidate a transition after it has already started.
                // Stop targeting the unreachable room and search where the ghost actually is.
                Room current = controller.FindRoomAt(transform.position);
                if (current != null)
                {
                    blockedRoom = searchRoom;
                    blockedUntil = Time.time + secondsPerRoom;
                    searchRoom = current;
                    previousRoom = null;
                    transitioning = false;
                    pointsVisited = 0;
                    roomDeadline = Time.time + secondsPerRoom;
                    hasGoal = navigation.TryRoomDestination(searchRoom, speed);
                    Debug.Log("Ghost search: passage blocked, searching " + searchRoom.RoomName, this);
                }
                else hasGoal = navigation.TryRoomDestination(searchRoom, speed, true);
                nextRetry = Time.time + 0.5f;
            }
            return;
        }
        if (hasGoal && navigation.HasArrived)
        {
            pointsVisited++;
            hasGoal = false;
        }
        if (pointsVisited >= pointsPerRoom || Time.time >= roomDeadline)
        {
            if (TryNextRoom(speed)) return;
            // Closed passages may be unreachable. Stay here and retry, without crossing walls.
            pointsVisited = 0;
            roomDeadline = Time.time + secondsPerRoom;
        }
        if (!hasGoal || navigation.HasArrived || navigation.HasFailedPath || navigation.HasStalled)
        {
            hasGoal = navigation.TryRoomDestination(searchRoom, speed);
            nextRetry = Time.time + 0.5f;
        }
    }

    private bool TryNextRoom(float speed)
    {
        candidates.Clear();
        foreach (Room room in searchRoom.ConnectedRooms)
            if (room != null && room != searchRoom &&
                !(room == blockedRoom && Time.time < blockedUntil) && !candidates.Contains(room)) candidates.Add(room);
        // Prefer a new neighbor; allow returning when this is a dead end.
        if (candidates.Count > 1) candidates.Remove(previousRoom);
        while (candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            Room next = candidates[index];
            candidates.RemoveAt(index);
            if (!navigation.TryRoomDestination(next, speed, true)) continue;
            previousRoom = searchRoom;
            searchRoom = next;
            transitioning = true;
            hasGoal = true;
            nextRetry = Time.time;
            Debug.Log("Ghost search -> " + searchRoom.RoomName, this);
            return true;
        }
        if (previousRoom != null)
        {
            foreach (Room room in searchRoom.ConnectedRooms)
            {
                if (room != previousRoom || (room == blockedRoom && Time.time < blockedUntil) ||
                    !navigation.TryRoomDestination(room, speed, true)) continue;
                Room old = searchRoom;
                searchRoom = room;
                previousRoom = old;
                transitioning = true;
                hasGoal = true;
                nextRetry = Time.time;
                Debug.Log("Ghost search -> " + searchRoom.RoomName, this);
                return true;
            }
        }
        return false;
    }

    public void Stop()
    {
        searchRoom = null;
        hasGoal = false;
        transitioning = false;
        navigation.Stop();
    }
}
