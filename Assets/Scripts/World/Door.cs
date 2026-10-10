using UnityEngine;

public class Door : MonoBehaviour, IInteractable, IGhostInteractable
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

    public void GhostInteract(
        GhostInteractionContext context)
    {
        ToggleDoor();
    }

    private void ToggleDoor()
    {
        isOpen =
            !isOpen;

        if (isOpen)
        {
            targetRotation =
                openRotation;
        }
        else
        {
            targetRotation =
                closedRotation;
        }
    }
}