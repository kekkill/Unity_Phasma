using System;
using UnityEngine;

public enum PlayerLifeStatus { Alive, Dead }

// Gameplay state only. A future network authority can replicate this transition.
public class PlayerLifeState : MonoBehaviour
{
    [SerializeField] private GameSession session;
    [SerializeField] private PlayerLifeStatus state;
    public PlayerLifeStatus State => state;
    public bool IsAlive => state == PlayerLifeStatus.Alive;
    public GameSession Session => session;
    public event Action<PlayerLifeState> Died;
    public event Action<PlayerLifeState> SessionChanged;

    private void OnEnable()
    {
        if (session != null) session.Register(this);
    }

    private void OnDisable()
    {
        if (session != null) session.Unregister(this);
    }

    public void BindSession(GameSession value)
    {
        if (session != null) session.Unregister(this);
        session = value;
        if (isActiveAndEnabled && session != null) session.Register(this);
        SessionChanged?.Invoke(this);
    }

    public bool TryDie()
    {
        if (!IsAlive || (session != null && session.HasEnded)) return false;
        state = PlayerLifeStatus.Dead;
        Debug.Log("Player died: " + name, this);
        Died?.Invoke(this);
        // Presentation subscribers finish before the round-end event is raised.
        if (session != null) session.CheckAllPlayersDead();
        return true;
    }
}
