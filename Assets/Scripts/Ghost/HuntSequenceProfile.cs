using UnityEngine;

[CreateAssetMenu(menuName = "Phasma/Hunt Sequence", fileName = "HuntSequence")]
public class HuntSequenceProfile : ScriptableObject
{
    [SerializeField, Min(0f)] private float warningDuration = 2f;
    [SerializeField, Min(0.1f)] private float flickerInterval = 0.18f;
    public float WarningDuration => Mathf.Max(0f, warningDuration);
    public float FlickerInterval => Mathf.Max(0.1f, flickerInterval);
}
