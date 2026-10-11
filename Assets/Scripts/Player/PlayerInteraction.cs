using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(PlayerInputReader))]
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private float interactionRadius = 0.1f;

    [Header("UI")]
    [SerializeField] private TMP_Text interactionText;
    [SerializeField] private Image crosshair;

    private PlayerInputReader inputReader;
    private IInteractable currentInteractable;

    private void Awake()
    {
        inputReader =
            GetComponent<PlayerInputReader>();
    }

    private void Start()
    {
        SetInteractionUI(false);
    }

    private void Update()
    {
        if (inputReader.GameplayBlocked)
        {
            currentInteractable = null;
            SetInteractionUI(false);
            return;
        }
        FindInteractable();

        if (currentInteractable != null &&
            inputReader.InteractPressedThisFrame)
        {
            currentInteractable.Interact(gameObject);
        }
    }

    private void FindInteractable()
    {
        currentInteractable = null;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.SphereCast(
            ray,
            interactionRadius,
            out RaycastHit hit,
            interactionDistance,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            MonoBehaviour[] behaviours =
                hit.collider.GetComponentsInParent<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IInteractable interactable)
                {
                    currentInteractable = interactable;
                    break;
                }
            }
        }

        SetInteractionUI(
            currentInteractable != null
        );
    }

    private void SetInteractionUI(bool canInteract)
    {
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(canInteract);

            if (canInteract)
            {
                interactionText.text =
                    currentInteractable.InteractionPrompt;
            }
        }

        if (crosshair != null)
        {
            crosshair.transform.localScale =
                canInteract
                    ? Vector3.one * 1.5f
                    : Vector3.one;
        }
    }
}