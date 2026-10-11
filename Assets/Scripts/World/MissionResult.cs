using System.Collections.Generic;

public enum MissionPhase { Preparation, Investigation, Ended }
public enum MissionEndReason { Extracted, AllPlayersDead }

// Immutable snapshots: later journal edits cannot change a completed result.
public sealed class ParticipantMissionResult
{
    public string PlayerName { get; }
    public bool Survived { get; }
    public string SelectedGhostId { get; }
    public string SelectedGhostName { get; }
    public bool Correct { get; }
    public ParticipantMissionResult(string playerName, bool survived, string selectedId, string selectedName, bool correct)
    {
        PlayerName = playerName; Survived = survived; SelectedGhostId = selectedId;
        SelectedGhostName = selectedName; Correct = correct;
    }
}
public sealed class MissionResult
{
    public MissionEndReason Reason { get; }
    public string ActualGhostId { get; }
    public string ActualGhostName { get; }
    public IReadOnlyList<ParticipantMissionResult> Participants { get; }
    public bool TeamSuccess { get; }
    public MissionResult(MissionEndReason reason, string actualId, string actualName, List<ParticipantMissionResult> participants)
    {
        Reason = reason; ActualGhostId = actualId; ActualGhostName = actualName;
        Participants = new List<ParticipantMissionResult>(participants).AsReadOnly();
        bool survivor = false, correct = true;
        foreach (ParticipantMissionResult player in Participants)
            if (player.Survived) { survivor = true; correct &= player.Correct; }
        TeamSuccess = reason == MissionEndReason.Extracted && survivor && correct;
    }
}
