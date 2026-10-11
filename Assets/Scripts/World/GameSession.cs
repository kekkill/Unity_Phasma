using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-150)]
public class GameSession : MonoBehaviour
{
    [SerializeField] private GhostActor[] ghosts = new GhostActor[0];
    private List<PlayerLifeState> participants;
    private JournalCatalog missionCatalog;
    private JournalGhostEntry actualGhost;
    private SafeZone extractionZone;
    public MissionPhase Phase { get; private set; }
    public bool HasEnded => Phase == MissionPhase.Ended;
    public MissionResult Result { get; private set; }
    public event Action AllPlayersDead;
    public event Action MissionEnded;
    public int ParticipantCount => participants != null ? participants.Count : 0;
    public IEnumerable<PlayerLifeState> Participants => participants != null ? participants.AsReadOnly() : (IEnumerable<PlayerLifeState>)Array.Empty<PlayerLifeState>();

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
    public bool ConfigureMission(GhostProfile profile, JournalCatalog catalog, SafeZone zone, out string reason)
    {
        if (HasEnded || missionCatalog != null) { reason = "Mission is already configured or ended."; return false; }
        if (catalog == null || !catalog.TryValidate(out _) || zone == null || profile == null)
        { reason = "Mission needs a valid catalog, ghost profile and extraction zone."; return false; }
        JournalGhostEntry entry = catalog.FindProfile(profile);
        if (entry == null) { reason = "The chosen ghost profile is missing from the mission catalog."; return false; }
        actualGhost = entry; missionCatalog = catalog; extractionZone = zone;
        reason = null; return true;
    }
    public bool BeginInvestigation(PlayerLifeState player)
    {
        if (Phase != MissionPhase.Preparation || missionCatalog == null || !IsParticipantAlive(player)) return false;
        Phase = MissionPhase.Investigation;
        return true;
    }
    private bool IsParticipantAlive(PlayerLifeState player) =>
        player != null && player.IsAlive && participants != null && participants.Contains(player);

    public bool TryExtract(PlayerLifeState requester, out string reason)
    {
        if (HasEnded) { reason = "Миссия уже завершена."; return false; }
        if (missionCatalog == null || actualGhost == null || extractionZone == null)
        { reason = "Миссия ещё не настроена."; return false; }
        if (!IsParticipantAlive(requester)) { reason = "Завершить расследование может только живой участник."; return false; }
        if (Phase != MissionPhase.Investigation) { reason = "Сначала войдите в дом и начните расследование."; return false; }
        foreach (PlayerLifeState player in participants)
        {
            if (player == null || !player.IsAlive) continue;
            if (!extractionZone.Contains(player.transform.position))
            { reason = "Все живые участники должны вернуться в безопасную зону."; return false; }
            JournalState notes = player.GetComponent<JournalState>();
            if (notes == null || notes.Catalog != missionCatalog || missionCatalog.Find(notes.SelectedGhostId) == null)
            { reason = "Каждый живой участник должен выбрать тип призрака в журнале (J)."; return false; }
        }
        reason = null;
        EndMission(MissionEndReason.Extracted);
        return true;
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
        if (currentParticipants != 0) EndMission(MissionEndReason.AllPlayersDead);
    }
    private void EndMission(MissionEndReason reason)
    {
        if (HasEnded) return;
        Phase = MissionPhase.Ended;
        var results = new List<ParticipantMissionResult>();
        foreach (PlayerLifeState player in Participants)
        {
            if (player == null) continue;
            JournalState notes = player.GetComponent<JournalState>();
            string selectedId = notes != null ? notes.SelectedGhostId : null;
            JournalGhostEntry selected = missionCatalog != null ? missionCatalog.Find(selectedId) : null;
            results.Add(new ParticipantMissionResult(player.name, player.IsAlive, selectedId,
                selected != null ? selected.DisplayName : "Не выбран", actualGhost != null && selectedId == actualGhost.Id));

        }
        Result = new MissionResult(reason, actualGhost != null ? actualGhost.Id : null,
            actualGhost != null ? actualGhost.DisplayName : "Неизвестен", results);
        foreach (GhostActor ghost in ghosts)
        {
            if (ghost == null) continue;
            GhostHuntController hunt = ghost.GetComponent<GhostHuntController>();
            if (hunt != null) hunt.ForceEndHunt();
            GhostNavigation navigation = ghost.GetComponent<GhostNavigation>();
            if (navigation != null) navigation.Stop();
            ghost.enabled = false;
            // Prevent perception/anger from restarting activity after the round.
            GhostPerception perception = ghost.GetComponent<GhostPerception>();
            if (perception != null) perception.enabled = false;
            GhostAggression aggression = ghost.GetComponent<GhostAggression>();
            if (aggression != null) aggression.enabled = false;
        }
        Debug.Log("Mission ended: " + reason, this);
        MissionEnded?.Invoke();
        if (reason == MissionEndReason.AllPlayersDead) AllPlayersDead?.Invoke();
    }
}
