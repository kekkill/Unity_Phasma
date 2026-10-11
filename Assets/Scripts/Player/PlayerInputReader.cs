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
    public bool PlacePressedThisFrame => placeAction != null && placeAction.WasPressedThisFrame();
    public Vector2 MoveInput =>
        moveAction.ReadValue<Vector2>();

    public Vector2 LookInput =>
        lookAction.ReadValue<Vector2>();

    public bool InteractPressedThisFrame =>
        interactAction.WasPressedThisFrame();

    public bool UsePressedThisFrame =>
        useAction.WasPressedThisFrame();

    public bool DropPressedThisFrame =>
        dropAction.WasPressedThisFrame();

    public bool CrouchIsPressed =>
        crouchAction.IsPressed();

    public bool SprintIsPressed =>
        sprintAction.IsPressed();

    private void Awake()
    {
        // Each participant owns its action maps. Disabling a dead player's input
        // must not disable maps used by another participant.
        runtimeActions = Instantiate(inputActions);
        playerActionMap = runtimeActions.FindActionMap("Player", true);

        placeAction = playerActionMap.FindAction("Place", false);

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