using UnityEngine;

[RequireComponent(typeof(GhostNavigation), typeof(GhostRoomSearch), typeof(GhostAttack))]
[RequireComponent(typeof(GhostWorldInteraction))]
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
        if (!navigation.IsReady || Time.time < repelledUntil) return false;
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

        huntEndTime =
            Time.time +
            ghostController.Profile.MaxHuntDuration;

        ghostAggression.BeginHunt();
        GetComponent<GhostWorldInteraction>().ResetHuntTimer();
        GetComponent<GhostActivityEmitter>()?.RefreshHuntSignal();

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

        return true;
    }

    public bool UpdateHunt()
    {
        if (!isHunting)
        {
            return false;
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
        roomSearch.Stop();
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

        navigation.MoveTo(chasePosition, ghostController.Profile.HuntMoveSpeed);
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
            navigation.MoveTo(searchTargetPosition, ghostController.Profile.HuntMoveSpeed);
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
                ghostController.FindRoomAt(transform.position), ghostController.Profile.HuntMoveSpeed);
            Debug.Log("Ghost Hunt: target unreachable, searching accessible rooms", this);
        }
        roomSearch.Tick(ghostController.Profile.HuntMoveSpeed);
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
            ghostController.Profile.HuntMoveSpeed);

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

        roomSearch.Tick(ghostController.Profile.HuntMoveSpeed);
    }

    private void FinishHunt()
    {
        roomSearch.Stop();
        searchingBlockedTarget = false;
        wasHearingTarget = false;
        isHunting =
            false;
        GetComponent<GhostActivityEmitter>()?.StopHuntSignal();

        ghostAggression
            .EndHuntAndStartRecovery();

        ScheduleNextHuntCheck();

        Debug.Log(
            "Ghost Hunt ended."
        );
    }

    private void ScheduleNextHuntCheck()
    {
        if (ghostController == null ||
            ghostController.Profile == null)
        {
            return;
        }

        float minimum =
            Mathf.Min(
                ghostController.Profile.MinHuntCheckInterval,
                ghostController.Profile.MaxHuntCheckInterval
            );

        float maximum =
            Mathf.Max(
                ghostController.Profile.MinHuntCheckInterval,
                ghostController.Profile.MaxHuntCheckInterval
            );

        float delay =
            Random.Range(
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
