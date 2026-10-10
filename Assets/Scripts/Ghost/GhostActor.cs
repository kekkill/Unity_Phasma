using System.Collections.Generic;
using UnityEngine;

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

    private Room ghostRoom;

    private Vector3 targetPosition;
    private float stateEndTime;

    public bool IsHunting =>
        currentState ==
        GhostState.Hunt;

    private void Awake()
    {
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

        Vector3 startPosition =
            ghostRoom.GetRandomPoint();

        startPosition.y =
            transform.position.y;

        transform.position =
            startPosition;

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
        currentState =
            GhostState.Idle;

        float idleTime =
            Random.Range(
                minIdleTime,
                maxIdleTime
            );

        stateEndTime =
            Time.time + idleTime;

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

        targetPosition =
            ghostRoom.GetRandomPoint();

        targetPosition.y =
            transform.position.y;

        Debug.Log(
            "Ghost -> Roaming"
        );
    }

    private void UpdateRoaming()
    {
        MoveTowards(
            targetPosition,
            moveSpeed
        );

        float distance =
            Vector3.Distance(
                transform.position,
                targetPosition
            );

        if (distance < 0.05f)
        {
            DecideNextAction();
        }
    }

    private void DecideNextAction()
    {
        float randomValue =
            Random.value;

        if (randomValue <=
            interactionChance)
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
        currentState =
            GhostState.Interaction;

        TryInteractWithNearbyObject();

        stateEndTime =
            Time.time +
            interactionDuration;

        Debug.Log(
            "Ghost -> Interaction"
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

        if (direction.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        transform.rotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );
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

    private void TryInteractWithNearbyObject()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                interactionRadius,
                ~0,
                QueryTriggerInteraction.Ignore
            );

        List<IGhostInteractable> possibleTargets =
            new List<IGhostInteractable>();

        foreach (Collider hit in hits)
        {
            MonoBehaviour[] behaviours =
                hit.GetComponentsInParent
                    <MonoBehaviour>();

            foreach (
                MonoBehaviour behaviour
                in behaviours)
            {
                if (behaviour is not
                    IGhostInteractable
                    ghostInteractable)
                {
                    continue;
                }

                if (!possibleTargets.Contains(
                    ghostInteractable))
                {
                    possibleTargets.Add(
                        ghostInteractable
                    );
                }
            }
        }

        if (possibleTargets.Count == 0)
        {
            Debug.Log(
                "Ghost found no nearby object to interact with."
            );

            return;
        }

        int randomIndex =
            Random.Range(
                0,
                possibleTargets.Count
            );

        IGhostInteractable target =
            possibleTargets[randomIndex];

        GhostInteractionContext context =
            new GhostInteractionContext(
                this,
                ghostController,
                ghostController != null
                    ? ghostController.Profile
                    : null
            );

        target.GhostInteract(
            context
        );

        if (ghostController != null)
        {
            ghostController
                .CreateInteractionEvidenceAt(
                    target
                        .GhostInteractionPosition
                );
        }

        Debug.Log(
            "Ghost interacted with an object."
        );
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            0.3f
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            interactionRadius
        );
    }
}