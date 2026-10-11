using UnityEngine;

// Local input/menu cleanup is separate from the shared mission's gameplay decisions.
[RequireComponent(typeof(PlayerLifeState))]
public class PlayerMissionPresentation : MonoBehaviour
{
    private PlayerLifeState life;
    private GameSession session;
    private PlayerInputReader input;
    private PlayerMovement movement;
    private PlayerJournalController journal;
    private PlayerItemPlacement placement;
    private void Awake()
    {
        life = GetComponent<PlayerLifeState>();
        input = GetComponent<PlayerInputReader>();
        movement = GetComponent<PlayerMovement>();
        journal = GetComponent<PlayerJournalController>();
        placement = GetComponent<PlayerItemPlacement>();
    }
    private void OnEnable()
    {
        life.SessionChanged += RefreshBinding;
        RefreshBinding(life);
    }
    private void OnDisable()
    {
        if (life != null) life.SessionChanged -= RefreshBinding;
        if (session != null) session.MissionEnded -= OnMissionEnded;
        session = null;
        if (input != null) input.SetGameplayBlocked(this, false);
    }
    private void RefreshBinding(PlayerLifeState _)
    {
        if (session != null) session.MissionEnded -= OnMissionEnded;
        session = life.Session;
        if (input != null) input.SetGameplayBlocked(this, false);
        if (session == null) return;
        session.MissionEnded += OnMissionEnded;
        if (session.HasEnded) OnMissionEnded();
    }
    private void OnMissionEnded()
    {
        if (placement != null) placement.Cancel();
        if (journal != null) journal.Close();
        if (movement != null) movement.enabled = false;
        if (input != null) input.SetGameplayBlocked(this, true);
    }
}
