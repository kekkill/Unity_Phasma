using UnityEngine;

// Bounded local presentation. This never publishes observations to AI or controls gameplay state.
[DefaultExecutionOrder(-175)]
public class WorldAudio : MonoBehaviour
{
    public static WorldAudio Instance { get; private set; }
    [SerializeField] private PrototypeAudioProfile profile;
    public PrototypeAudioProfile Profile => profile;
    private AudioSource[] voices;
    private int nextVoice;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Instance = null; }
    private void Awake()
    {
        int count = profile != null ? profile.MaxOneShotVoices : 1;
        voices = new AudioSource[count];
        for (int i = 0; i < count; i++) voices[i] = CreateSource(transform, "World sound " + i);
    }
    private void OnEnable() { Instance = this; }
    private void OnDisable()
    {
        if (Instance == this) Instance = null;
        if (voices != null) foreach (var voice in voices) if (voice != null) voice.Stop();
    }
    public AudioSource CreateSource(Transform parent, string sourceName)
    {
        var obj = new GameObject(sourceName); obj.transform.SetParent(parent, false);
        var source = obj.AddComponent<AudioSource>(); source.playOnAwake = false;
        source.spatialBlend = 1f; source.dopplerLevel = 0f; source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = profile != null ? profile.MinimumDistance : 1f;
        source.maxDistance = profile != null ? profile.MaximumDistance : 18f;
        return source;
    }
    public static void Play(SoundCue cue, Vector3 position, float volume = 1f, bool spatial = true, float pitch = 1f)
    {
        var world = Instance;
        if (world == null || !world.isActiveAndEnabled || world.profile == null || world.voices == null) return;
        AudioClip clip = world.profile.Clip(cue); if (clip == null) return;
        AudioSource voice = null;
        foreach (var candidate in world.voices) if (!candidate.isPlaying) { voice = candidate; break; }
        // Reuse one bounded voice under load; no unbounded PlayClipAtPoint objects/PlayOneShot mixing.
        if (voice == null) { voice = world.voices[world.nextVoice]; world.nextVoice = (world.nextVoice + 1) % world.voices.Length; }
        voice.Stop(); voice.transform.position = position; voice.clip = clip;
        voice.loop = false; voice.spatialBlend = spatial ? 1f : 0f;
        voice.pitch = Mathf.Clamp(pitch, 0.5f, 1.5f);
        voice.volume = world.profile.MasterVolume * Mathf.Clamp01(volume); voice.Play();
    }
}
