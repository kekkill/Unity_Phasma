#if UNITY_EDITOR
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class SoundHidingValidation
{
    private static bool running;
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new InvalidOperationException(message); }
    private static void Assign(UnityEngine.Object obj, string name, UnityEngine.Object value)
    {
        var data = new SerializedObject(obj); data.FindProperty(name).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Number(UnityEngine.Object obj, string name, float value)
    {
        var data = new SerializedObject(obj); data.FindProperty(name).floatValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static GameObject Child(Transform root, string name, Vector3 position)
    {
        var obj = new GameObject(name); obj.transform.SetParent(root); obj.transform.position = position; return obj;
    }
    private static GhostPerceptionTarget Player(Transform root, Vector3 position, GameSession session = null)
    {
        var obj = Child(root, "Sound validation participant", position);
        var body = obj.AddComponent<CharacterController>(); body.height = 2f; body.radius = 0.3f;
        var life = obj.AddComponent<PlayerLifeState>(); if (session != null) life.BindSession(session);
        obj.AddComponent<PlayerNoiseEmitter>().enabled = false;
        var target = obj.AddComponent<GhostPerceptionTarget>();
        var eyes = Child(obj.transform, "Validation eyes", position + Vector3.up * 0.6f);
        Assign(target, "visionPoint", eyes.transform);
        return target;
    }
    private static async Task WaitFor(Func<bool> condition, Action tick = null)
    {
        double end = EditorApplication.timeSinceStartup + 12.0;
        while (!condition())
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || Time.timeScale != 1f)
                throw new InvalidOperationException("Validation interrupted: use unpaused normal Play Mode.");
            if (EditorApplication.timeSinceStartup >= end) throw new InvalidOperationException("Sound/hiding physics or navigation timed out.");
            tick?.Invoke(); await Task.Yield();
        }
    }
    private static Task Delay(float seconds)
    { float until = Time.time + seconds; return WaitFor(() => Time.time >= until); }
    private static void Noise(GhostPerceptionTarget target, float level) => Number(target.GetComponent<PlayerNoiseEmitter>(), "currentNoiseLevel", level);
    private static float FlatDistance(Vector3 a, Vector3 b) => Vector3.ProjectOnPlane(a - b, Vector3.up).magnitude;
    [MenuItem("Phasma/Validate Sound and Hiding (Play Mode)")]
    public static async void Validate()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || Time.timeScale != 1f || running ||
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "RoomSearchPrototype")
        { Debug.LogWarning("Open RoomSearchPrototype in normal unpaused Play Mode; run one validation at a time."); return; }
        running = true; checks = 0;
        var root = new GameObject("Temporary sound and hiding validation"); root.SetActive(false);
        GhostProfile profile = null;
        try
        {
            var originalZone = HidingZone.Active.Single();
            Check(originalZone.Door != null && originalZone.Volume.isTrigger && !SafeZone.ContainsAny(originalZone.InvestigationPosition),
                "Scene needs one real wardrobe, independent from the safe annex.");
            Check(NavMesh.GetAreaFromName("Salt") == 3 && NavMesh.GetAreaFromName("Hiding") == 4,
                "Hiding area must be configured without changing the existing Salt ID.");
            profile = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GhostProfile>("Assets/GhostProfiles/FootprintGhostProfile.asset"));
            Number(profile, "hearingRange", 10f); Number(profile, "targetMemoryDuration", 15f);
            Vector3 point = new Vector3(9000f, 0f, 9000f);
            var session = Child(root.transform, "Validation local session", point).AddComponent<GameSession>();
            var player = Player(root.transform, point + new Vector3(0f, 1f, 4f), session);
            var peer = Player(root.transform, point + new Vector3(20f, 1f, 0f), session);
            var ghostObj = Child(root.transform, "Validation hearing ghost", point + Vector3.up * 0.59f);
            var controller = ghostObj.AddComponent<GhostController>(); controller.enabled = false;
            Assign(controller, "ghostProfile", profile);
            Assign(controller, "activityPrefab", AssetDatabase.LoadAssetAtPath<GhostActivityEvent>("Assets/Prefabs/Evidence/GhostActivityEvent.prefab"));
            Assign(controller, "ultravioletTracePrefab", AssetDatabase.LoadAssetAtPath<UltravioletTrace>("Assets/Prefabs/Evidence/UltravioletTrace.prefab"));
            var aggression = ghostObj.AddComponent<GhostAggression>(); aggression.enabled = false;
            var perception = ghostObj.AddComponent<GhostPerception>(); perception.enabled = false;
            Assign(perception, "target", player); Assign(perception, "session", session);
            var hunt = ghostObj.AddComponent<GhostHuntController>();
            // This phase has no NavMesh. Enable the agent through Initialize only after the arena is built.
            var agent = ghostObj.GetComponent<NavMeshAgent>(); agent.enabled = false;
            var hearing = ghostObj.GetComponent<GhostHearing>();
            var knowledge = ghostObj.GetComponent<GhostHidingKnowledge>();
            var wallObj = Child(root.transform, "Validation acoustic wall", point + new Vector3(0f, 1.3f, 2f));
            wallObj.AddComponent<BoxCollider>().size = new Vector3(4f, 3f, 0.2f);
            var surface = wallObj.AddComponent<AcousticSurface>();
            var secondCollider = Child(wallObj.transform, "Same surface collider", wallObj.transform.position).AddComponent<BoxCollider>();
            secondCollider.size = new Vector3(4f, 3f, 0.1f);
            var doorObj = Child(root.transform, "Validation noise door", point + new Vector3(-1f, 0f, 2f));
            var panel = Child(doorObj.transform, "Door panel", point + new Vector3(0f, 1.3f, 2f)).AddComponent<BoxCollider>();
            panel.size = new Vector3(2f, 2.6f, 0.12f);
            var door = doorObj.AddComponent<Door>();
            var floor = Child(root.transform, "Validation impact floor", point + Vector3.down * 0.1f).AddComponent<BoxCollider>();
            floor.size = new Vector3(30f, 0.2f, 30f);
            root.SetActive(true); ghostObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            wallObj.SetActive(false); doorObj.SetActive(false); Noise(player, 1f); Physics.SyncTransforms();
            perception.RefreshTarget();
            Check(perception.CanHearTarget && !perception.CanSeeTarget, "Clear-path movement must be heard independently of sight.");
            Check(perception.LastKnownPosition == player.transform.position, "Movement hearing must store the observed movement position.");
            wallObj.SetActive(true); Physics.SyncTransforms();
            float attenuation = hearing.Transmission(ghostObj.transform.position + Vector3.up * 0.4f, player.transform.position + Vector3.up * 0.4f, player);
            Check(Mathf.Approximately(attenuation, 0.25f), "Multiple colliders on one acoustic surface must attenuate exactly once.");
            perception.RefreshTarget();
            Check(!perception.CanHearTarget, "A wall must suppress movement outside the attenuated range.");
            Number(surface, "transmission", 0f); Physics.SyncTransforms();
            Check(!hearing.TryHear(ghostObj.transform.position, player.transform.position, player, 1f, 10f, out _), "Zero transmission must block hearing.");
            wallObj.SetActive(false); doorObj.SetActive(true); Noise(player, 0.6f); Physics.SyncTransforms();
            perception.RefreshTarget();
            Check(!perception.CanHearTarget, "Closed door must reduce the effective movement range.");
            door.SetOpen(true); await WaitFor(() => door.IsFullyOpen); Physics.SyncTransforms();
            perception.RefreshTarget();
            Check(perception.CanHearTarget, "Opening the physical door must restore hearing through its opening.");
            Noise(player, 0f); door.Interact(player.gameObject);
            var action = WorldNoise.Recent.Last(item => item.Source == player && item.Kind == WorldNoiseKind.Door);
            Check(action.Source == player && action.Loudness > 0f, "Player door action must publish attributed noise.");
            Vector3 snapshot = action.Position;
            player.transform.position += Vector3.right * 6f; Physics.SyncTransforms(); perception.RefreshTarget();
            Check(perception.CanHearTarget && perception.LastKnownPosition == snapshot && perception.LastKnownPosition != player.transform.position,
                "Door memory must stay at the sound origin after the participant moves away.");
            await Delay(0.7f); perception.RefreshTarget();
            Check(!perception.CanHearTarget && perception.HasKnownTargetPosition && perception.LastKnownPosition == snapshot,
                "Expired action must stop hearing but leave its position in finite memory.");
            Number(profile, "targetMemoryDuration", 0.05f); await Delay(0.1f); perception.RefreshTarget();
            Check(!perception.HasKnownTargetPosition, "Old sound memory must expire.");
            Number(profile, "targetMemoryDuration", 15f);
            peer.transform.position = point + new Vector3(0f, 1f, -2f); Physics.SyncTransforms(); perception.RefreshTarget();
            Check(perception.Target == peer && perception.CanSeeTarget, "Visible local participant must take priority over an unseen one.");
            Check(WorldNoise.Publish(peer.transform.position, peer, 1f), "Living participant can publish noise.");
            peer.LifeState.TryDie(); perception.RefreshTarget();
            Check(perception.Target != peer && !WorldNoise.Recent.Any(item => item.Source == peer), "Dead participants must lose targeting and buffered noise.");
            Check(!WorldNoise.Publish(peer.transform.position, peer, 1f), "Dead participant cannot publish a new event.");
            var safety = Child(root.transform, "Validation safety", player.transform.position).AddComponent<SafeZone>();
            safety.GetComponent<BoxCollider>().isTrigger = true; Noise(player, 1f); Physics.SyncTransforms(); perception.RefreshTarget();
            Check(perception.Target == null && !perception.HasKnownTargetPosition, "Safe participants must clear hearing/vision/memory.");
            Check(!WorldNoise.Publish(player.transform.position, player, 1f), "Safe-zone player actions must not leak into ghost targeting.");
            safety.gameObject.SetActive(false); Noise(player, 0f); doorObj.SetActive(false);

            // Real player holder/drop and Rigidbody collision, not direct Publish standing in for the throw.
            var hand = Child(player.transform, "Validation hand", player.transform.position + Vector3.up);
            var holder = player.gameObject.AddComponent<PlayerItemHolder>(); Assign(holder, "holdPoint", hand.transform);
            var itemObj = Child(root.transform, "Validation thrown equipment", player.transform.position + Vector3.right);
            itemObj.AddComponent<BoxCollider>().size = Vector3.one * 0.2f; itemObj.AddComponent<Rigidbody>();
            var item = itemObj.AddComponent<PickupItem>();
            Check(holder.TryPickup(item), "Actual holder must equip the throw fixture.");
            holder.DropCurrentItem();
            Check(WorldNoise.Recent.Any(sound => sound.Source == player && sound.Kind == WorldNoiseKind.Throw), "Drop input path must emit throw noise.");
            Check(item.GetComponent<ThrownItemNoise>().IsArmed, "Player throw must arm bounded collision noises.");
            await WaitFor(() => WorldNoise.Recent.Any(sound => sound.Source == player && sound.Kind == WorldNoiseKind.Impact));
            Check(WorldNoise.Recent.Any(sound => sound.Source == player && sound.Kind == WorldNoiseKind.Impact), "Real floor collision must emit impact noise.");
            Check(holder.TryPickup(item) && !item.GetComponent<ThrownItemNoise>().IsArmed, "Pickup must disarm collision noise.");
            Check(holder.PlaceCurrentItem(item.transform.position + Vector3.right, Quaternion.identity) && !item.GetComponent<ThrownItemNoise>().IsArmed,
                "Installed equipment must remain disarmed.");
            Check(holder.TryPickup(item), "Placed equipment can be picked up again."); holder.DropCurrentItem();
            Check(item.GetComponent<GhostThrowableItem>().TryGhostInteract(new GhostInteractionContext(null, controller, profile)), "Ghost physical throw fixture must work.");
            Check(!item.GetComponent<ThrownItemNoise>().IsArmed, "Ghost throw must not retain player noise attribution.");
            itemObj.SetActive(false);
            for (int i = 0; i < WorldNoise.Capacity + 5; i++) WorldNoise.Publish(player.transform.position, player, 1f);
            Check(WorldNoise.Recent.Count == WorldNoise.Capacity, "Noise event storage must remain bounded.");

            // Clone the actual wardrobe into an isolated native-navigation arena.
            Vector3 arena = new Vector3(9400f, 0f, 9400f);
            var arenaObj = Child(root.transform, "Validation wardrobe arena", arena); arenaObj.SetActive(false);
            var wardrobe = UnityEngine.Object.Instantiate(originalZone.gameObject, arenaObj.transform);
            wardrobe.name = "Validation actual wardrobe"; wardrobe.transform.position = arena;
            var zone = wardrobe.GetComponent<HidingZone>();
            zone.Door.transform.localRotation = Quaternion.identity;
            var roomObj = Child(arenaObj.transform, "Validation wardrobe room", arena);
            var room = roomObj.AddComponent<Room>(); var roomBox = room.GetComponent<BoxCollider>();
            roomBox.isTrigger = true; roomBox.size = new Vector3(8f, 3f, 6f); roomBox.center = Vector3.up * 1.5f;
            var arenaFloor = Child(arenaObj.transform, "Validation native floor", arena).AddComponent<BoxCollider>();
            arenaFloor.size = new Vector3(8f, 0.2f, 6f); arenaFloor.center = Vector3.down * 0.1f;
            var world = arenaFloor.gameObject.AddComponent<RuntimeNavigationWorld>();
            var geometry = wardrobe.GetComponentsInChildren<Collider>().Where(collider => !collider.isTrigger && collider.GetComponentInParent<Door>() == null).ToList();
            geometry.Insert(0, arenaFloor);
            var data = new SerializedObject(world); var sources = data.FindProperty("geometry"); sources.arraySize = geometry.Count;
            for (int i = 0; i < geometry.Count; i++) sources.GetArrayElementAtIndex(i).objectReferenceValue = geometry[i];
            var obstacles = data.FindProperty("movingObstacles"); obstacles.arraySize = 1;
            obstacles.GetArrayElementAtIndex(0).objectReferenceValue = zone.Door.GetComponentInChildren<BoxCollider>(); data.ApplyModifiedPropertiesWithoutUndo();
            data = new SerializedObject(controller); data.FindProperty("rooms").arraySize = 1;
            data.FindProperty("rooms").GetArrayElementAtIndex(0).objectReferenceValue = room; data.ApplyModifiedPropertiesWithoutUndo();
            arenaObj.SetActive(true); Physics.SyncTransforms();
            var navigation = ghostObj.GetComponent<GhostNavigation>();
            Check(navigation.Initialize(room), "Native wardrobe arena must provide a valid external ghost spawn.");
            Check(NavMesh.SamplePosition(arena + Vector3.forward * 2.3f, out var start, 0.5f, NavMesh.AllAreas) && agent.Warp(start.position), "Place probe outside the wardrobe.");
            ghostObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            player.transform.position = arena + Vector3.up; Noise(player, 0f); Physics.SyncTransforms(); perception.RefreshTarget();
            Check(player.CanBeDetected && !perception.CanSeeTarget && !perception.CanHearTarget, "Closed wardrobe blocks sight without making the player globally invulnerable.");
            Check(!knowledge.CanEnter(zone) && !navigation.TryReachablePoint(zone.InvestigationPosition, out _), "Unknown wardrobe must be inaccessible to native movement.");
            var context = new GhostInteractionContext(null, controller, profile, true);
            Check(!zone.Door.TryGhostInteract(context), "Random hunt interaction cannot open an unknown wardrobe.");
            Check(!ghostObj.GetComponent<GhostAttack>().TryAttack(player), "Attack must independently protect undiscovered hiding space.");
            // InvestigationPosition is a standing-body target 0.6m above the floor, not a ground probe.
            Vector3 floorProbe = zone.transform.TransformPoint(zone.Volume.center - Vector3.up * (zone.Volume.size.y * 0.5f));
            Check(NavMesh.SamplePosition(floorProbe + Vector3.up * 0.1f, out var interior, 0.35f, NavMesh.AllAreas),
                "Wardrobe floor has no nearby NavMesh. Floor position: " + floorProbe.ToString("F3"));
            Check(zone.ContainsFootprint(interior.position) &&
                (interior.mask & (1 << NavMesh.GetAreaFromName("Hiding"))) != 0,
                "Wardrobe floor must have its real Hiding NavMesh area. Sample: " + interior.position.ToString("F3") +
                "; area mask: " + interior.mask + "; expected Hiding mask: " + (1 << NavMesh.GetAreaFromName("Hiding")));
            for (int i = 0; i < 5; i++)
            {
                Check(navigation.TryRoomPoint(room, false, out var randomPoint) && !zone.ContainsFootprint(randomPoint), "Random room points must exclude unknown hiding space.");
            }
            zone.Door.SetOpen(true); await WaitFor(() => zone.Door.IsFullyOpen); Physics.SyncTransforms(); perception.RefreshTarget();
            Check(perception.CanSeeTarget && perception.LastKnownHidingZone == zone && knowledge.CanEnter(zone), "Actually seeing a player inside must authorize this ghost.");
            var otherKnowledge = Child(root.transform, "Independent validation knowledge", arena).AddComponent<GhostHidingKnowledge>();
            Check(!otherKnowledge.CanEnter(zone) && !knowledge.CanEnter(originalZone), "Discovery must belong to one ghost and one hiding place.");
            Check(!knowledge.AllowsPath(originalZone.InvestigationPosition + Vector3.forward * 2f,
                new[] { originalZone.InvestigationPosition + Vector3.back * 2f }), "An enabled Hiding area must not authorize crossing another unknown zone.");
            await WaitFor(() => navigation.TryReachablePoint(zone.InvestigationPosition, out _));
            Check(navigation.MoveTo(zone.InvestigationPosition, 3f), "Discovered open wardrobe must accept a complete native path.");
            await WaitFor(() => FlatDistance(ghostObj.transform.position, zone.InvestigationPosition) < 0.2f);
            Check(zone.Contains(ghostObj.transform.position), "Authorized ghost must physically enter the wardrobe.");
            knowledge.Observe(zone, 0.05f); await Delay(0.1f);
            zone.Door.SetOpen(false);
            Check(zone.Door.CanGhostInteract(context), "An already-inside ghost may open the door to leave after knowledge expires.");
            zone.Door.SetOpen(true);
            Check(!knowledge.CanEnter(zone) && navigation.MoveTo(zone.ApproachPosition, 3f), "Expired knowledge must still allow an already-inside ghost to leave.");
            await WaitFor(() => navigation.HasArrived);
            Check(!zone.Contains(ghostObj.transform.position) && !navigation.TryReachablePoint(zone.InvestigationPosition, out _), "Exited ghost must not re-enter after knowledge expires.");
            zone.Door.SetOpen(false); await WaitFor(() => zone.Door.OpenFraction < 0.01f); Noise(player, 0.1f); Physics.SyncTransforms(); perception.RefreshTarget();
            Check(!perception.CanSeeTarget && !perception.CanHearTarget && !knowledge.CanEnter(zone), "Quiet hidden movement outside attenuated range must not reveal the wardrobe.");
            Noise(player, 0f);
            Vector3 soundOrigin = player.transform.position;
            Check(WorldNoise.Publish(soundOrigin, player, 1f), "Loud hidden action can emit a real observation.");
            perception.RefreshTarget();
            Check(!perception.CanSeeTarget && perception.CanHearTarget && knowledge.CanEnter(zone) && perception.LastKnownHidingZone == zone,
                "Heard hidden noise must reveal only its observed hiding place.");
            player.transform.position = arena + new Vector3(0f, 1f, -2f); Physics.SyncTransforms(); perception.RefreshTarget();
            Check(perception.LastKnownPosition == soundOrigin && perception.LastKnownPosition != player.transform.position, "Hidden participant movement must not drag the noise snapshot.");
            Check(hunt.BeginHunt(), "Remembered hidden noise must support a real hunt investigation.");
            await WaitFor(() => zone.Door.IsFullyOpen, () => { perception.RefreshTarget(); hunt.UpdateHunt(); });
            Check(zone.Door.IsOpen, "Hunt must approach and physically open the discovered wardrobe.");
            await WaitFor(() => zone.Contains(ghostObj.transform.position), () => { perception.RefreshTarget(); hunt.UpdateHunt(); });
            Check(zone.Contains(ghostObj.transform.position), "Real hunt search must enter the remembered sound area after opening.");
            hunt.ForceEndHunt();
            Check(!hunt.IsHunting && ghostObj.GetComponent<GhostActivityEmitter>().HuntSignal == null, "Ending investigation must clean up hunt activity.");
            var victim = Player(root.transform, arena + new Vector3(0f, 1f, 0.7f));
            Check(NavMesh.SamplePosition(arena + new Vector3(0f, 0f, 1.5f), out var attackPoint, 0.5f, NavMesh.AllAreas) && agent.Warp(attackPoint.position), "Place attack probe outside the open wardrobe.");
            knowledge.Observe(zone, 0.05f); await Delay(0.1f); Physics.SyncTransforms();
            Check(!ghostObj.GetComponent<GhostAttack>().TryAttack(victim) && victim.IsAlive,
                "Unknown hiding-space guard must prevent an otherwise in-range, unobstructed attack.");
            knowledge.Observe(zone, 1f);
            Check(ghostObj.GetComponent<GhostAttack>().TryAttack(victim) && !victim.IsAlive, "Known open hiding place must not make an exposed player immune to attack.");
            Debug.Log("Sound/hiding validation PASSED: " + checks + " checks, attenuation, immutable attributed sound memory, living/safe filters, actual door/throw/impact events and native wardrobe discovery, entry, expiry, exit and attack. Local simulation, not networking or recorded audio playback.");
        }
        catch (Exception error) { Debug.LogError("Sound/hiding validation FAILED: " + error); }
        finally
        {
            if (root != null) { root.SetActive(false); UnityEngine.Object.Destroy(root); }
            if (profile != null) UnityEngine.Object.Destroy(profile);
            running = false;
        }
    }
}
#endif
