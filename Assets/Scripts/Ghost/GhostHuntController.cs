using System;
using UnityEngine;

public enum HuntPhase { Inactive, Warning, Active }

[RequireComponent(typeof(GhostNavigation), typeof(GhostRoomSearch), typeof(GhostAttack))]
[RequireComponent(typeof(GhostWorldInteraction))]
[RequireComponent(typeof(GhostHidingInvestigation))]
public class GhostHuntController : MonoBehaviour
{
    private enum HuntMode
    {
        Chase,
        Search
    }

    [Header("Debug")]
    [SerializeField] private bool isHunting;
    [SerializeField] private HuntMode currentMode;
    [SerializeField] private HuntSequenceProfile sequenceProfile;
    [SerializeField] private HuntPhase phase;
    private float warningEndTime;
    public HuntPhase Phase => isHunting ? phase : HuntPhase.Inactive;
    public HuntSequenceProfile SequenceProfile => sequenceProfile;
    public event Action<bool> HuntStateChanged;

    [SerializeField] private Vector3 searchCenter;
    [SerializeField] private Vector3 searchTargetPosition;

    private GhostController ghostController;
    private GhostPerception ghostPerception;
    private GhostAggression ghostAggression;
    private GhostActionRestrictions actionRestrictions;

    private GhostAttack attack;
    private GhostNavigation navigation;
    private GhostRoomSearch roomSearch;
    private bool wasHearingTarget;
    private bool searchingBlockedTarget;
    private bool approachingHidingPlace;
    [SerializeField] private float repelledUntil;
    private float nextHuntCheckTime;
    private float huntEndTime;
    private float searchEndTime;

    public bool IsHunting =>
        isHunting;

    private void Awake()
    {
        attack = GetComponent<GhostAttack>();
        navigation = GetComponent<GhostNavigation>();
        roomSearch = GetComponent<GhostRoomSearch>();
        ghostController =
            GetComponent<GhostController>();

        ghostPerception =
            GetComponent<GhostPerception>();

        ghostAggression =
            GetComponent<GhostAggression>();

        actionRestrictions =
            GetComponent<GhostActionRestrictions>();
    }

    public void Initialize()
    {
        ScheduleNextHuntCheck();
    }

    public bool ShouldStartHunt()
    {
        if (!navigation.IsReady || Time.time < repelledUntil) return false;

        if (isHunting)
        {
            return false;
        }

        if (Time.time <
            nextHuntCheckTime)
        {
            return false;
        }

        ScheduleNextHuntCheck();

        if (ghostController == null ||
            ghostController.Profile == null ||
            ghostPerception == null ||
            ghostAggression == null)
        {
            return false;
        }

        if (!ghostPerception.HasKnownTargetPosition)
        {
            return false;
        }

        if (IsActionBlocked(
            GhostActionType.Hunt))
        {
            Debug.Log(
                "Ghost Hunt is currently blocked."
            );

            return false;
        }

        return
            ghostAggression.RollForHunt();
    }

    public bool BeginHunt()
    {
        if (isHunting || !isActiveAndEnabled || !navigation.IsReady || Time.time < repelledUntil) return false;
        if (ghostController == null ||
            ghostController.Profile == null ||
            ghostPerception == null ||
            ghostAggression == null)
        {
            return false;
        }

        if (!ghostPerception.HasKnownTargetPosition)
        {
            return false;
        }

        isHunting =
            true;

        float warning = sequenceProfile != null ? sequenceProfile.WarningDuration * ghostController.HuntWarningMultiplier : 0f;
        warningEndTime = Time.time + warning;
        phase = warning > 0f ? HuntPhase.Warning : HuntPhase.Active;

        huntEndTime =
            warningEndTime +
            ghostController.HuntDuration;

        ghostAggression.BeginHunt();
        navigation.Stop();
        HuntStateChanged?.Invoke(true);
        GetComponent<GhostWorldInteraction>().ResetHuntTimer();
        GetComponent<GhostActivityEmitter>()?.RefreshHuntSignal();

        if (phase == HuntPhase.Warning)
        {
            Debug.Log("Ghost Hunt -> Warning (entrance locked, attacks delayed)", this);
            return true;
        }
        StartPursuit();
        return true;
    }

    private void StartPursuit()
    {
        phase = HuntPhase.Active;

        if (ghostPerception.CanSeeTarget)
        {
            EnterChase();
        }
        else
        {
            EnterSearch(
                ghostPerception.LastKnownPosition
            );
        }

        Debug.Log(
            "Ghost -> HUNT"
        );

    }

