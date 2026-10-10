using UnityEngine;

public class GhostAggression : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField]
    [Range(0f, 100f)]
    private float currentAnger;

    [SerializeField]
    [Range(0f, 1f)]
    private float currentHuntChance;

    [SerializeField] private bool huntActive;

    [SerializeField]
    private float recoveryAngerRemaining;

    private GhostController ghostController;
    private GhostPerception ghostPerception;

    private float recoveryReductionPerSecond;

    public float CurrentAnger =>
        currentAnger;

    public float CurrentHuntChance =>
        currentHuntChance;

    private void Awake()
    {
        ghostController =
            GetComponent<GhostController>();

        ghostPerception =
            GetComponent<GhostPerception>();
    }

    private void Start()
    {
        if (ghostController == null ||
            ghostController.Profile == null)
        {
            Debug.LogError(
                "GhostAggression: Ghost Profile is missing."
            );

            return;
        }

        currentAnger =
            ghostController.Profile.InitialAnger;

        UpdateHuntChance();
    }

    private void Update()
    {
        if (ghostController == null ||
            ghostController.Profile == null ||
            ghostPerception == null)
        {
            return;
        }

        if (!huntActive)
        {
            UpdateAnger();
            UpdatePostHuntRecovery();
        }

        UpdateHuntChance();
    }

    private void UpdateAnger()
    {
        GhostProfile profile =
            ghostController.Profile;

        if (ghostPerception.CanSeeTarget)
        {
            AddAnger(
                profile.VisionAngerGainPerSecond *
                Time.deltaTime
            );

            return;
        }

        if (ghostPerception.CanHearTarget)
        {
            AddAnger(
                profile.HearingAngerGainPerSecond *
                Time.deltaTime
            );

            return;
        }

        ReduceAnger(
            profile.CalmDownPerSecond *
            Time.deltaTime
        );
    }

    private void UpdatePostHuntRecovery()
    {
        if (recoveryAngerRemaining <= 0f)
        {
            return;
        }

        float reduction =
            recoveryReductionPerSecond *
            Time.deltaTime;

        reduction =
            Mathf.Min(
                reduction,
                recoveryAngerRemaining
            );

        ReduceAnger(
            reduction
        );

        recoveryAngerRemaining -=
            reduction;
    }

    private void UpdateHuntChance()
    {
        GhostProfile profile =
            ghostController.Profile;

        float normalizedAnger =
            currentAnger / 100f;

        float power =
            Mathf.Max(
                0.01f,
                profile.HuntChancePower
            );

        float shapedAnger =
            Mathf.Pow(
                normalizedAnger,
                power
            );

        currentHuntChance =
            Mathf.Lerp(
                profile.MinimumHuntChance,
                profile.MaximumHuntChance,
                shapedAnger
            );
    }

    public bool RollForHunt()
    {
        float randomValue =
            Random.value;

        return
            randomValue <= currentHuntChance;
    }

    public void BeginHunt()
    {
        huntActive =
            true;

        recoveryAngerRemaining =
            0f;
    }

    public void EndHuntAndStartRecovery()
    {
        huntActive =
            false;

        float totalReduction =
            ghostController.Profile
                .AngerReductionAfterHunt;

        float duration =
            Mathf.Max(
                0.1f,
                ghostController.Profile
                    .PostHuntRecoveryDuration
            );

        recoveryAngerRemaining =
            totalReduction;

        recoveryReductionPerSecond =
            totalReduction /
            duration;
    }

    public void AddAnger(float amount)
    {
        currentAnger =
            Mathf.Clamp(
                currentAnger + amount,
                0f,
                100f
            );
    }

    public void ReduceAnger(float amount)
    {
        currentAnger =
            Mathf.Clamp(
                currentAnger - amount,
                0f,
                100f
            );
    }
}