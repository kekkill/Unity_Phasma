using System.Collections.Generic;
using UnityEngine;

public enum WorldNoiseKind { Action, Door, Throw, Impact }

// Immutable observation: its position never follows the participant after emission.
public readonly struct WorldNoiseEvent
{
    public readonly ulong Id;
    public readonly Vector3 Position;
    public readonly GhostPerceptionTarget Source;
    public readonly float Loudness, ExpiresAt;
    public readonly WorldNoiseKind Kind;
    public readonly HidingZone OriginHidingZone;
    public WorldNoiseEvent(ulong id, Vector3 position, GhostPerceptionTarget source, float loudness,
        float expiresAt, WorldNoiseKind kind, HidingZone zone)
    {
        Id = id; Position = position; Source = source; Loudness = loudness;
        ExpiresAt = expiresAt; Kind = kind; OriginHidingZone = zone;
    }
}

// Local event buffer, ready for a future authoritative publisher; no audio/input ownership here.
public static class WorldNoise
{
    public const int Capacity = 64;
    private static readonly List<WorldNoiseEvent> events = new List<WorldNoiseEvent>();
    private static ulong sequence;
    public static IReadOnlyList<WorldNoiseEvent> Recent
    {
        get { events.RemoveAll(item => Time.time >= item.ExpiresAt || item.Source == null || !item.Source.CanBeDetected); return events; }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { events.Clear(); sequence = 0; }
    public static bool Publish(Vector3 position, GhostPerceptionTarget source, float loudness,
        float duration = 0.5f, WorldNoiseKind kind = WorldNoiseKind.Action, HidingZone originZone = null)
    {
        if (source == null || !source.CanBeDetected || SafeZone.ContainsAny(position) || loudness <= 0f ||
            (source.LifeState != null && source.LifeState.Session != null && source.LifeState.Session.HasEnded)) return false;
        events.RemoveAll(item => Time.time >= item.ExpiresAt);
        if (events.Count >= Capacity) events.RemoveAt(0);
        events.Add(new WorldNoiseEvent(++sequence, position, source, Mathf.Clamp01(loudness),
            Time.time + Mathf.Clamp(duration, 0.05f, 3f), kind, originZone != null ? originZone : HidingZone.FindAt(position)));
        return true;
    }
}
