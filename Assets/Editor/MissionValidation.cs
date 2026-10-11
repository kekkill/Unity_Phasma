#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public static class MissionValidation
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Assign(UnityEngine.Object component, string name, UnityEngine.Object value)
    {
        var data = new SerializedObject(component);
        data.FindProperty(name).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static GameObject Child(Transform parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent);
        return child;
    }
    private static PlayerLifeState Player(Transform root, GameSession session, JournalCatalog catalog, Vector3 position)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Mission validation player";
        player.SetActive(false);
        player.transform.SetParent(root);
        player.transform.position = position;
        player.AddComponent<PlayerJournalController>();
        Assign(player.GetComponent<PlayerInputReader>(), "inputActions",
            AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Input/PlayerControls.inputactions"));
        player.GetComponent<JournalState>().BindCatalog(catalog);
        PlayerItemHolder holder = player.AddComponent<PlayerItemHolder>();
        Assign(holder, "holdPoint", Child(player.transform, "Hand").transform);
        player.AddComponent<PlayerItemPlacement>();
        player.AddComponent<PlayerNoiseEmitter>();
        player.AddComponent<GhostPerceptionTarget>();
        player.SetActive(true);
        PlayerLifeState life = player.GetComponent<PlayerLifeState>();
        life.BindSession(session);
        player.AddComponent<PlayerMissionPresentation>();
        return life;
    }
    private static GameSession Session(Transform root, GhostProfile profile, JournalCatalog catalog, SafeZone zone)
    {
        var session = Child(root, "Validation session").AddComponent<GameSession>();
        Check(session.ConfigureMission(profile, catalog, zone, out _), "Mission configuration must accept catalog profiles.");
        return session;
    }
    [MenuItem("Phasma/Validate Mission Loop (Play Mode)")]
    public static void Validate()
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("Open RoomSearchPrototype and enter Play Mode first."); return; }
        checks = 0;
        GameObject root = new GameObject("Temporary mission validation");
        JournalCatalog invalidCatalog = null;
        UnityEngine.Random.State randomState = UnityEngine.Random.state;
        CursorLockMode originalLock = Cursor.lockState;
        bool originalCursor = Cursor.visible;
        try
        {
            JournalCatalog catalog = AssetDatabase.LoadAssetAtPath<JournalCatalog>("Assets/GhostProfiles/PrototypeJournalCatalog.asset");
            Check(catalog != null && catalog.TryValidate(out _) && catalog.Entries.Count == 3, "Real three-type catalog must be valid.");
            MissionDirector[] directors = UnityEngine.Object.FindObjectsByType<MissionDirector>();
            Check(directors.Length == 1, "Run in the mission scene with exactly one director.");
            SafeZone sceneZone = UnityEngine.Object.FindObjectsByType<SafeZone>().Single();
            Check(!NavMesh.SamplePosition(sceneZone.transform.position + Vector3.down * 1.5f,
                out _, 0.3f, NavMesh.AllAreas), "The safe annex floor must stay outside the ghost NavMesh.");
            GhostController sceneGhost = UnityEngine.Object.FindObjectsByType<GhostController>().Single();
            Check(catalog.FindProfile(sceneGhost.Profile) != null, "Scene must choose a catalog ghost before mission setup.");

            // All fixture positions are far from the actual scene, salt and crosses.
            Vector3 center = new Vector3(1000f, 1000f, 1000f);
            var zoneObject = Child(root.transform, "Fixture safe zone");
            zoneObject.transform.position = center;
            BoxCollider box = zoneObject.AddComponent<BoxCollider>();
            box.size = new Vector3(10f, 10f, 10f); box.isTrigger = true;
            SafeZone zone = zoneObject.AddComponent<SafeZone>();
            Check(zone.Contains(center) && !zone.Contains(center + Vector3.right * 6f), "Spatial safety boundary must be respected.");
            GhostProfile actual = catalog.Entries[0].Profile;
            GameSession session = Session(root.transform, actual, catalog, zone);
            session.CheckAllPlayersDead();
            Check(!session.HasEnded, "Empty roster must not count as all dead.");
            PlayerLifeState first = Player(root.transform, session, catalog, center);
            JournalState notes = first.GetComponent<JournalState>();
            GhostPerceptionTarget target = first.GetComponent<GhostPerceptionTarget>();
            Check(first.IsAlive && target.IsAlive && !target.CanBeDetected && session.LivingTargets().Count() == 1,
                "Safe participant must stay alive and registered, but undetectable.");
            Check(!session.TryExtract(first, out _) && !session.HasEnded, "Preparation must not finish before entering investigation.");
            PlayerLifeState outsider = Child(root.transform, "Unregistered player").AddComponent<PlayerLifeState>();
            Check(!session.BeginInvestigation(outsider), "An unregistered participant must not start investigation.");
            Check(session.BeginInvestigation(first) && !session.BeginInvestigation(first), "Investigation starts only once.");
            Check(!session.TryExtract(first, out _), "Missing journal choice must prevent extraction.");
            Check(notes.SelectGhost(catalog.Entries[0].Id), "Correct fixture choice must be selectable.");
            PlayerLifeState second = Player(root.transform, session, catalog, center + Vector3.right * 8f);
            Check(!session.TryExtract(first, out _), "Extraction must not abandon a surviving participant outside safety.");
            second.transform.position = center + Vector3.right * 2f;
            Check(!session.TryExtract(first, out _), "Every survivor must choose their own type.");
            second.GetComponent<JournalState>().SelectGhost(catalog.Entries[0].Id);
            first.transform.position = center + Vector3.right * 8f;
            Check(target.CanBeDetected && !session.TryExtract(first, out _), "Leaving safety restores eligibility but prevents extraction.");
            first.transform.position = center;
            PlayerJournalController journal = first.GetComponent<PlayerJournalController>();
            Check(journal.Open(), "Journal must open before extraction for cleanup check.");
            int ended = 0, allDead = 0;
            session.MissionEnded += () => ended++;
            session.AllPlayersDead += () => allDead++;
            Check(session.TryExtract(first, out _) && session.Result.TeamSuccess && session.Result.Participants.Count == 2,
                "All safe survivors with correct guesses must finish successfully.");
            Check(ended == 1 && allDead == 0 && session.Phase == MissionPhase.Ended,
                "Extraction emits a single completion, not an all-dead event.");
            Check(!journal.IsOpen && first.GetComponent<PlayerInputReader>().HasGameplayBlockers &&
                !journal.Open() && !session.LivingTargets().Any(), "Result must close menus and suppress gameplay/targets.");
            string savedId = session.Result.Participants[0].SelectedGhostId;
            notes.ClearSelection();
            Check(session.Result.Participants[0].SelectedGhostId == savedId, "Result must retain an immutable journal snapshot.");
            Check(!session.TryExtract(first, out _) && !first.TryDie() && ended == 1,
                "Repeated exit/death after completion must not change the outcome or emit events.");

            GameSession wrong = Session(root.transform, actual, catalog, zone);
            PlayerLifeState guesser = Player(root.transform, wrong, catalog, center);
            wrong.BeginInvestigation(guesser);
            guesser.GetComponent<JournalState>().SelectGhost(catalog.Entries[1].Id);
            Check(wrong.TryExtract(guesser, out _) && !wrong.Result.TeamSuccess && !wrong.Result.Participants[0].Correct,
                "A wrong guess must finish the mission with a negative result, not prevent exit.");
            Check(wrong.Result.ActualGhostId == catalog.Entries[0].Id &&
                wrong.Result.ActualGhostName == catalog.Entries[0].DisplayName, "Completed result must reveal the actual stable type.");

            GameSession deaths = Session(root.transform, actual, catalog, zone);
            PlayerLifeState survivor = Player(root.transform, deaths, catalog, center);
            PlayerLifeState dead = Player(root.transform, deaths, catalog, center + Vector3.right * 8f);
            deaths.BeginInvestigation(survivor);
            dead.TryDie();
            Check(!deaths.HasEnded && !deaths.TryExtract(dead, out _), "Dead requester cannot finish while a survivor remains.");
            survivor.GetComponent<JournalState>().SelectGhost(catalog.Entries[0].Id);
            Check(deaths.TryExtract(survivor, out _) && deaths.Result.Participants.Count == 2 &&
                deaths.Result.Participants.Count(p => p.Survived) == 1, "Dead teammates need not return or guess but remain in results.");
            GameSession wipe = Session(root.transform, actual, catalog, zone);
            PlayerLifeState doomed = Player(root.transform, wipe, catalog, center);
            int wipeEvents = 0;
            wipe.AllPlayersDead += () => wipeEvents++;
            doomed.TryDie(); wipe.CheckAllPlayersDead();
            Check(wipe.Result.Reason == MissionEndReason.AllPlayersDead && !wipe.Result.TeamSuccess && wipeEvents == 1,
                "A final death must use the same result pipeline and remain idempotent.");

            var ghostObject = Child(root.transform, "Safety test ghost");
            ghostObject.transform.position = center + Vector3.forward;
            GhostController controller = ghostObject.AddComponent<GhostController>();
            Assign(controller, "ghostProfile", catalog.Entries[2].Profile); // Cross-immune: no item can explain protection.
            GhostAttack attack = ghostObject.AddComponent<GhostAttack>();
            GhostPerception perception = ghostObject.AddComponent<GhostPerception>();
            GameSession safety = Session(root.transform, catalog.Entries[2].Profile, catalog, zone);
            PlayerLifeState safePlayer = Player(root.transform, safety, catalog, center + Vector3.forward * 2f);
            var noiseData = new SerializedObject(safePlayer.GetComponent<PlayerNoiseEmitter>());
            noiseData.FindProperty("currentNoiseLevel").floatValue = 1f;
            noiseData.ApplyModifiedPropertiesWithoutUndo();
            Check(safePlayer.GetComponent<GhostPerceptionTarget>().NoiseLevel == 0f, "Safe target must expose no ghost hearing noise.");
            Assign(perception, "session", safety);
            Assign(perception, "target", safePlayer.GetComponent<GhostPerceptionTarget>());
            var perceptionData = new SerializedObject(perception);
            perceptionData.FindProperty("hasKnownTargetPosition").boolValue = true;
            perceptionData.ApplyModifiedPropertiesWithoutUndo();
            Physics.SyncTransforms();
            perception.RefreshTarget();
            Check(perception.Target == null && !perception.CanSeeTarget && !perception.CanHearTarget &&
                !perception.HasKnownTargetPosition, "Safety must clear vision, hearing, target and memory.");
            Check(!attack.TryAttack(safePlayer.GetComponent<GhostPerceptionTarget>()) && safePlayer.IsAlive,
                "Safety must independently reject a cross-immune ghost's nearby attack.");
            zoneObject.SetActive(false);
            Check(safePlayer.GetComponent<GhostPerceptionTarget>().CanBeDetected, "Disabling a safe zone must release safety immediately.");
            Physics.SyncTransforms();
            perception.RefreshTarget();
            Check(perception.Target == safePlayer.GetComponent<GhostPerceptionTarget>() && perception.CanSeeTarget && perception.CanHearTarget,
                "Player must be seen and heard again outside safety.");
            Check(attack.TryAttack(safePlayer.GetComponent<GhostPerceptionTarget>()), "Unprotected nearby attack must still kill outside safety.");
            zoneObject.SetActive(true);

            // Fresh controllers are inactive until mode/catalog assignment, exercising Awake selection.
            var seen = new HashSet<GhostProfile>();
            UnityEngine.Random.InitState(917);
            for (int i = 0; i < 32; i++)
            {
                GameObject sample = Child(root.transform, "Random profile sample");
                sample.SetActive(false);
                GhostController picker = sample.AddComponent<GhostController>();
                var data = new SerializedObject(picker);
                data.FindProperty("selectionMode").enumValueIndex = (int)GhostSelectionMode.RandomFromCatalog;
                data.FindProperty("selectionCatalog").objectReferenceValue = catalog;
                data.ApplyModifiedPropertiesWithoutUndo();
                sample.SetActive(true);
                GhostProfile chosen = picker.Profile;
                Check(chosen != null && catalog.FindProfile(chosen) != null && picker.TryInitializeProfile(out _) && picker.Profile == chosen,
                    "Random choice must be in the catalog and stable across repeated initialization.");
                seen.Add(chosen);
                sample.SetActive(false);
            }
            Check(seen.Count == 3, "Seeded repeated fresh missions must exercise all three actual profiles.");
            var manual = Child(root.transform, "Manual profile sample").AddComponent<GhostController>();
            Assign(manual, "ghostProfile", actual);
            Check(manual.TryInitializeProfile(out _) && manual.Profile == actual, "Manual mode must retain its assigned profile.");
            invalidCatalog = ScriptableObject.CreateInstance<JournalCatalog>();
            var badObject = Child(root.transform, "Invalid catalog sample"); badObject.SetActive(false);
            GhostController bad = badObject.AddComponent<GhostController>();
            var badData = new SerializedObject(bad);
            badData.FindProperty("selectionMode").enumValueIndex = (int)GhostSelectionMode.RandomFromCatalog;
            badData.FindProperty("selectionCatalog").objectReferenceValue = invalidCatalog;
            badData.ApplyModifiedPropertiesWithoutUndo(); badObject.SetActive(true);
            Check(!bad.TryInitializeProfile(out _) && bad.Profile == null, "Invalid random configuration must fail without quietly using the manual fallback.");
            Check(!Child(root.transform, "Invalid config").AddComponent<GameSession>().ConfigureMission(actual, invalidCatalog, zone, out _),
                "Invalid mission catalog must be rejected.");
            Debug.Log("Mission validation PASSED: " + checks + " checks, random/manual profiles, safe targeting/attack, team extraction rules, correct/wrong choices, immutable results, death and menu cleanup. Local simulation, not a multiplayer test.");
        }
        catch (Exception error) { Debug.LogError("Mission validation FAILED: " + error); }
        finally
        {
            root.SetActive(false);
            UnityEngine.Object.Destroy(root);
            if (invalidCatalog != null) UnityEngine.Object.Destroy(invalidCatalog);
            UnityEngine.Random.state = randomState;
            Cursor.lockState = originalLock; Cursor.visible = originalCursor;
        }
    }
}
#endif
