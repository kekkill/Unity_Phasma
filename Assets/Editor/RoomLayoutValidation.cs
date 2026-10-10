#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class RoomLayoutValidation
{
    [MenuItem("Phasma/Validate Room Layout (Play Mode)")]
    public static void Validate()
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("Open RoomSearchPrototype and enter Play Mode first.");
        Room[] rooms = UnityEngine.Object.FindObjectsByType<Room>();
        if (rooms.Length != 3) throw new InvalidOperationException("Expected exactly three room zones.");
        int paths = 0;
        foreach (Room room in rooms)
        {
            foreach (Room neighbor in room.ConnectedRooms)
            {
                if (neighbor == null || Array.IndexOf(neighbor.ConnectedRooms, room) < 0)
                    throw new InvalidOperationException("Missing reciprocal room connection: " + room.RoomName);
            }
            foreach (Room target in rooms)
            {
                if (room == target) continue;
                if (!NavMesh.SamplePosition(room.FloorCenter, out NavMeshHit from, 1f, NavMesh.AllAreas) ||
                    !NavMesh.SamplePosition(target.FloorCenter, out NavMeshHit to, 1f, NavMesh.AllAreas))
                    throw new InvalidOperationException("Room center is not walkable.");
                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path) ||
                    path.status != NavMeshPathStatus.PathComplete)
                    throw new InvalidOperationException("No complete route: " + room.RoomName + " -> " + target.RoomName + ". Check whether the door is open.");
                foreach (Vector3 corner in path.corners)
                {
                    bool inside = false;
                    foreach (Room zone in rooms) inside |= zone.ContainsPoint(corner);
                    if (!inside) throw new InvalidOperationException("Route leaves all configured rooms.");
                }
                paths++;
            }
        }
        if (!NavMesh.SamplePosition(new Vector3(-3f, 0f, 2f), out NavMeshHit left, 0.5f, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(new Vector3(3f, 0f, 2f), out NavMeshHit right, 0.5f, NavMesh.AllAreas))
            throw new InvalidOperationException("Wall-test start/end are not walkable.");
        if (!NavMesh.Raycast(left.position, right.position, out NavMeshHit block, NavMesh.AllAreas))
            throw new InvalidOperationException("A straight route crosses the partition; wall geometry is missing from the NavMesh.");
        Debug.Log("Room layout validation PASSED: " + paths + " complete routes, reciprocal connections, and a blocked straight route through the partitions. Actual ghost behavior still needs a play test.");
    }
}
#endif
