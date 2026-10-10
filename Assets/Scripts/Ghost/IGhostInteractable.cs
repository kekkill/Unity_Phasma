using UnityEngine;

public interface IGhostInteractable
{
    Vector3 GhostInteractionPosition { get; }

    void GhostInteract(
        GhostInteractionContext context
    );
}