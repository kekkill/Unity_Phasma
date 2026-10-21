using UnityEngine;

[CreateAssetMenu(menuName = "Phasma/Mission Difficulty")]
public class MissionDifficultyProfile : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField, Min(0f)] private float initialAngerMultiplier = 1f;
    [SerializeField, Min(0.1f)] private float huntSpeedMultiplier = 1f;
    [SerializeField, Min(0.1f)] private float huntDurationMultiplier = 1f;
    [SerializeField, Min(0.1f)] private float huntCheckIntervalMultiplier = 1f;
    [SerializeField, Min(0f)] private float warningDurationMultiplier = 1f;
    public string DisplayName => displayName;
    public string Description => description;
    public float InitialAngerMultiplier => Mathf.Max(0f, initialAngerMultiplier);
    public float HuntSpeedMultiplier => Mathf.Max(0.1f, huntSpeedMultiplier);
    public float HuntDurationMultiplier => Mathf.Max(0.1f, huntDurationMultiplier);
    public float HuntCheckIntervalMultiplier => Mathf.Max(0.1f, huntCheckIntervalMultiplier);
    public float WarningDurationMultiplier => Mathf.Max(0f, warningDurationMultiplier);
}
