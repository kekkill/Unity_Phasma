using UnityEngine;

[RequireComponent(typeof(WorldAudio))]
public sealed class ArchiveAmbience : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GameSession session;
    [SerializeField] private BoxCollider[] indoorVolumes = System.Array.Empty<BoxCollider>();
    [SerializeField] private BoxCollider basementVolume;

    private const float FadeSeconds = 1.35f;
    private enum Layer { Outdoor, Indoor, Basement }
    private readonly AudioSource[] sources = new AudioSource[3];
    private WorldAudio world;
    private Vector3 weights;

    private void Start()
    {
        world = GetComponent<WorldAudio>();
        if (player == null || world.Profile == null)
        {
            enabled = false;
            return;
        }

        sources[(int)Layer.Outdoor] = CreateLoop(SoundCue.ArchiveOutdoorAmbience, "Outdoor wind");
        sources[(int)Layer.Indoor] = CreateLoop(SoundCue.ArchiveIndoorAmbience, "House room tone");
        sources[(int)Layer.Basement] = CreateLoop(SoundCue.ArchiveBasementAmbience, "Basement ambience");
        Blend(1f);
    }

    private AudioSource CreateLoop(SoundCue cue, string sourceName)
    {
        AudioClip clip = world.Profile.Clip(cue);
        if (clip == null) return null;
        AudioSource source = world.CreateSource(transform, sourceName);
        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 0f;
        source.priority = 224;
        source.volume = 0f;
        return source;
    }

    private Layer GetLayer(Vector3 position)
    {
        if (basementVolume != null && basementVolume.bounds.Contains(position)) return Layer.Basement;
        foreach (BoxCollider volume in indoorVolumes)
            if (volume != null && volume.bounds.Contains(position)) return Layer.Indoor;
        return Layer.Outdoor;
    }

    private void Update() => Blend(Time.unscaledDeltaTime / FadeSeconds);

    private void Blend(float step)
    {
        if (world == null || player == null) return;
        bool play = world.isActiveAndEnabled && (session == null || !session.HasEnded);
        int active = play ? (int)GetLayer(player.position) : -1;
        Vector3 levels = world.Profile.ArchiveAmbienceLevels;
        for (int index = 0; index < sources.Length; index++)
        {
            AudioSource source = sources[index];
            if (source == null) continue;
            weights[index] = Mathf.MoveTowards(weights[index], index == active ? 1f : 0f, step);
            source.volume = weights[index] * Mathf.Clamp01(levels[index]) * world.Profile.MasterVolume;
            if (source.volume > 0f && !source.isPlaying) source.Play();
            else if (source.volume == 0f && source.isPlaying) source.Stop();
        }
    }

    private void OnDisable()
    {
        foreach (AudioSource source in sources)
            if (source != null) source.Stop();
        weights = Vector3.zero;
    }
}
