using UnityEngine;

public enum SoundCue { PlayerStep, GhostStep, GhostLoop, HuntStart, HuntEnd, DoorOpen, DoorClose, Throw, Impact }

[CreateAssetMenu(menuName = "Phasma/Audio Profile", fileName = "PrototypeAudio")]
public class PrototypeAudioProfile : ScriptableObject
{
    [SerializeField] private AudioClip playerStep, ghostStep, ghostLoop, huntStart, huntEnd, doorOpen, doorClose, itemThrow, impact;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float ghostVoiceVolume = 0.45f;
    [SerializeField, Min(0.3f)] private float playerStride = 1.4f;
    [SerializeField, Min(0.3f)] private float ghostStride = 0.95f;
    [SerializeField, Min(1)] private int maxOneShotVoices = 12;
    [SerializeField, Min(0.1f)] private float minimumDistance = 1.2f;
    [SerializeField, Min(1f)] private float maximumDistance = 18f;
    public float MasterVolume => Mathf.Clamp01(masterVolume);
    public float GhostVoiceVolume => Mathf.Clamp01(ghostVoiceVolume);
    public float PlayerStride => Mathf.Max(0.3f, playerStride);
    public float GhostStride => Mathf.Max(0.3f, ghostStride);
    public int MaxOneShotVoices => Mathf.Clamp(maxOneShotVoices, 1, 24);
    public float MinimumDistance => Mathf.Max(0.1f, minimumDistance);
    public float MaximumDistance => Mathf.Max(MinimumDistance + 0.1f, maximumDistance);
    public AudioClip Clip(SoundCue cue) => cue switch
    {
        SoundCue.PlayerStep => playerStep, SoundCue.GhostStep => ghostStep, SoundCue.GhostLoop => ghostLoop,
        SoundCue.HuntStart => huntStart, SoundCue.HuntEnd => huntEnd,
        SoundCue.DoorOpen => doorOpen, SoundCue.DoorClose => doorClose, SoundCue.Throw => itemThrow,
        _ => impact
    };
}
