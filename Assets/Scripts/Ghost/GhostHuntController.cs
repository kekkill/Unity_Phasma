using UnityEngine;

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

    private float nextHuntCheckTime;
    private float huntEndTime;
    private float searchEndTime;

    public bool IsHunting =>
        isHunting;

    private void Awake()
    {
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

        if (Time.time >= huntEndTime)
        {
            FinishHunt();

            return false;
        }

        // Видит игрока:
        // можно преследовать его точную текущую позицию.
        if (ghostPerception.CanSeeTarget)
        {
            if (currentMode != HuntMode.Chase)
            {
                EnterChase();
            }

            UpdateChase();

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

        // Потерял и зрение, и слух.
        if (currentMode == HuntMode.Chase)
        {
            EnterSearch(
                ghostPerception.LastKnownPosition
            );
        }

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

        MoveTowards(
            chasePosition,
            ghostController.Profile.HuntMoveSpeed
        );
    }

    private void UpdateHeardTarget()
    {
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

        MoveTowards(
            searchTargetPosition,
            ghostController.Profile.HuntMoveSpeed
        );
    }

    private void EnterSearch(
        Vector3 lostPosition)
    {
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

        MoveTowards(
            searchTargetPosition,
            ghostController.Profile.HuntMoveSpeed
        );

        float distance =
            Vector3.Distance(
                transform.position,
                searchTargetPosition
            );

        if (distance <= 0.1f)
        {
            ChooseNextSearchPoint();
        }
    }

    private void ChooseNextSearchPoint()
    {
        Vector2 randomCircle =
            Random.insideUnitCircle *
            ghostController.Profile.SearchRadius;

        searchTargetPosition =
            searchCenter +
            new Vector3(
                randomCircle.x,
                0f,
                randomCircle.y
            );

        Debug.Log(
            "Ghost chose another search point."
        );
    }

    private void FinishHunt()
    {
        isHunting =
            false;

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

    private void MoveTowards(
        Vector3 position,
        float speed)
    {
        TurnTowards(
            position
        );

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                position,
                speed * Time.deltaTime
            );
    }

    private void TurnTowards(
        Vector3 position)
    {
        Vector3 direction =
            position -
            transform.position;

        direction.y =
            0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        transform.rotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );
    }
}