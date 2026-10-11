using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    private InputActionAsset runtimeActions;
    private InputActionMap playerActionMap;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction interactAction;
    private InputAction useAction;
    private InputAction dropAction;
    private InputAction crouchAction;
    private InputAction sprintAction;
    private InputAction placeAction;
    private InputAction journalAction;
    private InputAction closeJournalAction;
    private readonly HashSet<object> gameplayBlockers = new HashSet<object>();
    private int resumeAfterFrame = -1;
    public bool HasGameplayBlockers => gameplayBlockers.Count > 0;
    public bool GameplayBlocked => HasGameplayBlockers || Time.frameCount <= resumeAfterFrame;
    public void SetGameplayBlocked(object owner, bool blocked)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (blocked) gameplayBlockers.Add(owner);
        else if (gameplayBlockers.Remove(owner) && gameplayBlockers.Count == 0)
            resumeAfterFrame = Time.frameCount; // Closing a UI cannot also use/place an item in this frame.
    }
    // Menu actions stay available while gameplay actions are blocked.
    public bool JournalPressedThisFrame => journalAction != null && journalAction.WasPressedThisFrame();
    public bool CloseJournalPressedThisFrame => closeJournalAction != null && closeJournalAction.WasPressedThisFrame();
    public bool PlacePressedThisFrame => !GameplayBlocked && placeAction != null && placeAction.WasPressedThisFrame();
    public Vector2 MoveInput => !GameplayBlocked && moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 LookInput => !GameplayBlocked && lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
    public bool InteractPressedThisFrame => !GameplayBlocked && interactAction != null && interactAction.WasPressedThisFrame();
    public bool UsePressedThisFrame => !GameplayBlocked && useAction != null && useAction.WasPressedThisFrame();
    public bool DropPressedThisFrame => !GameplayBlocked && dropAction != null && dropAction.WasPressedThisFrame();
    public bool CrouchIsPressed => !GameplayBlocked && crouchAction != null && crouchAction.IsPressed();
    public bool SprintIsPressed => !GameplayBlocked && sprintAction != null && sprintAction.IsPressed();

    private void Awake()
    {
        // Each participant owns its action maps. Disabling a dead player's input
        // must not disable maps used by another participant.
        runtimeActions = Instantiate(inputActions);
        playerActionMap = runtimeActions.FindActionMap("Player", true);

        placeAction = playerActionMap.FindAction("Place", false);
        journalAction = playerActionMap.FindAction("Journal", false);
        closeJournalAction = playerActionMap.FindAction("CloseJournal", false);

        moveAction =
            playerActionMap.FindAction("Move", true);

        lookAction =
            playerActionMap.FindAction("Look", true);

        interactAction =
            playerActionMap.FindAction("Interact", true);

        useAction =
            playerActionMap.FindAction("Use", true);

        dropAction =
            playerActionMap.FindAction("Drop", true);

        crouchAction =
            playerActionMap.FindAction("Crouch", true);

        sprintAction =
            playerActionMap.FindAction("Sprint", true);
    }

    private void OnEnable()
    {
        playerActionMap?.Enable();
    }

    private void OnDisable()
    {
        playerActionMap?.Disable();
    }
    private void OnDestroy()
    {
        if (runtimeActions != null) Destroy(runtimeActions);
    }
}