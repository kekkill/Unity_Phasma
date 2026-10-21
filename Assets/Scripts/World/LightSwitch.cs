using System.Collections.Generic;
using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable, IGhostInteractionTarget
{
    [SerializeField] private Light targetLight;
    [SerializeField] private Transform switchVisual;
    [SerializeField] private BuildingPower circuit;

    [SerializeField] private float onAngle = -20f;
    [SerializeField] private float offAngle = 20f;

    private bool isOn;
    private readonly HashSet<object> suppressors = new HashSet<object>();
    public bool IsOn => isOn;
    public void SetSuppressed(object owner, bool suppressed)
    {
        if (owner == null) return;
        if (suppressed) suppressors.Add(owner); else suppressors.Remove(owner);
        ApplyLight();
    }
    private void ApplyLight()
    {
        if (targetLight != null)
            targetLight.enabled = isOn && (circuit == null || circuit.IsPowered) && suppressors.Count == 0;
    }
    private void OnEnable()
    {
        if (circuit != null) circuit.PowerChanged += OnPowerChanged;
        ApplyLight();
    }
    private void OnDisable()
    {
        if (circuit != null) circuit.PowerChanged -= OnPowerChanged;
    }
    private void OnPowerChanged(bool powered) => ApplyLight();

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
        ApplyLight();
    }

    public void Interact(GameObject interactor)
    {
        ToggleLight();
    }

    public bool CanGhostInteract(GhostInteractionContext context) => isActiveAndEnabled && !context.IsHunting && targetLight != null;
    public bool TryGhostInteract(GhostInteractionContext context)
    {
        if (!CanGhostInteract(context)) return false;
        ToggleLight();
        return true;
    }
    public void GhostInteract(GhostInteractionContext context) => TryGhostInteract(context);

    private void ToggleLight()
    {
        isOn =
            !isOn;

        ApplyLight();

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
