using UnityEngine;

public enum GhostSaltResponse { AvoidUntilHunt, LeaveFootprints, Ignore }

[CreateAssetMenu(
    fileName = "GhostProfile",
    menuName = "Ghost/Ghost Profile"
)]
public class GhostProfile : ScriptableObject
{
    [SerializeField] private string ghostName = "Ghost";

    [Header("Defensive items")]
    [SerializeField] private bool ignoresCross;
    [SerializeField, Range(0f, 1f)] private float crossBreakChance = 0.15f;
    [SerializeField, Range(0f, 1f)] private float angerCrossBreakContribution = 0.5f;
    [SerializeField, Min(1f)] private float crossRepelDuration = 6f;
    [SerializeField] private GhostSaltResponse saltResponse;
    public bool IgnoresCross => ignoresCross;
    public float CrossRepelDuration => crossRepelDuration;
    public GhostSaltResponse SaltResponse => saltResponse;
    public float GetCrossBreakChance(float anger) => Mathf.Clamp01(crossBreakChance +
        Mathf.Clamp01(anger / 100f) * angerCrossBreakContribution);

    [Header("Evidence")]
    [SerializeField] private bool hasFreezingEvidence;
    [SerializeField] private bool hasEmfEvidence;
    [SerializeField] private bool hasUltravioletEvidence;
    [SerializeField] private bool hasWritingEvidence;

    [Header("Perception")]
    [SerializeField] private float visionRange = 10f;

    [Range(0f, 180f)]
    [SerializeField] private float visionAngle = 90f;

    [SerializeField] private float hearingRange = 8f;

    [SerializeField] private float targetMemoryDuration = 15f;

    [Header("Aggression")]
    [Range(0f, 100f)]
    [SerializeField] private float initialAnger = 0f;

    [SerializeField] private float visionAngerGainPerSecond = 8f;
    [SerializeField] private float hearingAngerGainPerSecond = 4f;
    [SerializeField] private float calmDownPerSecond = 1f;

    [Header("Hunt Probability")]
    [Range(0f, 1f)]
    [SerializeField] private float minimumHuntChance = 0.01f;

    [Range(0f, 1f)]
    [SerializeField] private float maximumHuntChance = 0.75f;

    [SerializeField] private float huntChancePower = 2f;

    [SerializeField] private float minHuntCheckInterval = 4f;
    [SerializeField] private float maxHuntCheckInterval = 8f;

    [Header("Hunt Behaviour")]
    [SerializeField] private float huntMoveSpeed = 3f;
    [SerializeField] private float maxHuntDuration = 25f;
    [SerializeField] private float searchDuration = 10f;
    [SerializeField] private float searchRadius = 4f;

    [Range(0f, 100f)]
    [SerializeField] private float angerReductionAfterHunt = 30f;

    [SerializeField] private float postHuntRecoveryDuration = 8f;

    public string GhostName =>
        ghostName;

    public bool HasFreezingEvidence =>
        hasFreezingEvidence;

    public bool HasEmfEvidence =>
        hasEmfEvidence;

    public bool HasUltravioletEvidence =>
        hasUltravioletEvidence;

    public bool HasWritingEvidence =>
        hasWritingEvidence;

    public float VisionRange =>
        visionRange;

    public float VisionAngle =>
        visionAngle;

    public float HearingRange =>
        hearingRange;

    public float TargetMemoryDuration =>
        targetMemoryDuration;

    public float InitialAnger =>
        initialAnger;

    public float VisionAngerGainPerSecond =>
        visionAngerGainPerSecond;

    public float HearingAngerGainPerSecond =>
        hearingAngerGainPerSecond;

    public float CalmDownPerSecond =>
        calmDownPerSecond;

    public float MinimumHuntChance =>
        minimumHuntChance;

    public float MaximumHuntChance =>
        maximumHuntChance;

    public float HuntChancePower =>
        huntChancePower;

    public float MinHuntCheckInterval =>
        minHuntCheckInterval;

    public float MaxHuntCheckInterval =>
        maxHuntCheckInterval;

    public float HuntMoveSpeed =>
        huntMoveSpeed;

    public float MaxHuntDuration =>
        maxHuntDuration;

    public float SearchDuration =>
        searchDuration;

    public float SearchRadius =>
        searchRadius;

    public float AngerReductionAfterHunt =>
        angerReductionAfterHunt;

    public float PostHuntRecoveryDuration =>
        postHuntRecoveryDuration;
}