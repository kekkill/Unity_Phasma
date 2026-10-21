using UnityEngine;

[RequireComponent(typeof(AcousticSurface))]
public class Door : MonoBehaviour, IInteractable, IGhostInteractionTarget
{
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float rotationSpeed = 180f;

    [SerializeField] private bool initiallyOpen;
    [SerializeField] private string displayName = "дверь";
    [Header("Player interaction noise")]
    [SerializeField, Range(0f, 1f)] private float interactionNoise = 0.85f;
    [SerializeField, Min(0.05f)] private float noiseDuration = 0.6f;
    private bool isOpen;

    public string InteractionPrompt =>
        isOpen
            ? "[E] Закрыть " + displayName
            : "[E] Открыть " + displayName;

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
        var source = interactor != null ? interactor.GetComponent<GhostPerceptionTarget>() : null;
        WorldNoise.Publish(NoisePosition, source, interactionNoise, noiseDuration, WorldNoiseKind.Door,
            source != null ? HidingZone.FindAt(source.transform.position) : null);
    }

    public bool IsOpen => isOpen;
    public float OpenFraction => 1f - Mathf.Clamp01(Quaternion.Angle(transform.localRotation, openRotation) /
        Mathf.Max(0.01f, Quaternion.Angle(closedRotation, openRotation)));
    public bool IsFullyOpen => isOpen && Quaternion.Angle(transform.localRotation, openRotation) < 2f;
    private Vector3 NoisePosition
    {
        get { Collider panel = GetComponentInChildren<Collider>(); return panel != null ? panel.bounds.center : transform.position + Vector3.up; }
    }
    public bool CanGhostInteract(GhostInteractionContext context)
    {
        if (!isActiveAndEnabled || (context.IsHunting && isOpen)) return false;
        HidingZone zone = GetComponentInParent<HidingZone>();
        GhostHidingKnowledge knowledge = context.Controller != null ? context.Controller.GetComponent<GhostHidingKnowledge>() : null;
        return zone == null || (knowledge != null && (knowledge.CanEnter(zone) || zone.Contains(context.Controller.transform.position)));
    }
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
