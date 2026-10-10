public readonly struct GhostInteractionContext
{
    public GhostActor Actor { get; }
    public GhostController Controller { get; }
    public GhostProfile Profile { get; }

    public GhostInteractionContext(
        GhostActor actor,
        GhostController controller,
        GhostProfile profile)
    {
        Actor = actor;
        Controller = controller;
        Profile = profile;
    }
}