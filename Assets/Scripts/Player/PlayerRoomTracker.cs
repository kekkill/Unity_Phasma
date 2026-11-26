using System.Collections.Generic;
using UnityEngine;

public class PlayerRoomTracker : MonoBehaviour
{
    private readonly List<Room> overlaps = new();

    public Room CurrentRoom
    {
        get
        {
            Room current = null;
            float nearestFloor = float.PositiveInfinity;
            Vector3 position = transform.position;
            foreach (var room in overlaps)
            {
                if (room == null || !room.isActiveAndEnabled || !room.ContainsPoint(position)) continue;
                float floorDistance = Mathf.Abs(position.y - room.FloorCenter.y);
                if (floorDistance >= nearestFloor) continue;
                current = room;
                nearestFloor = floorDistance;
            }
            return current;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Room room) && !overlaps.Contains(room))
            overlaps.Add(room);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Room room)) overlaps.Remove(room);
    }

    private void OnDisable() => overlaps.Clear();
}
