using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-150)]
public class GameSession : MonoBehaviour
{
    [SerializeField] private GhostActor[] ghosts = new GhostActor[0];
    private List<PlayerLifeState> participants;
    public bool HasEnded { get; private set; }
    public event Action AllPlayersDead;
    public int ParticipantCount => participants != null ? participants.Count : 0;

    public void Register(PlayerLifeState player)
    {
        if (participants == null) participants = new List<PlayerLifeState>();
        if (!HasEnded && player != null && !participants.Contains(player)) participants.Add(player);
    }

    public void Unregister(PlayerLifeState player)
    {
        if (participants != null) participants.Remove(player);
        // Disconnection/despawn is not automatically a death. Future networking defines that policy.
    }

    public IEnumerable<GhostPerceptionTarget> LivingTargets()
    {
        if (participants == null || HasEnded) yield break;
        foreach (PlayerLifeState player in participants)
        {
            if (player == null || !player.IsAlive) continue;
            GhostPerceptionTarget target = player.GetComponent<GhostPerceptionTarget>();
            if (target != null && target.isActiveAndEnabled) yield return target;
        }
    }

    public void CheckAllPlayersDead()
    {
        if (HasEnded || participants == null || participants.Count == 0) return;
        int currentParticipants = 0;
        foreach (PlayerLifeState player in participants)
        {
            if (player == null) continue;
            currentParticipants++;
            if (player.IsAlive) return;
        }
        if (currentParticipants == 0) return;
        HasEnded = true;
        foreach (GhostActor ghost in ghosts)
        {
            if (ghost == null) continue;
            GhostHuntController hunt = ghost.GetComponent<GhostHuntController>();
            if (hunt != null) hunt.ForceEndHunt();
            GhostNavigation navigation = ghost.GetComponent<GhostNavigation>();
            if (navigation != null) navigation.Stop();
            ghost.enabled = false;
        }
        Debug.Log("Round ended: all participants are dead", this);
        AllPlayersDead?.Invoke();
    }
}
