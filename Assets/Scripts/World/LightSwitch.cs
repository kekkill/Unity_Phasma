using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable, IGhostInteractable
{
    [SerializeField] private Light targetLight;
    [SerializeField] private Transform switchVisual;

    [SerializeField] private float onAngle = -20f;
    [SerializeField] private float offAngle = 20f;

    private bool isOn;

    public string InteractionPrompt =>
        isOn
            ? "[E] Выключить свет"
            : "[E] Включить свет";

    public Vector3 GhostInteractionPosition =>
        transform.position;

    private void Awake()
    {
        if (targetLight != null)
        {
            isOn =
                targetLight.enabled;
        }

        UpdateVisual();
    }

    public void Interact(GameObject interactor)
    {
        ToggleLight();
    }

    public void GhostInteract(
        GhostInteractionContext context)
    {
        ToggleLight();
    }

    private void ToggleLight()
    {
        isOn =
            !isOn;

        if (targetLight != null)
        {
            targetLight.enabled =
                isOn;
        }

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (switchVisual == null)
        {
            return;
        }

        float angle =
            isOn
                ? onAngle
                : offAngle;

        switchVisual.localRotation =
            Quaternion.Euler(
                angle,
                0f,
                0f
            );
    }
}