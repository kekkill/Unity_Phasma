using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AcousticSurface), typeof(DoorAudio))]
public class Door : MonoBehaviour, IInteractable, IGhostInteractionTarget
{
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float rotationSpeed = 180f;

    [SerializeField] private bool initiallyOpen;
    [SerializeField] private Transform leafPivot;
    private Transform Pivot => leafPivot != null ? leafPivot : transform;
    [SerializeField] private string displayName = "дверь";
    [Header("Player interaction noise")]
    [SerializeField, Range(0f, 1f)] private float interactionNoise = 0.85f;
    [SerializeField, Min(0.05f)] private float noiseDuration = 0.6f;
    private bool isOpen;
    private readonly HashSet<object> lockOwners = new HashSet<object>();
    public bool IsLocked => lockOwners.Count > 0;
    public event Action<bool> OpenStateChanged;

    public string InteractionPrompt =>
        IsLocked ? "Вход заперт на время охоты" :
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
            Pivot.localRotation;

        openRotation =
            closedRotation *
            Quaternion.Euler(
                0f,
                openAngle,
                0f
            );

        isOpen = initiallyOpen;
        targetRotation = isOpen ? openRotation : closedRotation;
        Pivot.localRotation = targetRotation;
    }

    private void Update()
    {
        Pivot.localRotation =
            Quaternion.RotateTowards(
                Pivot.localRotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }

    public void Interact(GameObject interactor)
    {
        if (IsLocked) return;
        ToggleDoor();
        var source = interactor != null ? interactor.GetComponent<GhostPerceptionTarget>() : null;
        WorldNoise.Publish(NoisePosition, source, interactionNoise, noiseDuration, WorldNoiseKind.Door,
            source != null ? HidingZone.FindAt(source.transform.position) : null);
    }

    public bool IsOpen => isOpen;
    public float OpenFraction => 1f - Mathf.Clamp01(Quaternion.Angle(Pivot.localRotation, openRotation) /
        Mathf.Max(0.01f, Quaternion.Angle(closedRotation, openRotation)));
    public bool IsFullyOpen => isOpen && Quaternion.Angle(Pivot.localRotation, openRotation) < 2f;
    public Vector3 NoisePosition
    {
        get { Collider panel = GetComponentInChildren<Collider>(); return panel != null ? panel.bounds.center : transform.position + Vector3.up; }
    }
    public bool CanGhostInteract(GhostInteractionContext context)
    {
        if (!isActiveAndEnabled || IsLocked || (context.IsHunting && isOpen)) return false;
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
        if (value && IsLocked) return;
        bool changed = isOpen != value;
        isOpen = value;
        targetRotation = isOpen ? openRotation : closedRotation;
        if (changed) OpenStateChanged?.Invoke(isOpen);
    }
    public void SetLocked(object owner, bool locked)
    {
        if (owner == null) return;
        if (locked) { lockOwners.Add(owner); SetOpen(false); }
        else lockOwners.Remove(owner);
    }
}
