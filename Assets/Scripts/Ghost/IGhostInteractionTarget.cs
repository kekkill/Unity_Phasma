// Eligibility and a successful state change are distinct from an attempted interaction.
public interface IGhostInteractionTarget : IGhostInteractable
{
    bool CanGhostInteract(GhostInteractionContext context);
    bool TryGhostInteract(GhostInteractionContext context);
}
