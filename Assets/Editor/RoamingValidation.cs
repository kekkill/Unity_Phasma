#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class RoamingValidation
{
    private static bool running;

    [MenuItem("Phasma/Validate Roaming (Play Mode)")]
    public static async void Validate()
    {
        if (!EditorApplication.isPlaying || running)
        {
            Debug.LogWarning("Enter Play Mode first; wait if a validation is already running.");
            return;
        }
        running = true;
        GameObject probe = null;
        try
        {
            Room[] rooms = UnityEngine.Object.FindObjectsByType<Room>();
            int tested = 0;
            foreach (Room room in rooms)
            {
                if (!room.CanBeGhostRoom) continue;
                probe = new GameObject("Temporary roaming validation agent");
                GhostNavigation navigation = probe.AddComponent<GhostNavigation>();
                if (!navigation.Initialize(room))
                    throw new InvalidOperationException("Cannot initialize navigation in " + room.RoomName);
                bool assigned = false;
                for (int attempt = 0; attempt < 16; attempt++)
                {
                    if (!navigation.TryRoomDestination(room, 1.5f)) continue;
                    Vector3 delta = navigation.RequestedDestination - probe.transform.position;
                    delta.y = 0f;
                    if (delta.magnitude >= 1f) { assigned = true; break; }
                }
                if (!assigned)
                    throw new InvalidOperationException("No nontrivial confined roam in " + room.RoomName + ": " + navigation.LastMoveResult);
                float deadline = Time.realtimeSinceStartup + 12f;
                while (!navigation.HasArrived && Time.realtimeSinceStartup < deadline)
                {
                    if (!EditorApplication.isPlaying || probe == null)
                        throw new InvalidOperationException("Play Mode stopped during validation.");
                    if (!room.ContainsPoint(probe.transform.position))
                        throw new InvalidOperationException("Roaming agent left " + room.RoomName);
                    if (navigation.HasFailedPath)
                        throw new InvalidOperationException("Roaming path failed in " + room.RoomName);
                    await Task.Delay(100);
                }
                if (!EditorApplication.isPlaying || probe == null || !navigation.HasArrived)
                    throw new InvalidOperationException("Roaming agent did not reach its target in " + room.RoomName);
                tested++;
                UnityEngine.Object.Destroy(probe);
                probe = null;
            }
            if (tested == 0) throw new InvalidOperationException("No ghost rooms found; zero checks is not a pass.");
            Debug.Log("Roaming validation PASSED: actual movement and arrival in " + tested + " rooms. This checks the navigation component; observe the real GhostActor separately.");
        }
        catch (Exception error)
        {
            Debug.LogError("Roaming validation FAILED: " + error.Message);
        }
        finally
        {
            if (probe != null) UnityEngine.Object.Destroy(probe);
            running = false;
        }
    }
}
#endif
