using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    private InputActionMap playerActionMap;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction interactAction;
    private InputAction useAction;
    private InputAction dropAction;
    private InputAction crouchAction;
    private InputAction sprintAction;
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
        playerActionMap =
            inputActions.FindActionMap("Player", true);

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
}