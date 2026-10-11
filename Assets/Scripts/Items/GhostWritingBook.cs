using UnityEngine;

public class GhostWritingBook : MonoBehaviour, IGhostInteractionTarget
{
    [Header("Writing")]
    [SerializeField] private GameObject writingVisual;

    [Range(0f, 1f)]
    [SerializeField] private float writingChance = 1f;

    [Header("Debug")]
    [SerializeField] private bool hasWriting;

    public Vector3 GhostInteractionPosition =>
        transform.position;

    private void Awake()
    {
        if (writingVisual != null)
        {
            writingVisual.SetActive(
                hasWriting
            );
        }
    }

    public bool HasWriting => hasWriting;
    public bool CanGhostInteract(GhostInteractionContext context)
    {
        PickupItem pickup = GetComponent<PickupItem>();
        return isActiveAndEnabled && !context.IsHunting && !hasWriting && context.Profile != null &&
            context.Profile.HasWritingEvidence && (pickup == null || pickup.State == PickupItemState.Placed);
    }
    public bool TryGhostInteract(GhostInteractionContext context)
    {
        if (!CanGhostInteract(context) || Random.value > writingChance) return false;
        hasWriting = true;
        if (writingVisual != null) writingVisual.SetActive(true);
        Debug.Log("Ghost wrote in the book.");
        return true;
    }
    public void GhostInteract(GhostInteractionContext context) => TryGhostInteract(context);
}
