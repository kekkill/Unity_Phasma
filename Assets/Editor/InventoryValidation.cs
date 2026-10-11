#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class InventoryValidation
{
    private static bool running;
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void CheckResolvedKeyboardBindings(InputActionAsset source)
    {
        // Resolve the actual layout, not just expected strings. No input events are
        // queued, so this isolated device cannot switch the gameplay player's slots.
        Keyboard keyboard = null;
        InputActionAsset isolated = null;
        try
        {
            keyboard = InputSystem.AddDevice<Keyboard>("Inventory binding validation");
            isolated = UnityEngine.Object.Instantiate(source);
            isolated.devices = new InputDevice[] { keyboard };
            isolated.Enable();
            InputActionMap map = isolated.FindActionMap("Player", true);
            Check(map.FindAction("Slot1", true).controls.Contains(keyboard.digit1Key) &&
                map.FindAction("Slot2", true).controls.Contains(keyboard.digit2Key) &&
                map.FindAction("Slot3", true).controls.Contains(keyboard.digit3Key) &&
                map.FindAction("Slot1", true).controls.Contains(keyboard.numpad1Key) &&
                map.FindAction("Slot2", true).controls.Contains(keyboard.numpad2Key) &&
                map.FindAction("Slot3", true).controls.Contains(keyboard.numpad3Key) &&
                map.FindAction("Drop", true).controls.Contains(keyboard.gKey),
                "Slot actions must resolve to real top-row/numpad 1/2/3 controls and Drop to G; unresolved paths are invalid.");
        }
        finally
        {
            if (isolated != null) { isolated.Disable(); UnityEngine.Object.Destroy(isolated); }
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        }
    }
    private static GameObject Player(Transform root, JournalCatalog catalog, Vector3 position)
    {
        var player = new GameObject("Inventory validation player");
        player.SetActive(false);
        player.transform.SetParent(root);
        player.transform.position = position;
        player.AddComponent<PlayerJournalController>();
        Assign(player.GetComponent<PlayerInputReader>(), "inputActions",
            AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Input/PlayerControls.inputactions"));
        player.GetComponent<JournalState>().BindCatalog(catalog);
        PlayerItemHolder holder = player.AddComponent<PlayerItemHolder>();
        var hand = new GameObject("Validation hand");
        hand.transform.SetParent(player.transform, false);
        hand.transform.localPosition = Vector3.up;
        Assign(holder, "holdPoint", hand.transform);
        var camera = new GameObject("Validation camera");
        camera.transform.SetParent(player.transform, false);
        Camera view = camera.AddComponent<Camera>();
        view.enabled = false;
        Assign(player.AddComponent<PlayerItemPlacement>(), "playerCamera", view);
        PlayerItemController controller = player.AddComponent<PlayerItemController>();
        PlayerDeathPresentation death = player.AddComponent<PlayerDeathPresentation>();
        var deathData = new SerializedObject(death);
        var controls = deathData.FindProperty("controlsToDisable");
        controls.arraySize = 3;
        controls.GetArrayElementAtIndex(0).objectReferenceValue = player.GetComponent<PlayerInputReader>();
        controls.GetArrayElementAtIndex(1).objectReferenceValue = controller;
        controls.GetArrayElementAtIndex(2).objectReferenceValue = player.GetComponent<PlayerItemPlacement>();
        deathData.ApplyModifiedPropertiesWithoutUndo();
        player.AddComponent<PlayerMissionPresentation>();
        player.SetActive(true);
        return player;
    }
    private static PickupItem Item(Transform root, List<PickupItem> items, string name, Type feature = null)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.SetActive(false);
        obj.name = name;
        obj.transform.SetParent(root, false);
        obj.transform.localPosition = new Vector3(5000f, 1f, 5000f);
        obj.transform.localScale = Vector3.one * 0.2f;
        obj.AddComponent<Rigidbody>();
        var trigger = new GameObject("Original child trigger");
        trigger.transform.SetParent(obj.transform, false);
        trigger.AddComponent<SphereCollider>().isTrigger = true;
        var disabled = new GameObject("Originally disabled collider");
        disabled.transform.SetParent(obj.transform, false);
        disabled.AddComponent<BoxCollider>().enabled = false;
        PickupItem item = obj.AddComponent<PickupItem>();
        if (feature != null) obj.AddComponent(feature);
        if (feature == typeof(FlashlightItem))
        {
            var lamp = new GameObject("Validation light");
            lamp.transform.SetParent(obj.transform, false);
            Assign(obj.GetComponent<FlashlightItem>(), "flashlightLight", lamp.AddComponent<Light>());
        }
        if (feature == typeof(GhostWritingBook))
        {
            var data = new SerializedObject(obj.GetComponent<GhostWritingBook>());
            data.FindProperty("hasWriting").boolValue = true;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        obj.SetActive(true);
        items.Add(item);
        return item;
    }
    private static bool Dropped(PickupItem item) => item.InventoryOwner == null &&
        item.State == PickupItemState.Dropped && item.gameObject.activeInHierarchy &&
        item.transform.parent == null && !item.GetComponent<Rigidbody>().isKinematic &&
        item.GetComponent<Rigidbody>().useGravity && item.GetComponent<Collider>().enabled;
    private static async Task NextFrame()
    {
        int frame = Time.frameCount;
        double deadline = EditorApplication.timeSinceStartup + 3;
        while (EditorApplication.isPlaying && !EditorApplication.isPaused && Time.frameCount == frame &&
            EditorApplication.timeSinceStartup < deadline) await Task.Delay(10);
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || Time.frameCount == frame)
            throw new InvalidOperationException("Play Mode stopped/paused or no next frame arrived.");
    }

    [MenuItem("Phasma/Validate Inventory (Play Mode)")]
    public static async void Validate()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || running)
        {
            Debug.LogWarning("Enter unpaused Play Mode first; wait if inventory validation is already running.");
            return;
        }
        running = true;
        checks = 0;
        var root = new GameObject("Temporary inventory validation");
        var items = new List<PickupItem>();
        SaltPile poured = null;
        GhostProfile profile = ScriptableObject.CreateInstance<GhostProfile>();
        CursorLockMode cursorLock = Cursor.lockState;
        bool cursorVisible = Cursor.visible;
        try
        {
            JournalCatalog catalog = AssetDatabase.LoadAssetAtPath<JournalCatalog>("Assets/GhostProfiles/PrototypeJournalCatalog.asset");
            Check(catalog != null && catalog.TryValidate(out _), "Real journal catalog must be valid.");
            InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Input/PlayerControls.inputactions");
            InputActionMap map = controls.FindActionMap("Player", true);
            Check(Enumerable.Range(1, 3).All(i => map.FindAction("Slot" + i, true).bindings.Any(b => b.path == "<Keyboard>/" + i)) &&
                map.FindAction("Drop", true).bindings.Any(b => b.path == "<Keyboard>/g"), "Three slot keys and G must be bound in the real asset.");
            CheckResolvedKeyboardBindings(controls);
            GameObject first = Player(root.transform, catalog, new Vector3(5000f, 0f, 5000f));
            GameObject second = Player(root.transform, catalog, new Vector3(5010f, 0f, 5000f));
            PlayerItemHolder holder = first.GetComponent<PlayerItemHolder>(), other = second.GetComponent<PlayerItemHolder>();
            PlayerInventory inventory = holder.Inventory;
            Check(PlayerInventory.SlotCount == 3 && inventory.Count == 0 && inventory.SelectedSlot == 0 && !holder.HasItem,
                "A new participant must start with three empty slots and slot 1 selected.");
            Check(!inventory.SelectSlot(-1) && !inventory.SelectSlot(3) && !holder.TryPickup(null), "Invalid commands must leave inventory unchanged.");
            PickupItem flashlight = Item(root.transform, items, "Flashlight", typeof(FlashlightItem));
            Light light = flashlight.GetComponentInChildren<Light>(true);
            Check(holder.TryPickup(flashlight) && inventory.Count == 1 && inventory.GetItem(0) == flashlight &&
                flashlight.InventoryOwner == inventory && flashlight.State == PickupItemState.Held, "First pickup must occupy slot 1 and equip the same object.");
            Check(flashlight.GetComponentsInChildren<Collider>(true).All(c => !c.enabled) && flashlight.GetComponent<Rigidbody>().isKinematic,
                "All held colliders must be disabled and physics suspended.");
            Check(!holder.TryPickup(flashlight) && !other.TryPickup(flashlight), "Held equipment cannot be duplicated or stolen by another participant.");
            PickupItem crossItem = Item(root.transform, items, "Cross", typeof(CrossItem));
            CrossItem cross = crossItem.GetComponent<CrossItem>();
            Check(holder.TryPickup(crossItem) && inventory.SelectedSlot == 1 && inventory.Count == 2 && cross.IsActive,
                "Second pickup must equip slot 2 and activate the held cross.");
            Check(flashlight.State == PickupItemState.Stored && !flashlight.gameObject.activeInHierarchy && light.enabled &&
                !light.gameObject.activeInHierarchy && flashlight.InventoryOwner == inventory,
                "Stored flashlight must retain its on state but emit no light or collisions.");
            PickupItem saltItem = Item(root.transform, items, "Salt", typeof(SaltContainer));
            SaltContainer salt = saltItem.GetComponent<SaltContainer>();
            Check(holder.TryPickup(saltItem) && inventory.SelectedSlot == 2 && inventory.IsFull && !cross.IsActive &&
                crossItem.State == PickupItemState.Stored, "Third pickup must fill inventory and remove stored-cross protection.");
            Check(!other.TryPickup(crossItem) && inventory.Count == 3 && other.Inventory.Count == 0,
                "Inactive stored equipment must retain exclusive per-participant ownership.");
            PickupItem book = Item(root.transform, items, "Written book", typeof(GhostWritingBook));
            Check(!holder.TryPickup(book) && inventory.Count == 3 && book.InventoryOwner == null &&
                book.State == PickupItemState.Dropped && book.GetComponent<Collider>().enabled,
                "Full inventory must leave the fourth world item untouched.");
            Check(inventory.SelectSlot(0) && holder.CurrentItem == flashlight && flashlight.gameObject.activeInHierarchy && light.enabled &&
                saltItem.State == PickupItemState.Stored, "Switching must restore the selected light and hide the former item.");
            holder.UseCurrentItem(first);
            Check(!light.enabled, "Only the selected item must receive use.");
            inventory.SelectSlot(1);
            inventory.SelectSlot(0);
            Check(!light.enabled && inventory.Count == 3, "Switching must preserve flashlight state without recreating items.");
            inventory.SelectSlot(1);
            var data = new SerializedObject(profile);
            data.FindProperty("crossBreakChance").floatValue = 1f;
            data.FindProperty("angerCrossBreakContribution").floatValue = 0f;
            data.ApplyModifiedPropertiesWithoutUndo();
            Check(cross.TryProtect(profile, 0f) && cross.IsBroken, "Fixture attack protection must break the selected cross.");
            inventory.SelectSlot(2);
            inventory.SelectSlot(1);
            Check(cross.IsBroken && !cross.IsActive, "Switching must retain the broken cross state.");
            Check(holder.PlaceCurrentItem(first.transform.position + Vector3.right, Quaternion.identity) &&
                inventory.GetItem(1) == null && inventory.Count == 2 && crossItem.InventoryOwner == null &&
                crossItem.State == PickupItemState.Placed && crossItem.gameObject.activeInHierarchy,
                "Placement must free only the selected slot and release ownership.");
            Check(!holder.HasItem && !holder.PlaceCurrentItem(Vector3.zero, Quaternion.identity), "An empty selected slot must leave the hand empty.");
            int portions = salt.Portions;
            holder.UseCurrentItem(first);
            Check(salt.Portions == portions, "Use on an empty slot must not use stored salt.");
            Check(holder.TryPickup(book) && inventory.SelectedSlot == 1 && inventory.GetItem(1) == book &&
                new SerializedObject(book.GetComponent<GhostWritingBook>()).FindProperty("hasWriting").boolValue,
                "Pickup must prefer the empty selected slot and retain existing writing.");
            holder.PlaceCurrentItem(first.transform.position + Vector3.forward, Quaternion.identity);
            inventory.SelectSlot(2);
            Check(salt.Pour(new Vector3(5020f, 0f, 5000f)) && salt.Portions == portions - 1 && inventory.GetItem(2) == saltItem,
                "Pouring must consume a portion while retaining the salt container in its slot.");
            poured = SaltPile.Active.First(p => p != null && Vector3.Distance(p.transform.position, new Vector3(5020f, 0f, 5000f)) < 0.1f);
            inventory.SelectSlot(0);
            inventory.SelectSlot(2);
            Check(salt.Portions == portions - 1, "Salt portions must survive switching.");
            PlayerItemPlacement placement = first.GetComponent<PlayerItemPlacement>();
            PlayerItemController controller = first.GetComponent<PlayerItemController>();
            holder.UseCurrentItem(first);
            Check(placement.IsPlacing, "Using held salt must begin placement preview.");
            Check(controller.TrySelectSlot(0) && !placement.IsPlacing && holder.CurrentItem == flashlight,
                "Slot switching must cancel the previous placement preview immediately.");
            PlayerInputReader input = first.GetComponent<PlayerInputReader>();
            Check(!input.UsePressedThisFrame && !input.PlacePressedThisFrame && !input.DropPressedThisFrame &&
                !input.InteractPressedThisFrame, "A switch must consume this frame's item actions.");
            await NextFrame();
            PlayerJournalController journal = first.GetComponent<PlayerJournalController>();
            Check(journal.Open() && input.GameplayBlocked, "Journal must block its participant's input.");
            Check(!controller.TrySelectSlot(2) && inventory.SelectedSlot == 0 && !holder.TryPickup(book) &&
                !holder.PlaceCurrentItem(Vector3.zero, Quaternion.identity) && input.RequestedSlotThisFrame == -1,
                "Open journal must prevent slot switching, pickup and placement.");
            holder.DropCurrentItem();
            holder.UseCurrentItem(first);
            Check(inventory.GetItem(0) == flashlight && !light.enabled, "Journal must also prevent drop and item use.");
            Check(other.Inventory.SelectSlot(1) && !second.GetComponent<PlayerInputReader>().GameplayBlocked,
                "One participant's journal must not block another inventory.");
            journal.Close();
            Check(!controller.TrySelectSlot(2), "The journal closing frame must not also switch equipment.");
            await NextFrame();
            Check(controller.TrySelectSlot(2), "Switching must resume after the journal closing frame.");
            holder.DropCurrentItem();
            Check(Dropped(saltItem) && inventory.GetItem(2) == null && inventory.Count == 1 && salt.Portions == portions - 1,
                "Drop command must release selected ownership and restore world physics without resetting portions.");
            Check(saltItem.GetComponentInChildren<SphereCollider>().enabled &&
                !saltItem.GetComponentsInChildren<BoxCollider>().Single(c => c.gameObject.name == "Originally disabled collider").enabled,
                "Drop must restore original child-collider flags, including originally disabled colliders.");
            Check(other.TryPickup(saltItem) && saltItem.InventoryOwner == other.Inventory && inventory.Count == 1,
                "Released equipment must be collectible by another local participant.");
            other.DropCurrentItem();
            Check(holder.TryPickup(saltItem) && salt.Portions == portions - 1, "Drop and repickup must retain the same container state.");
            UnityEngine.Object.Destroy(flashlight.gameObject);
            await NextFrame();
            Check(inventory.GetItem(0) == null && inventory.Count == 1 && holder.CurrentItem == saltItem,
                "Destroying stored equipment must free its slot without changing selected equipment.");
            PickupItem replacement = Item(root.transform, items, "Replacement flashlight", typeof(FlashlightItem));
            Check(holder.TryPickup(replacement) && inventory.SelectedSlot == 0, "Pickup must reuse the first remaining empty slot.");
            Check(holder.TryPickup(book) && inventory.Count == 3 && inventory.SelectedSlot == 1,
                "A placed book must be collectible into the remaining slot.");
            inventory.SelectSlot(0);
            inventory.SelectSlot(1);
            Check(new SerializedObject(book.GetComponent<GhostWritingBook>()).FindProperty("hasWriting").boolValue,
                "Written-book state must survive storage and re-equipping.");
            PickupItem otherItem = Item(root.transform, items, "Other participant item", typeof(MotionSensorItem));
            Check(other.TryPickup(otherItem) && !otherItem.GetComponent<MotionSensorItem>().IsPlaced,
                "Held motion sensor must remain inactive until placed.");
            var session = new GameObject("Inventory death session").AddComponent<GameSession>();
            session.transform.SetParent(root.transform);
            first.GetComponent<PlayerLifeState>().BindSession(session);
            second.GetComponent<PlayerLifeState>().BindSession(session);
            Check(first.GetComponent<PlayerLifeState>().TryDie() && inventory.Count == 0 && !holder.HasItem,
                "Death must release all three inventory slots.");
            Check(new[] { replacement, book, saltItem }.All(Dropped) &&
                new[] { replacement, book, saltItem }.All(i => i.GetComponentInChildren<Renderer>(true).enabled),
                "All dropped equipment must remain active, visible and detached before hiding the dead player.");
            Check(!session.HasEnded && other.Inventory.Count == 1 && other.CurrentItem == otherItem,
                "One death must not clear the surviving participant's inventory or end the round.");
            Check(!holder.TryPickup(book) && !inventory.SelectSlot(0) && !controller.TrySelectSlot(0),
                "A dead participant must not collect or switch equipment.");
            holder.DropAllItems();
            Check(inventory.Count == 0 && new[] { replacement, book, saltItem }.All(Dropped), "Repeated death cleanup must be harmless.");
            Check(second.GetComponent<PlayerLifeState>().TryDie() && session.HasEnded && other.Inventory.Count == 0 && Dropped(otherItem),
                "Final death must drop remaining equipment before completing the mission.");

            GameObject survivor = Player(root.transform, catalog, new Vector3(5050f, 0f, 5000f));
            PlayerItemHolder survivorHolder = survivor.GetComponent<PlayerItemHolder>();
            Check(survivorHolder.Inventory.Count == 0 && survivorHolder.Inventory.SelectedSlot == 0,
                "A newly created participant must start with an empty inventory.");
            var zoneObject = new GameObject("Inventory extraction zone");
            zoneObject.transform.SetParent(root.transform);
            zoneObject.transform.position = survivor.transform.position;
            BoxCollider zoneBox = zoneObject.AddComponent<BoxCollider>();
            zoneBox.size = Vector3.one * 10f;
            zoneBox.isTrigger = true;
            SafeZone zone = zoneObject.AddComponent<SafeZone>();
            var mission = new GameObject("Inventory extraction session").AddComponent<GameSession>();
            mission.transform.SetParent(root.transform);
            Check(mission.ConfigureMission(catalog.Entries[0].Profile, catalog, zone, out _), "Extraction fixture must configure a real mission.");
            PlayerLifeState survivorLife = survivor.GetComponent<PlayerLifeState>();
            survivorLife.BindSession(mission);
            survivor.GetComponent<JournalState>().SelectGhost(catalog.Entries[0].Id);
            Check(survivorHolder.TryPickup(saltItem) && mission.BeginInvestigation(survivorLife), "Living participant must carry equipment during investigation.");
            Physics.SyncTransforms();
            Check(mission.TryExtract(survivorLife, out _) && mission.HasEnded, "Extraction must still complete with occupied inventory.");
            survivorHolder.DropCurrentItem();
            Check(survivorHolder.Inventory.Count == 1 && !survivor.GetComponent<PlayerItemController>().TrySelectSlot(1) &&
                !survivorHolder.TryPickup(book) && !survivorHolder.PlaceCurrentItem(Vector3.zero, Quaternion.identity),
                "Completed mission must block item commands without spilling surviving equipment.");
            Debug.Log("Inventory validation PASSED: " + checks + " checks, three slots, exclusive local ownership, stored equipment, state preservation, journal/placement guards, drop/death and mission completion. Local simulation, not a multiplayer test.");
        }
        catch (Exception error) { Debug.LogError("Inventory validation FAILED: " + error.Message); }
        finally
        {
            if (root != null) root.SetActive(false);
            foreach (PickupItem item in items)
                if (item != null) { item.gameObject.SetActive(false); UnityEngine.Object.Destroy(item.gameObject); }
            if (poured != null) { poured.gameObject.SetActive(false); UnityEngine.Object.Destroy(poured.gameObject); }
            if (root != null) UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(profile);
            Cursor.lockState = cursorLock;
            Cursor.visible = cursorVisible;
            running = false;
        }
    }
}
#endif