    public bool UpdateHunt()
    {
        if (!isHunting)
        {
            return false;
        }

        if (phase == HuntPhase.Warning)
        {
            if (Time.time < warningEndTime) return true;
            StartPursuit();
        }

        bool killedPlayer = attack.TryAttack(ghostPerception.Target);
        if (attack.WasRepelled)
        {
            repelledUntil = Time.time + ghostController.Profile.CrossRepelDuration;
            FinishHunt();
            Debug.Log("Ghost repelled by cross", this);
            return false;
        }
        if (killedPlayer)
        {
            ghostPerception.RefreshTarget();
            // Ending the round may already have stopped this hunt via GameSession.
            if (!isHunting) return false;
            if (ghostPerception.Target == null)
            {
                FinishHunt();
                return false;
            }
        }

        if (Time.time >= huntEndTime)
        {
            FinishHunt();

            return false;
        }

        GetComponent<GhostWorldInteraction>().TickHunt();
        HidingZone hidingPlace = ghostPerception.HasKnownTargetPosition ? ghostPerception.LastKnownHidingZone : null;
        if (GetComponent<GhostHidingInvestigation>().TryApproach(hidingPlace, ghostController.HuntMoveSpeed))
        {
            approachingHidingPlace = true;
            return true;
        }
        if (approachingHidingPlace)
        {
            approachingHidingPlace = false;
            EnterSearch(hidingPlace != null ? hidingPlace.InvestigationPosition : ghostPerception.LastKnownPosition);
        }

        // Видит игрока:
        // можно преследовать его точную текущую позицию.
        if (ghostPerception.CanSeeTarget)
        {
            Vector3 observedPosition = ghostPerception.Target.transform.position;
            if (navigation.TryReachablePoint(observedPosition, out _))
            {
                if (currentMode != HuntMode.Chase) EnterChase();
                searchingBlockedTarget = false;
                UpdateChase();
            }
            else UpdateBlockedTargetSearch();

            return true;
        }

        // Не видит, но слышит:
        // идём не за Player напрямую,
        // а к последней услышанной позиции.
        if (ghostPerception.CanHearTarget)
        {
            UpdateHeardTarget();

            return true;
        }

        // Resume room search at the last observed position after hearing/vision is lost.
        if (currentMode == HuntMode.Chase || wasHearingTarget)
        {
            EnterSearch(
                ghostPerception.LastKnownPosition
            );
        }

        wasHearingTarget = false;
        UpdateSearch();

        return
            isHunting;
    }

    public void ForceEndHunt()
    {
        if (!isHunting)
        {
            return;
        }

        FinishHunt();
    }

    private void EnterChase()
    {
        searchingBlockedTarget = false;
        wasHearingTarget = false;
        if (roomSearch != null) roomSearch.Stop();
        currentMode =
            HuntMode.Chase;

        Debug.Log(
            "Ghost Hunt -> Chase"
        );
    }

    private void UpdateChase()
    {
        if (ghostPerception.Target == null)
        {
            EnterSearch(
                ghostPerception.LastKnownPosition
            );

            return;
        }

        Vector3 chasePosition =
            ghostPerception.Target
                .transform.position;

        chasePosition.y =
            transform.position.y;

        navigation.MoveTo(chasePosition, ghostController.HuntMoveSpeed);
    }

    private void UpdateHeardTarget()
    {
        wasHearingTarget = true;
        if (currentMode != HuntMode.Search)
        {
            currentMode =
                HuntMode.Search;

            Debug.Log(
                "Ghost Hunt -> Investigating sound"
            );
        }

        searchCenter =
            ghostPerception.LastKnownHidingZone != null ? ghostPerception.LastKnownHidingZone.InvestigationPosition :
            ghostPerception.LastKnownPosition;

        searchCenter.y =
            transform.position.y;

        searchTargetPosition =
            searchCenter;

        // Пока Ghost продолжает слышать игрока,
        // срок последующего поиска обновляется.
        searchEndTime =
            Time.time +
            ghostController.Profile.SearchDuration;

        if (navigation.TryReachablePoint(searchTargetPosition, out _))
        {
            searchingBlockedTarget = false;
            navigation.MoveTo(searchTargetPosition, ghostController.HuntMoveSpeed);
        }
        else UpdateBlockedTargetSearch();
    }

    private void UpdateBlockedTargetSearch()
    {
        if (!searchingBlockedTarget)
        {
            searchingBlockedTarget = true;
            currentMode = HuntMode.Search;
            roomSearch.Begin(transform.position,
                ghostController.FindRoomAt(transform.position), ghostController.HuntMoveSpeed);
            Debug.Log("Ghost Hunt: target unreachable, searching accessible rooms", this);
        }
        roomSearch.Tick(ghostController.HuntMoveSpeed);
    }

    private void EnterSearch(
        Vector3 lostPosition)
    {
        searchingBlockedTarget = false;
        currentMode =
            HuntMode.Search;

        searchCenter =
            lostPosition;

        searchCenter.y =
            transform.position.y;

        searchTargetPosition =
            searchCenter;

        searchEndTime =
            Time.time +
            ghostController.Profile.SearchDuration;

        roomSearch.Begin(searchCenter, ghostPerception.LastKnownRoom,
            ghostController.HuntMoveSpeed);

        Debug.Log(
            "Ghost Hunt -> Search"
        );
    }

    private void UpdateSearch()
    {
        if (Time.time >= searchEndTime)
        {
            FinishHunt();

            return;
        }

        roomSearch.Tick(ghostController.HuntMoveSpeed);
    }

    private void FinishHunt()
    {
        approachingHidingPlace = false;
        if (roomSearch != null) roomSearch.Stop();
        searchingBlockedTarget = false;
        wasHearingTarget = false;
        isHunting =
            false;
        phase = HuntPhase.Inactive;
        GetComponent<GhostActivityEmitter>()?.StopHuntSignal();

        if (ghostAggression != null) ghostAggression.EndHuntAndStartRecovery();

        ScheduleNextHuntCheck();
        HuntStateChanged?.Invoke(false);

        Debug.Log(
            "Ghost Hunt ended."
        );
    }

    private void OnDisable() { if (isHunting) FinishHunt(); }

    private void ScheduleNextHuntCheck()
    {
        if (ghostController == null ||
            ghostController.Profile == null)
        {
            return;
        }

        float minimum =
            Mathf.Min(
                ghostController.MinHuntCheckInterval,
                ghostController.MaxHuntCheckInterval
            );

        float maximum =
            Mathf.Max(
                ghostController.MinHuntCheckInterval,
                ghostController.MaxHuntCheckInterval
            );

        float delay =
            UnityEngine.Random.Range(
                minimum,
                maximum
            );

        nextHuntCheckTime =
            Time.time + delay;
    }

    private bool IsActionBlocked(
        GhostActionType actionType)
    {
        if (actionRestrictions == null)
        {
            return false;
        }

        return
            actionRestrictions.IsActionBlocked(
                actionType
            );
    }

}
