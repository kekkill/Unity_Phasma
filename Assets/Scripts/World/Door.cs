using UnityEngine;

public class Door : MonoBehaviour, IInteractable, IGhostInteractionTarget
{
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float rotationSpeed = 180f;

    [SerializeField] private bool initiallyOpen;
    private bool isOpen;

    public string InteractionPrompt =>
        isOpen
            ? "[E] Закрыть дверь"
            : "[E] Открыть дверь";

    public Vector3 GhostInteractionPosition =>
        transform.position;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Quaternion targetRotation;

    private void Awake()
    {
        closedRotation =
            transform.localRotation;

        openRotation =
            closedRotation *
            Quaternion.Euler(
                0f,
                openAngle,
                0f
            );

        isOpen = initiallyOpen;
        targetRotation = isOpen ? openRotation : closedRotation;
        transform.localRotation = targetRotation;
    }

    private void Update()
    {
        transform.localRotation =
            Quaternion.RotateTowards(
                transform.localRotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }

    public void Interact(GameObject interactor)
    {
        ToggleDoor();
    }

    public bool IsOpen => isOpen;
    public bool CanGhostInteract(GhostInteractionContext context) => isActiveAndEnabled && (!context.IsHunting || !isOpen);
    public bool TryGhostInteract(GhostInteractionContext context)
    {
        if (!CanGhostInteract(context)) return false;
        SetOpen(context.IsHunting || !isOpen);
        return true;
    }
    public void GhostInteract(GhostInteractionContext context) => TryGhostInteract(context);
    private void ToggleDoor() => SetOpen(!isOpen);
    public void SetOpen(bool value)
    {
        isOpen = value;
        targetRotation = isOpen ? openRotation : closedRotation;
    }
}
