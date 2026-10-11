using UnityEngine;

[RequireComponent(typeof(GhostNavigation), typeof(GhostWorldInteraction))]
public class GhostActor : MonoBehaviour
{
    private enum GhostState
    {
        Idle,
        Roaming,
        Interaction,
        Hunt
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;

    [Header("Idle")]
    [SerializeField] private float minIdleTime = 2f;
    [SerializeField] private float maxIdleTime = 5f;

    [Header("Interaction")]
    [SerializeField] private float interactionDuration = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float interactionChance = 0.3f;

    [SerializeField] private float interactionRadius = 2.5f;

    [Header("Debug")]
    [SerializeField] private GhostState currentState;

    private GhostController ghostController;
    private GhostActionRestrictions actionRestrictions;
    private GhostHuntController huntController;

    private GhostNavigation navigation;
    private Room ghostRoom;

    private float stateEndTime;
    private bool plannedInteraction;

    public bool IsHunting =>
        currentState ==
        GhostState.Hunt;

    private void Awake()
    {
        navigation = GetComponent<GhostNavigation>();
        ghostController =
            GetComponent<GhostController>();

        actionRestrictions =
            GetComponent<GhostActionRestrictions>();

        huntController =
            GetComponent<GhostHuntController>();
    }

    public void Initialize(Room room)
    {
        ghostRoom =
            room;

        if (ghostRoom == null)
        {
            Debug.LogError(
                "GhostActor: ghost room is null."
            );

            return;
        }

        if (!navigation.Initialize(ghostRoom))
        {
            enabled = false;
            return;
        }

        if (huntController != null)
        {
            huntController.Initialize();
        }

        EnterIdle();
    }

    private void Update()
    {
        if (ghostRoom == null)
        {
            return;
        }

        TryStartHunt();

        switch (currentState)
        {
            case GhostState.Idle:
                UpdateIdle();
                break;

            case GhostState.Roaming:
                UpdateRoaming();
                break;

            case GhostState.Interaction:
                UpdateInteraction();
                break;

            case GhostState.Hunt:
                UpdateHunt();
                break;
        }
    }

    private void TryStartHunt()
    {
        if (currentState ==
            GhostState.Hunt)
        {
            return;
        }

        if (currentState ==
            GhostState.Interaction)
        {
            return;
        }

        if (huntController == null)
        {
            return;
        }

        if (!huntController
            .ShouldStartHunt())
        {
            return;
        }

        if (!huntController
            .BeginHunt())
        {
            return;
        }

        currentState =
            GhostState.Hunt;
        plannedInteraction = false;
    }

    private void UpdateHunt()
    {
        if (huntController == null)
        {
            EnterIdle();

            return;
        }

        bool huntContinues =
            huntController.UpdateHunt();

        if (!huntContinues)
        {
            EnterIdle();
        }
    }

    private void EnterIdle()
    {
        plannedInteraction = false;
        navigation.Stop();
        currentState =
            GhostState.Idle;

        float idleTime =
            Random.Range(
                minIdleTime,
                maxIdleTime
            );

        stateEndTime =
            Time.time + idleTime * GhostActivityRules.IdleMultiplier(ghostController.Profile, ghostController.CurrentAnger);

        Debug.Log(
            "Ghost -> Idle"
        );
    }

    private void UpdateIdle()
    {
        if (Time.time >=
            stateEndTime)
        {
            EnterRoaming();
        }
    }

    private void EnterRoaming()
    {
        currentState =
            GhostState.Roaming;

        bool outsideHome = !ghostRoom.ContainsPoint(transform.position);
        Vector3 interactionPoint = default;
        plannedInteraction = !outsideHome && Random.value <=
            GhostActivityRules.InteractionChance(interactionChance, ghostController.CurrentAnger) &&
            GetComponent<GhostWorldInteraction>().TryFindInteractionPoint(ghostRoom, interactionRadius, out interactionPoint);
        bool assigned = plannedInteraction && navigation.MoveWithinRoom(ghostRoom, interactionPoint, moveSpeed);
        if (!assigned) plannedInteraction = false;
        if (!assigned) assigned = navigation.TryRoomDestination(ghostRoom, moveSpeed, outsideHome);
        if (!assigned && outsideHome)
        {
            // A closed door must not freeze normal behavior after the hunt either.
            Room currentRoom = ghostController.FindRoomAt(transform.position);
            assigned = navigation.TryRoomDestination(currentRoom, moveSpeed);
            if (assigned) Debug.Log("Ghost: home room unreachable, roaming current room", this);
        }
        if (!assigned)
        {
            EnterIdle();
            return;
        }

        Debug.Log(
            "Ghost -> Roaming"
        );
    }

    private void UpdateRoaming()
    {
        if (navigation.HasFailedPath)
        {
            EnterIdle();
            return;
        }
        if (navigation.HasStalled) { EnterIdle(); return; }
        if (navigation.HasArrived)
        {
            if (plannedInteraction) TryEnterInteraction();
            else DecideNextAction();
        }
    }

    private void DecideNextAction()
    {
        float randomValue =
            Random.value;

        if (randomValue <=
            GhostActivityRules.InteractionChance(interactionChance, ghostController.CurrentAnger))
        {
            TryEnterInteraction();
        }
        else
        {
            EnterIdle();
        }
    }

    private void TryEnterInteraction()
    {
        if (IsActionBlocked(
            GhostActionType.Interaction))
        {
            Debug.Log(
                "Ghost Interaction is currently blocked."
            );

            EnterIdle();

            return;
        }

        EnterInteraction();
    }

    private void EnterInteraction()
    {
        navigation.Stop();
        currentState =
            GhostState.Interaction;

        bool completed = TryInteractWithNearbyObject();

        stateEndTime =
            Time.time +
            interactionDuration;

        Debug.Log(
            completed ? "Ghost -> Interaction: completed" : "Ghost -> Interaction: no accessible target"
        );
    }

    private void UpdateInteraction()
    {
        if (Time.time >=
            stateEndTime)
        {
            EnterIdle();
        }
    }

    private bool IsActionBlocked(
        GhostActionType actionType)
    {
        if (actionRestrictions == null)
        {
            return false;
        }

        return
            actionRestrictions
                .IsActionBlocked(
                    actionType
                );
    }

    private bool TryInteractWithNearbyObject() => GetComponent<GhostWorldInteraction>().TryInteractNearby(interactionRadius);
    private void OnDrawGizmos() => Gizmos.DrawWireSphere(transform.position, 0.3f);
    private void OnDrawGizmosSelected() => Gizmos.DrawWireSphere(transform.position, interactionRadius);
}
