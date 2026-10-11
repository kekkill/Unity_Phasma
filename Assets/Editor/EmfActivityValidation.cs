#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class EmfActivityValidation
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
    private static void Hunting(GhostController controller, bool value)
    {
        var data = new SerializedObject(controller.GetComponent<GhostHuntController>());
        data.FindProperty("isHunting").boolValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
        controller.GetComponent<GhostActivityEmitter>().RefreshHuntSignal();
    }
    private static GameObject ObjectAt(Transform root, string name, Vector3 position)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(root); obj.transform.position = position;
        return obj;
    }
    private static GhostController Ghost(Transform root, GhostProfile profile, Vector3 position)
    {
        var obj = ObjectAt(root, "Validation activity ghost", position); obj.SetActive(false);
        var controller = obj.AddComponent<GhostController>(); controller.enabled = false;
        Assign(controller, "ghostProfile", profile);
        Assign(controller, "activityPrefab", AssetDatabase.LoadAssetAtPath<GhostActivityEvent>("Assets/Prefabs/Evidence/GhostActivityEvent.prefab"));
        Assign(controller, "ultravioletTracePrefab", AssetDatabase.LoadAssetAtPath<UltravioletTrace>("Assets/Prefabs/Evidence/UltravioletTrace.prefab"));
        var anger = obj.AddComponent<GhostAggression>(); anger.enabled = false;
        obj.AddComponent<GhostHuntController>();
        obj.SetActive(true);
        return controller;
    }
    private static PickupItem Item(Transform root, Vector3 position)
    {
        var obj = ObjectAt(root, "Validation dropped equipment", position);
        obj.AddComponent<BoxCollider>().size = Vector3.one * 0.2f;
        var body = obj.AddComponent<Rigidbody>(); body.useGravity = false;
        return obj.AddComponent<PickupItem>();
    }
    private static async Task WaitFor(Func<bool> condition)
    {
        double end = EditorApplication.timeSinceStartup + 8.0;
        while (!condition())
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || Time.timeScale != 1f)
                throw new InvalidOperationException("Validation interrupted; use normal unpaused Play Mode.");
            if (EditorApplication.timeSinceStartup >= end) throw new InvalidOperationException("Physics/navigation check timed out.");
            await Task.Yield();
        }
    }
    private static bool CompletePath(Vector3 from, Vector3 to)
    {
        if (!NavMesh.SamplePosition(from, out var start, 1f, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(to, out var finish, 1f, NavMesh.AllAreas)) return false;
        var path = new NavMeshPath();
        return NavMesh.CalculatePath(start.position, finish.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
    }
    [MenuItem("Phasma/Validate EMF and Hunt Activity (Play Mode)")]
    public static async void Validate()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || Time.timeScale != 1f || running)
        { Debug.LogWarning("Use normal unpaused Play Mode and wait for any running activity validation."); return; }
        running = true; checks = 0;
        var root = new GameObject("Temporary EMF activity validation");
        try
        {
            var ordinary = AssetDatabase.LoadAssetAtPath<GhostProfile>("Assets/GhostProfiles/RoomSearchTestProfile.asset");
            var signature = AssetDatabase.LoadAssetAtPath<GhostProfile>("Assets/GhostProfiles/FootprintGhostProfile.asset");
            var immune = AssetDatabase.LoadAssetAtPath<GhostProfile>("Assets/GhostProfiles/CrossImmuneGhostProfile.asset");
            Check(ordinary != null && signature != null && immune != null, "Real catalog profiles must exist.");
            Check(!ordinary.HasEmfEvidence && signature.HasEmfEvidence && immune.HasEmfEvidence,
                "Only configured EMF-5 types may produce the signature.");
            Check(ordinary.HasFreezingEvidence && !signature.HasFreezingEvidence && !immune.HasWritingEvidence,
                "Catalog types need distinct evidence combinations.");
            float[] angerValues = { 0f, 24.99f, 25f, 49.99f, 50f, 74.99f, 75f, 100f };
            int[] levels = { 2, 2, 3, 3, 4, 4, 5, 5 };
            UnityEngine.Random.State randomBefore = UnityEngine.Random.state;
            for (int i = 0; i < angerValues.Length; i++)
            {
                Check(GhostActivityRules.EmfLevel(signature, angerValues[i], false) == levels[i], "Incorrect signature anger threshold.");
                Check(GhostActivityRules.EmfLevel(ordinary, angerValues[i], false) == Mathf.Min(4, levels[i]), "Ordinary ghost must stop at EMF 4.");
            }
            Check(randomBefore.Equals(UnityEngine.Random.state), "EMF computation must not consume random state.");
            Check(GhostActivityRules.EmfLevel(ordinary, 0f, true) == 4 && GhostActivityRules.EmfLevel(signature, 0f, true) == 5,
                "Hunts must use the profile maximum even at low anger.");
            Check(GhostActivityRules.InteractionChance(0.3f, 100f) == 1f && GhostActivityRules.IdleMultiplier(signature, 100f) < 1f,
                "Anger must increase normal interaction frequency.");

            Vector3 point = new Vector3(5000f, 10f, 5000f);
            GhostController ghost = Ghost(root.transform, ordinary, point);
            var emitter = ghost.GetComponent<GhostActivityEmitter>();
            Check(ghost.ActivityPrefab != null && ghost.UltravioletTracePrefab != null, "Real evidence prefabs must be assigned.");
            var reader = ObjectAt(root.transform, "Validation EMF reader", point).AddComponent<EMFReaderItem>();
            var notes = reader.gameObject.AddComponent<JournalState>();
            notes.BindCatalog(AssetDatabase.LoadAssetAtPath<JournalCatalog>("Assets/GhostProfiles/PrototypeJournalCatalog.asset"));
            GhostActivityEvent signal = emitter.EmitEmfAt(point);
            Physics.SyncTransforms(); reader.RefreshReading();
            Check(signal != null && reader.CurrentLevel == 2 && !reader.HasEmfFiveReading, "Ordinary types must still emit measurable low EMF.");
            ghost.GetComponent<GhostAggression>().AddAnger(100f);
            signal = emitter.EmitEmfAt(point);
            Physics.SyncTransforms(); reader.RefreshReading();
            Check(signal.EmfLevel == 4 && reader.CurrentLevel == 4, "High anger without the signature must physically read 4.");
            var trace = emitter.EmitUltravioletAt(point);
            Check(trace != null, "Ordinary pre-hunt interactions may leave configured UV traces.");
            Hunting(ghost, true);
            Physics.SyncTransforms(); reader.RefreshReading();
            Check(emitter.HuntSignal != null && reader.CurrentLevel == 4, "Hunt activity must create a measurable moving signal.");
            var huntSignal = emitter.HuntSignal;
            ghost.transform.position += Vector3.right;
            emitter.RefreshHuntSignal();
            Check(emitter.HuntSignal == huntSignal && huntSignal.transform.position == ghost.transform.position,
                "Hunt must move one reused signal, not spawn a signal every frame.");
            Check(emitter.EmitUltravioletAt(point) == null && trace != null, "Hunt creates no new UV and retains existing traces.");
            Hunting(ghost, false);
            Check(emitter.HuntSignal == null && !huntSignal.IsActive, "Finishing a hunt must immediately remove its moving signal.");
            for (int i = 0; i < GhostActivityEmitter.MaxInteractionSignals + 3; i++) emitter.EmitEmfAt(point);
            Check(emitter.ActiveInteractionSignals == GhostActivityEmitter.MaxInteractionSignals, "Interaction EMF events must be bounded.");
            GhostController specialGhost = Ghost(root.transform, signature, point + Vector3.right * 20f);
            reader.transform.position = specialGhost.transform.position;
            var specialEmitter = specialGhost.GetComponent<GhostActivityEmitter>();
            specialGhost.GetComponent<GhostAggression>().AddAnger(75f);
            var signatureEvent = specialEmitter.EmitEmfAt(reader.transform.position);
            Physics.SyncTransforms(); reader.RefreshReading();
            Check(reader.CurrentLevel == 5 && reader.HasEmfFiveReading, "Configured signature must physically read EMF 5.");
            Check(notes.GetMark(EvidenceType.Emf) == EvidenceMark.Unknown,
                "Reading EMF 5 must not automatically mark the personal journal.");
            signatureEvent.Initialize(5, 0.05f);
            float expiry = Time.time + 0.08f;
            await WaitFor(() => Time.time >= expiry);
            reader.RefreshReading();
            Check(reader.CurrentLevel == 1, "Expired signals must stop being measured.");
            specialGhost.GetComponent<GhostAggression>().ReduceAnger(100f);
            Hunting(specialGhost, true);
            Physics.SyncTransforms(); reader.RefreshReading();
            Check(reader.HasEmfFiveReading, "EMF-5 profile must show its maximum throughout the hunt.");
            Hunting(specialGhost, false);

            var normalContext = new GhostInteractionContext(null, ghost, ordinary);
            var huntContext = new GhostInteractionContext(null, ghost, ordinary, true);
            var item = Item(root.transform, point + Vector3.forward);
            var throwable = item.GetComponent<GhostThrowableItem>();
            Check(throwable != null && throwable.CanGhostInteract(normalContext), "Dropped equipment must have an eligible throw adapter.");
            Check(throwable.TryGhostInteract(normalContext), "Normal physical throw must succeed.");
            Rigidbody body = item.GetComponent<Rigidbody>();
            await WaitFor(() => body.linearVelocity.sqrMagnitude > 0.1f);
            float ordinarySpeed = body.linearVelocity.magnitude;
            body.linearVelocity = Vector3.zero;
            Check(throwable.TryGhostInteract(huntContext), "Hunt physical throw must succeed.");
            await WaitFor(() => body.linearVelocity.magnitude > ordinarySpeed);
            Check(body.linearVelocity.magnitude > ordinarySpeed, "Hunt throws must be stronger than ordinary throws.");
            body.linearVelocity = Vector3.zero;
            item.transform.position = point + Vector3.forward;
            var collisionWall = ObjectAt(root.transform, "Validation throw collision wall", point + Vector3.forward * 2f);
            collisionWall.AddComponent<BoxCollider>().size = new Vector3(4f, 4f, 0.2f);
            Physics.SyncTransforms();
            Check(throwable.TryGhostInteract(huntContext), "Throw towards a wall must apply a physical impulse.");
            float collisionEnd = Time.time + 0.8f;
            await WaitFor(() => Time.time >= collisionEnd);
            Check(item.transform.position.z < collisionWall.transform.position.z + 0.1f,
                "Thrown equipment must collide with a solid wall instead of teleporting through it.");
            collisionWall.SetActive(false);
            var hand = ObjectAt(root.transform, "Validation hand", point).transform;
            item.AttachTo(hand);
            Check(!throwable.TryGhostInteract(huntContext), "Held equipment must never be thrown by the ghost.");
            item.Store(hand);
            Check(!throwable.TryGhostInteract(huntContext), "Stored equipment must never be thrown.");
            item.Place(point + Vector3.forward, Quaternion.identity);
            Check(!throwable.TryGhostInteract(huntContext), "Installed equipment must remain installed.");
            var book = item.gameObject.AddComponent<GhostWritingBook>();
            Check(book.CanGhostInteract(normalContext) && !book.TryGhostInteract(huntContext) && !book.HasWriting,
                "Installed book is usable normally, but hunt must not create writing evidence.");
            Check(book.TryGhostInteract(normalContext) && book.HasWriting && !book.TryGhostInteract(normalContext),
                "Normal writing must occur once and report an actual state change.");
            var inventory = ObjectAt(root.transform, "Validation owner", point).AddComponent<PlayerInventory>();
            Check(inventory.TryAdd(item) && !throwable.TryGhostInteract(huntContext), "Owned inventory equipment must be protected from ghost throws.");
            item.gameObject.SetActive(false);

            var lightObj = ObjectAt(root.transform, "Validation light", point + Vector3.forward);
            var light = lightObj.AddComponent<Light>();
            var lightSwitch = lightObj.AddComponent<LightSwitch>(); Assign(lightSwitch, "targetLight", light);
            Check(!lightSwitch.TryGhostInteract(huntContext) && light.enabled, "Hunt policy must reject light switching.");
            lightObj.SetActive(false);
            Hunting(ghost, true); ghost.transform.position = point;
            var targetItem = Item(root.transform, point + Vector3.forward);
            var interactions = ghost.GetComponent<GhostWorldInteraction>();
            var wall = ObjectAt(root.transform, "Validation intervening wall", point + Vector3.forward * 0.5f);
            wall.AddComponent<BoxCollider>().size = new Vector3(4f, 4f, 0.15f);
            Physics.SyncTransforms();
            Check(!interactions.TryInteractNearby(2.5f), "A solid wall must block nearby item interactions.");
            wall.SetActive(false); Physics.SyncTransforms();
            var safe = ObjectAt(root.transform, "Validation safe area", targetItem.transform.position).AddComponent<SafeZone>();
            safe.GetComponent<BoxCollider>().isTrigger = true;
            Physics.SyncTransforms();
            Check(!interactions.TryInteractNearby(2.5f), "Safe-zone equipment must be protected.");
            safe.gameObject.SetActive(false); Physics.SyncTransforms();
            var restrictions = ghost.GetComponent<GhostActionRestrictions>();
            restrictions.BlockAction(GhostActionType.Interaction);
            Check(!interactions.TryInteractNearby(2.5f), "Blocked Interaction action must prevent hunt object changes.");
            restrictions.UnblockAction(GhostActionType.Interaction);
            Check(interactions.TryInteractNearby(2.5f), "Clear line of sight must permit a real hunt throw.");
            Check(!interactions.TryInteractNearby(2.5f), "Same target must obey its interaction cooldown.");
            targetItem.gameObject.SetActive(false);
            var distantItem = Item(root.transform, point + Vector3.forward * 5f);
            Physics.SyncTransforms();
            Check(!interactions.TryInteractNearby(2.5f), "Hunt interactions must stay within the configured radius.");
            distantItem.gameObject.SetActive(false);
            var nearbyBook = Item(root.transform, point + Vector3.forward);
            nearbyBook.Place(nearbyBook.transform.position, Quaternion.identity);
            nearbyBook.gameObject.AddComponent<GhostWritingBook>();
            Physics.SyncTransforms();
            int signalsBefore = emitter.ActiveInteractionSignals;
            Check(!interactions.TryInteractNearby(2.5f) && emitter.ActiveInteractionSignals == signalsBefore,
                "Rejected hunt targets must not generate phantom interaction EMF.");
            nearbyBook.gameObject.SetActive(false);
            Hunting(ghost, false);
            var room = ObjectAt(root.transform, "Validation room", point + Vector3.left * 10f).AddComponent<Room>();
            var thermometer = ObjectAt(root.transform, "Validation thermometer", point).AddComponent<ThermometerItem>();
            Check(thermometer.MeasureRoom(room) && thermometer.HasReading && Mathf.Approximately(thermometer.LastTemperature, room.Temperature),
                "Thermometer must expose the actual measured room temperature.");
            Check(!thermometer.MeasureRoom(null) && !thermometer.HasReading, "Missing room must clear thermometer reading validity.");

            // Isolated native NavMesh test: opening a real rotating/carving door restores the route.
            Vector3 arena = new Vector3(4000f, 0f, 4000f);
            var floor = ObjectAt(root.transform, "Validation navigation floor", arena); floor.SetActive(false);
            var floorBox = floor.AddComponent<BoxCollider>(); floorBox.size = new Vector3(8f, 0.2f, 4f);
            floorBox.center = Vector3.down * 0.1f;
            var doorObj = ObjectAt(root.transform, "Validation carving door", arena + Vector3.up * 1.5f);
            var doorBox = doorObj.AddComponent<BoxCollider>(); doorBox.size = new Vector3(0.2f, 3f, 4f);
            var door = doorObj.AddComponent<Door>();
            var world = floor.AddComponent<RuntimeNavigationWorld>();
            var data = new SerializedObject(world);
            var geometry = data.FindProperty("geometry"); geometry.arraySize = 1;
            geometry.GetArrayElementAtIndex(0).objectReferenceValue = floorBox;
            var obstacles = data.FindProperty("movingObstacles"); obstacles.arraySize = 1;
            obstacles.GetArrayElementAtIndex(0).objectReferenceValue = doorBox;
            data.ApplyModifiedPropertiesWithoutUndo(); floor.SetActive(true);
            Vector3 from = arena + new Vector3(-3f, 0.1f, 0f), to = arena + new Vector3(3f, 0.1f, 0f);
            await WaitFor(() => NavMesh.SamplePosition(from, out _, 1f, NavMesh.AllAreas) && !CompletePath(from, to));
            Check(!CompletePath(from, to), "Closed carving door must block the isolated route.");
            ghost.transform.position = arena + new Vector3(-0.9f, 0.61f, 0f);
            Hunting(ghost, true); interactions.ResetHuntTimer(); Physics.SyncTransforms();
            Check(interactions.TryInteractNearby(2.5f) && door.IsOpen, "Hunt must open the physically reachable closed door.");
            Check(!door.TryGhostInteract(huntContext) && door.IsOpen, "Hunt must not close an already-open door.");
            await WaitFor(() => CompletePath(from, to));
            Check(CompletePath(from, to), "Door rotation and carving update must restore the real route.");
            var stoppedActor = ghost.gameObject.AddComponent<GhostActor>(); stoppedActor.enabled = false;
            emitter.RefreshHuntSignal();
            Check(emitter.HuntSignal == null && !interactions.TryInteractNearby(2.5f), "Stopping the actor must stop hunt activity.");
            Debug.Log("EMF/hunt activity validation PASSED: " + checks + " checks, anger thresholds, real readings/expiry, bounded hunt signals, UV policy, inventory safety, physical throws, walls/safety and native door carving. Local simulation, not a multiplayer test.");
        }
        catch (Exception error) { Debug.LogError("EMF/hunt activity validation FAILED: " + error); }
        finally
        {
            if (root != null) { root.SetActive(false); UnityEngine.Object.Destroy(root); }
            running = false;
        }
    }
}
#endif
