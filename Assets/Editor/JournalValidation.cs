#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class JournalValidation
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static GhostProfile Profile(bool freezing, bool emf, bool uv, bool writing, GhostSaltResponse salt, bool immune)
    {
        GhostProfile profile = ScriptableObject.CreateInstance<GhostProfile>();
        var data = new SerializedObject(profile);
        data.FindProperty("hasFreezingEvidence").boolValue = freezing;
        data.FindProperty("hasEmfEvidence").boolValue = emf;
        data.FindProperty("hasUltravioletEvidence").boolValue = uv;
        data.FindProperty("hasWritingEvidence").boolValue = writing;
        data.FindProperty("saltResponse").enumValueIndex = (int)salt;
        data.FindProperty("ignoresCross").boolValue = immune;
        data.ApplyModifiedPropertiesWithoutUndo();
        return profile;
    }
    private static JournalCatalog Catalog(params GhostProfile[] profiles)
    {
        var catalog = ScriptableObject.CreateInstance<JournalCatalog>();
        var data = new SerializedObject(catalog);
        var entries = data.FindProperty("entries");
        entries.arraySize = profiles.Length;
        for (int i = 0; i < profiles.Length; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("id").stringValue = "type-" + i;
            entry.FindPropertyRelative("displayName").stringValue = "Validation type " + i;
            entry.FindPropertyRelative("profile").objectReferenceValue = profiles[i];
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        return catalog;
    }
    private static GameObject Player(Transform root, JournalCatalog catalog)
    {
        GameObject player = new GameObject("Validation journal player");
        player.SetActive(false);
        player.transform.SetParent(root);
        player.transform.position = new Vector3(1000f, 1000f, 1000f);
        player.AddComponent<PlayerJournalController>();
        var input = new SerializedObject(player.GetComponent<PlayerInputReader>());
        input.FindProperty("inputActions").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Input/PlayerControls.inputactions");
        input.ApplyModifiedPropertiesWithoutUndo();
        player.GetComponent<JournalState>().BindCatalog(catalog);
        PlayerItemHolder holder = player.AddComponent<PlayerItemHolder>();
        GameObject hand = new GameObject("Validation hand");
        hand.transform.SetParent(player.transform, false);
        var holderData = new SerializedObject(holder);
        holderData.FindProperty("holdPoint").objectReferenceValue = hand.transform;
        holderData.ApplyModifiedPropertiesWithoutUndo();
        player.AddComponent<PlayerItemPlacement>();
        player.SetActive(true);
        return player;
    }
    private static bool InvalidArgument(Action action)
    {
        try { action(); return false; }
        catch (ArgumentOutOfRangeException) { return true; }
    }
    [MenuItem("Phasma/Validate Journal (Play Mode)")]
    public static void Validate()
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("Enter Play Mode first."); return; }
        checks = 0;
        GameObject root = new GameObject("Temporary journal validation");
        var temporaryAssets = new List<ScriptableObject>();
        CursorLockMode originalLock = Cursor.lockState;
        bool originalCursor = Cursor.visible;
        try
        {
            GhostProfile a = Profile(true, false, false, true, GhostSaltResponse.AvoidUntilHunt, false);
            GhostProfile b = Profile(false, true, true, false, GhostSaltResponse.LeaveFootprints, false);
            GhostProfile c = Profile(true, true, false, false, GhostSaltResponse.Ignore, true);
            temporaryAssets.AddRange(new ScriptableObject[] { a, b, c });
            JournalCatalog catalog = Catalog(a, b, c);
            temporaryAssets.Add(catalog);
            Check(catalog.TryValidate(out _), "Fixture catalog must be valid.");
            JournalState notes = root.AddComponent<JournalState>();
            notes.BindCatalog(catalog);
            Check(notes.MatchingEntries().Count() == 3 && notes.SelectedGhost == null,
                "Unknown notes must admit all types and choose none.");
            int changes = 0;
            notes.Changed += _ => changes++;
            notes.SetMark(EvidenceType.Freezing, EvidenceMark.Confirmed);
            Check(notes.MatchingEntries().Select(entry => entry.Id).SequenceEqual(new[] { "type-0", "type-2" }),
                "Confirmed freezing must keep only profiles with freezing.");
            int previousChanges = changes;
            Check(!notes.SetMark(EvidenceType.Freezing, EvidenceMark.Confirmed) && changes == previousChanges,
                "Repeating a mark must not emit duplicate change events.");
            notes.SetMark(EvidenceType.Emf, EvidenceMark.Excluded);
            Check(notes.MatchingEntries().Single().Id == "type-0", "Excluded EMF must reject EMF profiles.");
            Check(notes.SelectGhost("type-0") && notes.SelectionMatches, "Matching type must be selectable.");
            Check(!notes.SelectGhost("missing"), "Unknown catalog ID must be rejected.");
            notes.SetMark(EvidenceType.Emf, EvidenceMark.Unknown);
            notes.SetMark(EvidenceType.Freezing, EvidenceMark.Excluded);
            Check(notes.MatchingEntries().Single().Id == "type-1" && notes.SelectedGhostId == "type-0" && !notes.SelectionMatches,
                "Changed observations must retain the user's choice but expose its contradiction.");
            Check(!notes.SelectGhost("type-0"), "An eliminated type must not be newly selectable.");
            notes.SetSalt(SaltObservation.Footprints);
            Check(notes.MatchingEntries().Single().Id == "type-1", "Footprints must match the footprint salt policy.");
            notes.SetCross(CrossObservation.Ignored);
            Check(!notes.MatchingEntries().Any(), "Contradictory salt/cross observations must allow zero candidates.");
            notes.ResetNotes();
            Check(notes.MatchingEntries().Count() == 3 && notes.SelectedGhost == null &&
                notes.Salt == SaltObservation.Unknown && notes.Cross == CrossObservation.Unknown &&
                Enumerable.Range(0, JournalState.EvidenceCount).All(i => notes.GetMark((EvidenceType)i) == EvidenceMark.Unknown),
                "Reset must clear every note, observation and selection.");
            notes.SetSalt(SaltObservation.Ignored);
            notes.SetCross(CrossObservation.Ignored);
            Check(notes.MatchingEntries().Single().Id == "type-2", "Ignoring salt/cross must identify the immune fixture.");
            notes.ResetNotes();
            notes.SetMark(EvidenceType.Ultraviolet, EvidenceMark.Confirmed);
            Check(notes.MatchingEntries().Single().Id == "type-1", "UV must filter independently.");
            notes.ResetNotes();
            notes.SetMark(EvidenceType.Writing, EvidenceMark.Confirmed);
            Check(notes.MatchingEntries().Single().Id == "type-0", "Writing must filter independently.");
            notes.ResetNotes();
            notes.SetSalt(SaltObservation.AvoidedOutsideHunt);
            notes.SetCross(CrossObservation.Repelled);
            Check(notes.MatchingEntries().Single().Id == "type-0", "Avoiding salt and being repelled must match the ordinary type.");
            Check(InvalidArgument(() => notes.SetMark((EvidenceType)99, EvidenceMark.Confirmed)) &&
                InvalidArgument(() => notes.SetMark(EvidenceType.Emf, (EvidenceMark)99)) &&
                InvalidArgument(() => notes.SetSalt((SaltObservation)99)) &&
                InvalidArgument(() => notes.SetCross((CrossObservation)99)), "Invalid observation values must be rejected.");
            notes.ResetNotes();
            Check(notes.SelectGhost("type-0"), "Selection must still work after reset.");
            notes.ClearSelection();
            Check(notes.SelectedGhost == null, "Explicit clearing must remove the selection.");
            var catalogData = new SerializedObject(catalog);
            catalogData.FindProperty("entries").GetArrayElementAtIndex(1).FindPropertyRelative("id").stringValue = "type-0";
            catalogData.ApplyModifiedPropertiesWithoutUndo();
            Check(!catalog.TryValidate(out _), "Duplicate stable IDs must fail validation.");
            catalogData.FindProperty("entries").GetArrayElementAtIndex(1).FindPropertyRelative("id").stringValue = "type-1";
            catalogData.ApplyModifiedPropertiesWithoutUndo();
            JournalCatalog realCatalog = AssetDatabase.LoadAssetAtPath<JournalCatalog>("Assets/GhostProfiles/PrototypeJournalCatalog.asset");
            Check(realCatalog != null && realCatalog.TryValidate(out _) && realCatalog.Entries.Count == 3,
                "The scene's actual catalog must be valid and contain three types.");
            notes.BindCatalog(realCatalog);
            notes.SetSalt(SaltObservation.Footprints);
            Check(notes.MatchingEntries().Single().Profile.SaltResponse == GhostSaltResponse.LeaveFootprints,
                "Real catalog must narrow using actual profile traits.");

            GameObject first = Player(root.transform, catalog), second = Player(root.transform, catalog);
            PlayerJournalController controller = first.GetComponent<PlayerJournalController>();
            JournalState firstNotes = first.GetComponent<JournalState>(), secondNotes = second.GetComponent<JournalState>();
            PlayerInputReader firstInput = first.GetComponent<PlayerInputReader>(), secondInput = second.GetComponent<PlayerInputReader>();
            PlayerItemPlacement placement = first.GetComponent<PlayerItemPlacement>();
            GameObject itemObject = new GameObject("Validation held item");
            itemObject.transform.SetParent(root.transform);
            itemObject.AddComponent<BoxCollider>();
            itemObject.AddComponent<Rigidbody>();
            PickupItem item = itemObject.AddComponent<PickupItem>();
            Check(first.GetComponent<PlayerItemHolder>().TryPickup(item), "Fixture must hold an item.");
            placement.Begin();
            Check(placement.IsPlacing, "Placement preview must start before opening journal.");
            float timeScale = Time.timeScale;
            Check(controller.Open() && controller.IsOpen && firstInput.HasGameplayBlockers && !placement.IsPlacing,
                "Opening journal must block its controls and cancel placement.");
            Check(Cursor.lockState == CursorLockMode.None && Cursor.visible && Time.timeScale == timeScale,
                "Journal must unlock cursor without pausing the world.");
            Check(firstInput.MoveInput == Vector2.zero && firstInput.LookInput == Vector2.zero &&
                !firstInput.UsePressedThisFrame && !firstInput.DropPressedThisFrame && !firstInput.PlacePressedThisFrame &&
                !firstInput.InteractPressedThisFrame && !firstInput.SprintIsPressed && !firstInput.CrouchIsPressed,
                "Open journal must suppress all exposed gameplay controls.");
            firstNotes.SetMark(EvidenceType.Writing, EvidenceMark.Confirmed);
            firstNotes.SelectGhost("type-0");
            Check(!secondInput.HasGameplayBlockers && secondNotes.MatchingEntries().Count() == 3 && secondNotes.SelectedGhost == null,
                "Another participant's controls and notes must remain independent.");
            object anotherUI = new object();
            firstInput.SetGameplayBlocked(anotherUI, true);
            controller.Close();
            Check(!controller.IsOpen && firstInput.HasGameplayBlockers, "Closing journal must preserve another UI's input block.");
            firstInput.SetGameplayBlocked(anotherUI, false);
            Check(!firstInput.HasGameplayBlockers && firstInput.GameplayBlocked,
                "Final close must release owners but consume gameplay input for the closing frame.");
            Check(firstNotes.SelectedGhostId == "type-0" && firstNotes.GetMark(EvidenceType.Writing) == EvidenceMark.Confirmed,
                "Closing must preserve notes and final choice.");
            Check(controller.Open(), "Reopening must work.");
            first.GetComponent<PlayerLifeState>().TryDie();
            Check(!controller.IsOpen && !firstInput.HasGameplayBlockers && !controller.Open() && firstNotes.SelectedGhostId == "type-0",
                "Death must close the journal, prevent reopening and preserve personal notes.");
            Debug.Log("Journal validation PASSED: " + checks + " checks, four evidence filters, behavior filters, choice conflicts, catalog IDs, per-player isolation, placement cancellation, input ownership and death cleanup. Local simulation, not a multiplayer test.");
        }
        catch (Exception error) { Debug.LogError("Journal validation FAILED: " + error.Message); }
        finally
        {
            root.SetActive(false);
            UnityEngine.Object.Destroy(root);
            foreach (ScriptableObject asset in temporaryAssets) UnityEngine.Object.Destroy(asset);
            Cursor.lockState = originalLock;
            Cursor.visible = originalCursor;
        }
    }
}
#endif
