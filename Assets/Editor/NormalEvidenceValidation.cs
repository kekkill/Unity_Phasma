#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// Exercise Actor -> confined navigation -> real scene switch -> emitter -> instruments.
public static class NormalEvidenceValidation
{
    private static bool running;
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Assign(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(name).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Number(UnityEngine.Object target, string name, float value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(name).floatValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static GameObject Child(Transform parent, string name, Vector3 position)
    {
        var obj = new GameObject(name); obj.transform.SetParent(parent); obj.transform.position = position;
        return obj;
    }
    private static async Task WaitFor(Func<bool> condition, GhostController ghost, Room room)
    {
        double until = EditorApplication.timeSinceStartup + 20.0;
        while (!condition())
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || Time.timeScale != 1f)
                throw new InvalidOperationException("Use normal unpaused Play Mode; validation was interrupted.");
            if (!room.ContainsPoint(ghost.transform.position))
                throw new InvalidOperationException("Normal activity left its assigned room.");
            if (EditorApplication.timeSinceStartup >= until)
                throw new InvalidOperationException("Normal activity timed out: " + ghost.GetComponent<GhostWorldInteraction>().LastInteractionResult +
                    "; " + ghost.GetComponent<GhostNavigation>().LastMoveResult);
            await Task.Yield();
        }
    }
    private static async Task CheckRoom(Transform parent, Room sourceRoom, LightSwitch sourceSwitch,
        GhostProfile profile, Vector3 center)
    {
        var fixture = Child(parent, "Normal evidence fixture " + sourceRoom.RoomName, center);
        fixture.SetActive(false);
        var roomObj = UnityEngine.Object.Instantiate(sourceRoom.gameObject, fixture.transform);
        roomObj.name = "Validation " + sourceRoom.RoomName; roomObj.transform.position = center;
        Room room = roomObj.GetComponent<Room>();
        var switchObj = UnityEngine.Object.Instantiate(sourceSwitch.gameObject, fixture.transform);
        switchObj.name = "Validation " + sourceRoom.RoomName + " switch";
        switchObj.transform.position = center + sourceSwitch.transform.position - sourceRoom.transform.position;
        LightSwitch target = switchObj.GetComponent<LightSwitch>();
        var light = Child(fixture.transform, "Validation room light", center + Vector3.up * 2.5f).AddComponent<Light>();
        Assign(target, "targetLight", light);
        // Explicit local navigation sources, unrelated to the running mission.
        var floor = Child(fixture.transform, "Validation floor", center);
        var floorBox = floor.AddComponent<BoxCollider>(); floorBox.size = new Vector3(6f, 0.2f, 6f);
        floorBox.center = Vector3.down * 0.1f;
        var geometry = new Collider[5]; geometry[0] = floorBox;
        for (int i = 0; i < 4; i++)
        {
            bool xWall = i < 2;
            Vector3 offset = xWall ? new Vector3(i == 0 ? -3f : 3f, 1.5f, 0f) :
                new Vector3(0f, 1.5f, i == 2 ? -3f : 3f);
            var wall = Child(fixture.transform, "Validation enclosing wall", center + offset).AddComponent<BoxCollider>();
            wall.size = xWall ? new Vector3(0.3f, 3f, 6f) : new Vector3(6f, 3f, 0.3f);
            geometry[i + 1] = wall;
        }
        var world = floor.AddComponent<RuntimeNavigationWorld>();
        var data = new SerializedObject(world);
        var sources = data.FindProperty("geometry"); sources.arraySize = geometry.Length;
        for (int i = 0; i < geometry.Length; i++) sources.GetArrayElementAtIndex(i).objectReferenceValue = geometry[i];
        data.FindProperty("movingObstacles").arraySize = 0; data.ApplyModifiedPropertiesWithoutUndo();
        var ghostObj = Child(fixture.transform, "Validation normal ghost", center);
        GhostController ghost = ghostObj.AddComponent<GhostController>(); ghost.enabled = false;
        Assign(ghost, "ghostProfile", profile);
        Assign(ghost, "activityPrefab", AssetDatabase.LoadAssetAtPath<GhostActivityEvent>("Assets/Prefabs/Evidence/GhostActivityEvent.prefab"));
        Assign(ghost, "ultravioletTracePrefab", AssetDatabase.LoadAssetAtPath<UltravioletTrace>("Assets/Prefabs/Evidence/UltravioletTrace.prefab"));
        var aggression = ghostObj.AddComponent<GhostAggression>(); aggression.enabled = false;
        Number(aggression, "currentAnger", 100f);
        ghostObj.AddComponent<GhostHuntController>();
        var actor = ghostObj.AddComponent<GhostActor>(); actor.enabled = false;
        // Shorten only fixture timings; actual source scene/profile settings are untouched.
        Number(actor, "moveSpeed", 4f); Number(actor, "minIdleTime", 0.1f);
        Number(actor, "maxIdleTime", 0.1f); Number(actor, "interactionDuration", 0.05f);
        var reader = Child(fixture.transform, "Validation EMF reader", switchObj.transform.position).AddComponent<EMFReaderItem>();
        var ultraviolet = Child(fixture.transform, "Validation UV flashlight", switchObj.transform.position + Vector3.back).AddComponent<UVFlashlightItem>();
        fixture.SetActive(true);
        Physics.SyncTransforms(); reader.RefreshReading();
        Check(reader.CurrentLevel == 1, sourceRoom.RoomName + ": no preloaded test activity may appear in the fixture.");
        Check(ghost.GetComponent<GhostNavigation>().Initialize(room), sourceRoom.RoomName + ": native confined navigation must initialize.");
        ghost.GetComponent<GhostActionRestrictions>().BlockAction(GhostActionType.Hunt);
        actor.enabled = true; actor.Initialize(room);
        GhostWorldInteraction interactions = ghost.GetComponent<GhostWorldInteraction>();
        bool initialLight = light.enabled;
        await WaitFor(() => interactions.CompletedInteractions >= 1, ghost, room);
        Check(!ghost.IsHunting && light.enabled != initialLight, sourceRoom.RoomName + ": actor must operate the real switch outside a hunt.");
        Physics.SyncTransforms(); reader.RefreshReading();
        Check(reader.HasEmfFiveReading, sourceRoom.RoomName + ": normal successful interaction must physically read EMF 5 at high anger.");
        UltravioletTrace trace = null;
        foreach (var candidate in UnityEngine.Object.FindObjectsByType<UltravioletTrace>())
            if (Vector3.Distance(candidate.transform.position, target.GhostInteractionPosition) < 0.1f) { trace = candidate; break; }
        Check(trace != null, sourceRoom.RoomName + ": normal switch interaction must create a new UV trace.");
        var traceRenderer = trace.GetComponentInChildren<Renderer>();
        Check(traceRenderer != null, "Evidence prefab needs a visible renderer.");
        ultraviolet.Use(ultraviolet.gameObject);
        await WaitFor(() => traceRenderer.enabled, ghost, room);
        Check(traceRenderer.enabled, sourceRoom.RoomName + ": actual UV flashlight must reveal the fresh trace.");
        await WaitFor(() => interactions.CompletedInteractions >= 2, ghost, room);
        Check(!ghost.IsHunting && interactions.CompletedInteractions >= 2 && light.enabled == initialLight,
            sourceRoom.RoomName + ": normal evidence must recur, not exist only at startup.");
        ghost.GetComponent<GhostActionRestrictions>().BlockAction(GhostActionType.Interaction);
        Check(!interactions.TryFindInteractionPoint(room, 2.5f, out _), "Approach planning must respect blocked interaction actions.");
        Check(room.ContainsPoint(ghost.transform.position), "Normal interaction approach must remain inside the room.");
        fixture.SetActive(false);
    }
    [MenuItem("Phasma/Validate Normal Evidence (Play Mode)")]
    public static async void Validate()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || Time.timeScale != 1f || running ||
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "RoomSearchPrototype")
        { Debug.LogWarning("Open RoomSearchPrototype in normal unpaused Play Mode; run one validation at a time."); return; }
        running = true; checks = 0;
        var root = new GameObject("Temporary normal evidence validation");
        try
        {
            foreach (var signal in UnityEngine.Object.FindObjectsByType<GhostActivityEvent>(FindObjectsInactive.Include))
                if (signal.name == "TestGhostActivity") Check(!signal.gameObject.activeInHierarchy, "Startup test EMF must be disabled.");
            foreach (var trace in UnityEngine.Object.FindObjectsByType<UltravioletTrace>(FindObjectsInactive.Include))
                if (trace.name == "UltravioletTrace") Check(!trace.gameObject.activeInHierarchy, "Startup test UV must be disabled.");
            Room[] rooms = UnityEngine.Object.FindObjectsByType<Room>();
            LightSwitch[] switches = UnityEngine.Object.FindObjectsByType<LightSwitch>();
            GhostProfile profile = AssetDatabase.LoadAssetAtPath<GhostProfile>("Assets/GhostProfiles/FootprintGhostProfile.asset");
            Check(profile != null && profile.HasEmfEvidence && profile.HasUltravioletEvidence, "Use the actual footprint profile.");
            int tested = 0;
            foreach (Room room in rooms)
            {
                if (!room.CanBeGhostRoom) continue;
                LightSwitch target = null;
                foreach (var candidate in switches)
                    if (room.ContainsPoint(candidate.GhostInteractionPosition)) { target = candidate; break; }
                Check(target != null, room.RoomName + ": every possible home room needs a repeatable physical interaction target.");
                await CheckRoom(root.transform, room, target, profile, new Vector3(7000f + tested * 20f, 0f, 7000f));
                tested++;
            }
            Check(tested == 2, "Both Bedroom and Kitchen must be exercised.");
            Debug.Log("Normal evidence validation PASSED: " + checks + " checks across " + tested +
                " scene rooms, autonomous actor approach, confined native navigation, repeated switch activity, real EMF-5 and UV flashlight reveal outside hunts. Local simulation, not a multiplayer test.");
        }
        catch (Exception error) { Debug.LogError("Normal evidence validation FAILED: " + error); }
        finally
        {
            if (root != null) { root.SetActive(false); UnityEngine.Object.Destroy(root); }
            running = false;
        }
    }
}
#endif
