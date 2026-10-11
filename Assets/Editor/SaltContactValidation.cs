#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class SaltContactValidation
{
    private static bool running;
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static SaltPile Pile(Transform parent, Vector3 point)
    {
        var obj = new GameObject("Validation salt pile");
        obj.transform.SetParent(parent); obj.transform.position = point;
        return obj.AddComponent<SaltPile>();
    }
    private static GhostSaltContactSensor Ghost(Transform parent, GhostProfile profile, Vector3 point)
    {
        var obj = new GameObject("Validation salt ghost");
        obj.SetActive(false); obj.transform.SetParent(parent); obj.transform.position = point;
        GhostController controller = obj.AddComponent<GhostController>();
        controller.enabled = false; // Fixture uses no rooms or running hunts.
        var data = new SerializedObject(controller);
        data.FindProperty("ghostProfile").objectReferenceValue = profile;
        data.ApplyModifiedPropertiesWithoutUndo();
        obj.AddComponent<GhostSaltReaction>();
        obj.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
        GhostSaltContactSensor sensor = obj.GetComponent<GhostSaltContactSensor>();
        var foot = new GameObject("Explicit validation feet");
        foot.transform.SetParent(obj.transform, false);
        data = new SerializedObject(sensor);
        data.FindProperty("footPoint").objectReferenceValue = foot.transform;
        data.ApplyModifiedPropertiesWithoutUndo();
        var trailData = new SerializedObject(obj.GetComponent<SaltFootprintTrail>());
        trailData.FindProperty("lifetime").floatValue = 0.1f;
        trailData.ApplyModifiedPropertiesWithoutUndo();
        obj.SetActive(true);
        return sensor;
    }
    private static void MoveAndSample(GhostSaltContactSensor sensor, Vector3 point, bool reset = false)
    {
        if (reset) { sensor.enabled = false; sensor.enabled = true; }
        sensor.transform.position = point;
        Physics.SyncTransforms();
        sensor.SampleContacts();
    }
    [MenuItem("Phasma/Validate Salt Contact (Play Mode)")]
    public static async void Validate()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || Time.timeScale != 1f || running)
        { Debug.LogWarning("Use normal unpaused Play Mode; wait if salt validation is running."); return; }
        running = true; checks = 0;
        var root = new GameObject("Temporary salt contact validation");
        int navigationChanges = 0;
        Action onNavigationChanged = () => navigationChanges++;
        try
        {
            Vector3 center = new Vector3(3000f, 0f, 3000f);
            var floor = new GameObject("Validation salt floor");
            floor.SetActive(false); floor.transform.SetParent(root.transform); floor.transform.position = center;
            BoxCollider floorBox = floor.AddComponent<BoxCollider>();
            floorBox.size = new Vector3(14f, 0.2f, 8f); floorBox.center = new Vector3(0f, -0.1f, 0f);
            RuntimeNavigationWorld world = floor.AddComponent<RuntimeNavigationWorld>();
            var data = new SerializedObject(world);
            var geometry = data.FindProperty("geometry"); geometry.arraySize = 1;
            geometry.GetArrayElementAtIndex(0).objectReferenceValue = floorBox;
            data.FindProperty("movingObstacles").arraySize = 0;
            data.ApplyModifiedPropertiesWithoutUndo(); floor.SetActive(true);
            GhostProfile footprints = AssetDatabase.LoadAssetAtPath<GhostProfile>("Assets/GhostProfiles/FootprintGhostProfile.asset");
            GhostProfile ignore = AssetDatabase.LoadAssetAtPath<GhostProfile>("Assets/GhostProfiles/CrossImmuneGhostProfile.asset");
            GhostProfile avoid = AssetDatabase.LoadAssetAtPath<GhostProfile>("Assets/GhostProfiles/RoomSearchTestProfile.asset");
            Check(footprints != null && ignore != null && avoid != null, "Actual profiles must exist.");
            SaltPile pile = Pile(root.transform, center);
            Check(pile.IsIntact && !pile.WasSteppedOn, "New salt must be intact and unmarked.");
            Check(pile.GetComponent<BoxCollider>().isTrigger && pile.GetComponent<SaltContactArea>().Pile == pile,
                "Dynamically added salt needs a bound thin trigger without solid collision.");
            Check(pile.transform.Find("Persistent salt imprint") != null &&
                !pile.transform.Find("Persistent salt imprint").gameObject.activeSelf, "Fresh salt must not display a pre-existing mark.");
            GhostSaltContactSensor sensor = Ghost(root.transform, footprints, center + Vector3.up * 0.05f + Vector3.right);
            SaltFootprintTrail trail = sensor.GetComponent<SaltFootprintTrail>();
            int contacts = 0, entries = 0, states = 0;
            sensor.Contacted += contact => { if (contact.Pile == pile) { contacts++; if (contact.IsEntry) entries++; } };
            pile.StateChanged += _ => states++;
            SaltPile.Changed += onNavigationChanged;
            MoveAndSample(sensor, center + Vector3.up * 0.05f + Vector3.right, true);
            Check(!pile.WasSteppedOn && contacts == 0, "Nearby ghost outside the foot overlap must not mark salt.");
            MoveAndSample(sensor, center + Vector3.up, true);
            Check(!pile.WasSteppedOn && contacts == 0, "Foot above the thin salt volume must not trigger a contact.");
            MoveAndSample(sensor, center + new Vector3(0.7f, 0.05f, 0.7f), true);
            Check(!pile.WasSteppedOn && contacts == 0, "Square trigger corners must not create false circular contacts.");
            sensor.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            MoveAndSample(sensor, center + new Vector3(0.7f, 0.05f, 0f), true);
            Check(pile.WasSteppedOn && pile.IsIntact && states == 1 && contacts == 1 && entries == 1,
                "A physical foot overlap must mark intact salt exactly once.");
            Check(pile.transform.Find("Persistent salt imprint").gameObject.activeSelf,
                "Contact must reveal the persistent imprint immediately.");
            Check(Mathf.Abs(Mathf.DeltaAngle(pile.ImprintLocalYaw, 90f)) < 0.01f,
                "Imprint orientation must use the contacting ghost's heading.");
            Check(navigationChanges == 0, "Footprints must not request a salt NavMesh rebake.");
            Check(trail.RemainingSteps == 6, "First footprint contact must arm the separate six-step trail.");
            trail.Tick();
            Check(trail.EmittedCount == 1 && trail.RemainingSteps == 5 && trail.ActiveFootprints == 1,
                "Trail must create a real visual on the validated floor.");
            sensor.SampleContacts();
            Check(entries == 1 && states == 1 && trail.RemainingSteps == 5,
                "Standing on the pile must not repeatedly reset trail or emit state changes.");
            Vector3 savedPosition = pile.ImprintLocalPosition;
            Check(!pile.MarkSteppedOn(center, Vector3.back) && states == 1 && pile.ImprintLocalPosition == savedPosition,
                "Repeated marking must preserve the first footprint snapshot.");
            for (int i = 1; i <= 5; i++)
            {
                MoveAndSample(sensor, center + new Vector3(0.7f + i * 0.4f, 0.05f, 0f));
                trail.Tick();
            }
            Check(trail.EmittedCount == 6 && trail.RemainingSteps == 0, "Trail must stop after its configured six steps.");
            trail.Tick();
            Check(trail.EmittedCount == 6, "Exhausted trail must not add extra steps.");

            SaltPile swept = Pile(root.transform, center + Vector3.forward * 2f);
            int sweptEntries = 0;
            sensor.Contacted += contact => { if (contact.Pile == swept && contact.IsEntry) sweptEntries++; };
            MoveAndSample(sensor, swept.transform.position + new Vector3(-1f, 0.05f, 0f), true);
            MoveAndSample(sensor, swept.transform.position + new Vector3(1f, 0.05f, 0f));
            Check(swept.WasSteppedOn && sweptEntries == 1,
                "A swept physical step across the entire pile between updates must be detected once.");
            SaltPile skipped = Pile(root.transform, center + Vector3.back * 2f);
            MoveAndSample(sensor, skipped.transform.position + new Vector3(-2f, 0.05f, 0f), true);
            MoveAndSample(sensor, skipped.transform.position + new Vector3(2f, 0.05f, 0f));
            Check(!skipped.WasSteppedOn, "Large teleports over a pile must not count as walking across it.");
            MoveAndSample(sensor, pile.transform.position + new Vector3(0f, 0.05f, 0f), true);
            Check(pile.WasSteppedOn && states == 1 && trail.RemainingSteps == 6,
                "Re-entry can refresh the temporary trail without changing the saved pile imprint.");
            pile.gameObject.SetActive(false); pile.gameObject.SetActive(true);
            Check(pile.WasSteppedOn && pile.transform.Find("Persistent salt imprint").gameObject.activeSelf,
                "Disabling/re-enabling the object must preserve and restore its mark.");
            trail.enabled = false; // Stop the fixture trail while checking expiry of existing marks.
            float deadline = Time.realtimeSinceStartup + 5f;
            while (trail.ActiveFootprints != 0 && EditorApplication.isPlaying && Time.realtimeSinceStartup < deadline)
                await Task.Delay(50);
            Check(EditorApplication.isPlaying, "Play Mode stopped before salt validation completed.");
            Check(trail.ActiveFootprints == 0 && pile.WasSteppedOn &&
                pile.transform.Find("Persistent salt imprint").gameObject.activeSelf,
                "Temporary prints must expire while the pile mark stays visible.");
            SaltPile ignored = Pile(root.transform, center + Vector3.right * 4f);
            GhostSaltContactSensor ignoring = Ghost(root.transform, ignore, ignored.transform.position + Vector3.up * 0.05f);
            int physicalIgnoreContacts = 0;
            ignoring.Contacted += _ => physicalIgnoreContacts++;
            MoveAndSample(ignoring, ignored.transform.position + Vector3.up * 0.05f, true);
            Check(physicalIgnoreContacts == 1 && ignored.IsIntact && !ignored.WasSteppedOn &&
                ignoring.GetComponent<SaltFootprintTrail>().RemainingSteps == 0,
                "Ignoring type must physically overlap but produce no imprint, scattering or trail.");
            SaltPile barrier = Pile(root.transform, center + Vector3.left * 4f);
            GhostSaltContactSensor avoiding = Ghost(root.transform, avoid, barrier.transform.position + Vector3.up * 0.05f);
            MoveAndSample(avoiding, barrier.transform.position + Vector3.up * 0.05f, true);
            Check(barrier.IsIntact && !barrier.WasSteppedOn,
                "An avoiding type in contact outside a hunt must not falsely mark or scatter salt.");
            // Only the isolated fixture hunt flag changes; real scene hunts are untouched.
            data = new SerializedObject(avoiding.GetComponent<GhostHuntController>());
            data.FindProperty("isHunting").boolValue = true; data.ApplyModifiedPropertiesWithoutUndo();
            int beforeScatter = navigationChanges;
            avoiding.SampleContacts(); avoiding.SampleContacts();
            Check(!barrier.IsIntact && !barrier.WasSteppedOn && navigationChanges == beforeScatter + 1,
                "Hunt starting during an existing contact must scatter once and notify navigation once.");
            Check(!barrier.transform.Find("Persistent salt imprint").gameObject.activeSelf,
                "Scattered salt must remain visually distinct from marked intact salt.");
            Check(!barrier.MarkSteppedOn(barrier.transform.position, Vector3.forward),
                "Scattered salt must not accept a new footprint state.");
            beforeScatter = navigationChanges;
            pile.Scatter(); pile.Scatter();
            Check(pile.WasSteppedOn && !pile.IsIntact && navigationChanges == beforeScatter + 1,
                "Scattering preserves historical stepped state and stays idempotent.");
            Check(!pile.transform.Find("Persistent salt imprint").gameObject.activeSelf,
                "A scattered pile no longer renders an intact footprint shape.");
            Debug.Log("Salt contact validation PASSED: " + checks + " checks, real foot overlap/sweep, profile rules, persistent imprint, temporary trail expiry, state idempotence and navigation notifications. Local simulation, not a multiplayer test.");
        }
        catch (Exception error) { Debug.LogError("Salt contact validation FAILED: " + error); }
        finally
        {
            SaltPile.Changed -= onNavigationChanged;
            if (root != null) { root.SetActive(false); UnityEngine.Object.Destroy(root); }
            running = false;
        }
    }
}
#endif
