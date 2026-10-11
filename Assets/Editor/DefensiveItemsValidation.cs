#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class DefensiveItemsValidation
{
    private static bool running;
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Configure(GhostProfile profile, string field, float value)
    {
        var settings = new SerializedObject(profile);
        settings.FindProperty(field).floatValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
    private static PickupItem NewCross(Transform root)
    {
        GameObject obj = new GameObject("Validation cross");
        obj.transform.SetParent(root);
        obj.AddComponent<BoxCollider>();
        obj.AddComponent<Rigidbody>();
        GameObject detection = new GameObject("Validation child trigger");
        detection.transform.SetParent(obj.transform, false);
        detection.AddComponent<SphereCollider>().isTrigger = true;
        PickupItem item = obj.AddComponent<PickupItem>();
        obj.AddComponent<CrossItem>();
        return item;
    }

    [MenuItem("Phasma/Validate Defensive Items (Play Mode)")]
    public static async void Validate()
    {
        if (!EditorApplication.isPlaying || running)
        {
            Debug.LogWarning("Enter Play Mode first; wait if validation is already running.");
            return;
        }
        running = true;
        checks = 0;
        GameObject root = new GameObject("Temporary defensive-item validation");
        GameObject arena = null;
        PickupItem item = null;
        GhostProfile profile = ScriptableObject.CreateInstance<GhostProfile>();
        try
        {
            Configure(profile, "crossBreakChance", 0f);
            Configure(profile, "angerCrossBreakContribution", 0f);
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.transform.SetParent(root.transform);
            player.transform.position = new Vector3(1000f, 1000f, 1000f);
            PlayerLifeState life = player.AddComponent<PlayerLifeState>();
            GhostPerceptionTarget target = player.AddComponent<GhostPerceptionTarget>();
            GameObject hand = new GameObject("Validation hand");
            hand.transform.SetParent(player.transform, false);
            hand.transform.localPosition = new Vector3(0.2f, 0f, 0.2f);
            item = NewCross(root.transform);
            CrossItem cross = item.GetComponent<CrossItem>();
            Check(item.State == PickupItemState.Dropped && !cross.IsActive, "Dropped cross must be inactive.");
            item.AttachTo(hand.transform);
            Check(item.State == PickupItemState.Held && cross.IsActive && Array.TrueForAll(item.GetComponentsInChildren<Collider>(), collider => !collider.enabled),
                "Held cross must protect, with its collision disabled.");
            GameObject attacker = new GameObject("Validation attacker");
            attacker.transform.SetParent(root.transform);
            attacker.transform.position = player.transform.position + Vector3.back * 0.9f;
            GhostController controller = attacker.AddComponent<GhostController>();
            controller.enabled = false; // This isolated fixture has no real rooms/actor.
            var controllerData = new SerializedObject(controller);
            controllerData.FindProperty("ghostProfile").objectReferenceValue = profile;
            controllerData.ApplyModifiedPropertiesWithoutUndo();
            GhostAttack attack = attacker.AddComponent<GhostAttack>();
            Physics.SyncTransforms();
            Check(!attack.TryAttack(target) && attack.WasRepelled && life.IsAlive && !cross.IsBroken,
                "Intact held cross must repel an actual attack without killing the player.");
            item.Drop(player.transform.position, Vector3.forward, 0f);
            Check(item.State == PickupItemState.Dropped && !cross.IsActive && item.GetComponent<Collider>().enabled && item.GetComponentInChildren<SphereCollider>().enabled,
                "Dropping must restore collision and remove cross protection.");
            item.Place(player.transform.position + Vector3.right, Quaternion.identity);
            Check(item.State == PickupItemState.Placed && cross.IsActive && item.GetComponent<Rigidbody>().isKinematic && item.GetComponentInChildren<SphereCollider>().enabled,
                "Placed cross must protect and remain stationary.");
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(root.transform);
            wall.transform.position = player.transform.position + Vector3.right * 0.5f;
            wall.transform.localScale = new Vector3(0.1f, 3f, 2f);
            Physics.SyncTransforms();
            Check(!CrossItem.ProtectPlayer(target, profile, 0f), "Placed cross must not protect through a wall.");
            wall.SetActive(false);
            Configure(profile, "crossBreakChance", 1f);
            Physics.SyncTransforms();
            Check(!attack.TryAttack(target) && attack.WasRepelled && life.IsAlive && cross.IsBroken,
                "Breaking cross must still save the player from that attack.");
            Check(!cross.IsActive && attack.TryAttack(target) && !life.IsAlive,
                "Broken cross must not prevent the next attack.");
            var immune = new SerializedObject(profile);
            immune.FindProperty("ignoresCross").boolValue = true;
            immune.ApplyModifiedPropertiesWithoutUndo();
            item = ReplaceCross(item, root.transform, hand.transform);
            cross = item.GetComponent<CrossItem>();
            Check(!cross.TryProtect(profile, 0f) && !cross.IsBroken, "Immune type must bypass even an intact held cross.");
            immune.Update();
            immune.FindProperty("ignoresCross").boolValue = false;
            immune.ApplyModifiedPropertiesWithoutUndo();
            Configure(profile, "crossBreakChance", 0.15f);
            Configure(profile, "angerCrossBreakContribution", 0.5f);
            Check(profile.GetCrossBreakChance(100f) > profile.GetCrossBreakChance(0f),
                "Aggression must increase the configured break probability.");
            Check(AssetDatabase.LoadAssetAtPath<GhostProfile>("Assets/GhostProfiles/RoomSearchTestProfile.asset").SaltResponse == GhostSaltResponse.AvoidUntilHunt &&
                AssetDatabase.LoadAssetAtPath<GhostProfile>("Assets/GhostProfiles/FootprintGhostProfile.asset").SaltResponse == GhostSaltResponse.LeaveFootprints &&
                AssetDatabase.LoadAssetAtPath<GhostProfile>("Assets/GhostProfiles/CrossImmuneGhostProfile.asset").SaltResponse == GhostSaltResponse.Ignore,
                "The three example profiles must expose distinct salt rules.");

            // Actual RuntimeNavigationWorld update, isolated from the gameplay NavMesh.
            arena = new GameObject("Temporary salt navigation arena");
            arena.SetActive(false);
            arena.transform.position = new Vector3(2000f, 0f, 2000f);
            GameObject floor = new GameObject("Validation floor");
            floor.transform.SetParent(root.transform);
            floor.transform.position = arena.transform.position;
            Physics.SyncTransforms();
            BoxCollider floorBox = floor.AddComponent<BoxCollider>();
            floorBox.size = new Vector3(10f, 0.2f, 4f);
            RuntimeNavigationWorld world = arena.AddComponent<RuntimeNavigationWorld>();
            var worldData = new SerializedObject(world);
            var geometry = worldData.FindProperty("geometry");
            geometry.arraySize = 1;
            geometry.GetArrayElementAtIndex(0).objectReferenceValue = floorBox;
            worldData.FindProperty("movingObstacles").arraySize = 0;
            worldData.ApplyModifiedPropertiesWithoutUndo();
            arena.SetActive(true);
            floor.transform.SetParent(arena.transform, true);
            Vector3 center = arena.transform.position + Vector3.up * 0.1f;
            int saltArea = NavMesh.GetAreaFromName("Salt");
            Check(saltArea >= 0, "Project must define the Salt navigation area.");
            int avoidsSalt = NavMesh.AllAreas & ~(1 << saltArea);
            var path = new NavMeshPath();
            Vector3 start = center + Vector3.left * 3f, end = center + Vector3.right * 3f;
            Check(NavMesh.CalculatePath(start, end, avoidsSalt, path) && path.status == NavMeshPathStatus.PathComplete,
                "Empty arena must allow a complete path.");
            GameObject pileObject = new GameObject("Validation salt barrier");
            pileObject.SetActive(false);
            pileObject.transform.SetParent(arena.transform, false);
            pileObject.transform.position = center + Vector3.up * 0.02f;
            SaltPile pile = pileObject.AddComponent<SaltPile>();
            var pileData = new SerializedObject(pile);
            pileData.FindProperty("radius").floatValue = 2f;
            pileData.ApplyModifiedPropertiesWithoutUndo();
            pileObject.SetActive(true);
            await WaitForArea(center, 1 << saltArea);
            Check(!NavMesh.CalculatePath(start, end, avoidsSalt, path) || path.status != NavMeshPathStatus.PathComplete,
                "Salt must block the path of an avoiding type.");
            Check(NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete,
                "Salt must remain passable for hunting/footprint/ignoring types.");
            pile.Scatter();
            pile.Scatter();
            Check(!pile.IsIntact, "Scattering must be idempotent.");
            await WaitForArea(center, 1 << NavMesh.GetAreaFromName("Walkable"));
            Check(NavMesh.CalculatePath(start, end, avoidsSalt, path) && path.status == NavMeshPathStatus.PathComplete,
                "Scattered salt must restore the avoiding type's path.");
            Debug.Log("Defensive-item validation PASSED: " + checks + " checks, item states, attack/cross protection and breaking, profile rules and real dynamic salt NavMesh updates. This is a local test, not a multiplayer test.");
        }
        catch (Exception error) { Debug.LogError("Defensive-item validation FAILED: " + error.Message); }
        finally
        {
            if (item != null) UnityEngine.Object.Destroy(item.gameObject); // Items detach when placed/dropped.
            if (arena != null) { arena.SetActive(false); UnityEngine.Object.Destroy(arena); }
            root.SetActive(false);
            UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(profile);
            running = false;
        }
    }
    private static PickupItem ReplaceCross(PickupItem old, Transform parent, Transform hand)
    {
        old.gameObject.SetActive(false);
        UnityEngine.Object.Destroy(old.gameObject);
        PickupItem item = NewCross(parent);
        item.AttachTo(hand);
        return item;
    }
    private static async Task WaitForArea(Vector3 center, int mask)
    {
        float deadline = Time.realtimeSinceStartup + 8f;
        while (EditorApplication.isPlaying && Time.realtimeSinceStartup < deadline)
        {
            if (NavMesh.SamplePosition(center, out _, 0.15f, mask)) return;
            await Task.Delay(50);
        }
        throw new InvalidOperationException("Salt NavMesh update timed out or Play Mode stopped.");
    }
}
#endif
