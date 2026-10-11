public readonly struct GhostInteractionContext
{
    public GhostActor Actor { get; }
    public GhostController Controller { get; }
    public GhostProfile Profile { get; }
    public bool IsHunting { get; }

    public GhostInteractionContext(
        GhostActor actor,
        GhostController controller,
        GhostProfile profile,
        bool isHunting = false)
    {
        Actor = actor;
        Controller = controller;
        Profile = profile;
        IsHunting = isHunting || (controller != null ? controller.IsHunting : actor != null && actor.IsHunting);
    }
}
