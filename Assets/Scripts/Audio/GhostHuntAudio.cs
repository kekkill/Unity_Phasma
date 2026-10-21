using UnityEngine;

public class GhostHuntAudio : MonoBehaviour
{
    [SerializeField] private GhostHuntController hunt;
    private AudioSource voice;
    private Vector3 previousPosition;
    private float travelled;
    private void OnEnable()
    {
        if (hunt == null) hunt = GetComponent<GhostHuntController>();
        if (hunt != null) { hunt.HuntStateChanged += OnHuntChanged; SynchronizeVoice(hunt.IsHunting); }
        previousPosition = transform.position; travelled = 0f;
    }
    private void SynchronizeVoice(bool hunting)
    {
        var world = WorldAudio.Instance;
        if (hunting && voice == null && world != null && world.Profile != null)
        {
            voice = world.CreateSource(transform, "Hunting ghost voice");
            voice.clip = world.Profile.Clip(SoundCue.GhostLoop); voice.loop = true; voice.priority = 80;
        }
        if (voice == null) return;
        if (!hunting) { voice.Stop(); return; }
        if (world != null && world.Profile != null) voice.volume = world.Profile.MasterVolume * world.Profile.GhostVoiceVolume;
        if (voice.clip != null && !voice.isPlaying) voice.Play();
    }
    private void OnHuntChanged(bool hunting)
    {
        SynchronizeVoice(hunting); previousPosition = transform.position; travelled = 0f;
        WorldAudio.Play(hunting ? SoundCue.HuntStart : SoundCue.HuntEnd, transform.position, 0.55f, false);
    }
    private void LateUpdate()
    {
        float moved = Vector3.ProjectOnPlane(transform.position - previousPosition, Vector3.up).magnitude;
        previousPosition = transform.position;
        bool hunting = hunt != null && hunt.IsHunting;
        SynchronizeVoice(hunting);
        var world = WorldAudio.Instance;
        if (!hunting || hunt.Phase != HuntPhase.Active || moved > 2.5f || world == null || world.Profile == null)
        { travelled = 0f; return; }
        travelled += moved;
        if (travelled < world.Profile.GhostStride) return;
        travelled %= world.Profile.GhostStride;
        WorldAudio.Play(SoundCue.GhostStep, transform.position + Vector3.down * 0.55f, 0.8f);
    }
    private void OnDisable()
    {
        if (hunt != null) hunt.HuntStateChanged -= OnHuntChanged;
        if (voice != null) voice.Stop();
    }
    private void OnDestroy() { if (voice != null) Destroy(voice.gameObject); }
}
