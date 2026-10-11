using System.Collections.Generic;
using UnityEngine;

// Owns world-position snapshots and one moving hunt signal; never changes journal notes.
public class GhostActivityEmitter : MonoBehaviour
{
    public const int MaxInteractionSignals = 16;
    private readonly List<GhostActivityEvent> signals = new List<GhostActivityEvent>();
    private readonly List<UltravioletTrace> traces = new List<UltravioletTrace>();
    private GhostActivityEvent huntSignal;
    private GhostController controller;
    private GhostController Controller => controller != null ? controller : controller = GetComponent<GhostController>();
    public GhostActivityEvent HuntSignal => huntSignal;
    public int ActiveInteractionSignals
    {
        get { signals.RemoveAll(signal => signal == null || !signal.IsActive); return signals.Count; }
    }
    public GhostActivityEvent EmitEmfAt(Vector3 position)
    {
        GhostController owner = Controller;
        if (!isActiveAndEnabled || owner == null || owner.Profile == null || owner.ActivityPrefab == null) return null;
        signals.RemoveAll(signal => signal == null);
        while (signals.Count >= MaxInteractionSignals)
        {
            RemoveSignal(signals[0]); signals.RemoveAt(0);
        }
        var signal = Instantiate(owner.ActivityPrefab, position, Quaternion.identity);
        signal.Initialize(GhostActivityRules.EmfLevel(owner.Profile, owner.CurrentAnger, owner.IsHunting), owner.ActivityLifetime);
        signals.Add(signal);
        return signal;
    }
    public UltravioletTrace EmitUltravioletAt(Vector3 position)
    {
        GhostController owner = Controller;
        if (!isActiveAndEnabled || owner == null || owner.IsHunting || owner.Profile == null ||
            !owner.Profile.HasUltravioletEvidence || owner.UltravioletTracePrefab == null) return null;
        traces.RemoveAll(trace => trace == null);
        var trace = Instantiate(owner.UltravioletTracePrefab, position, Quaternion.identity);
        trace.Initialize(owner.UltravioletTraceLifetime);
        traces.Add(trace);
        return trace;
    }
    public void RefreshHuntSignal()
    {
        GhostController owner = Controller;
        GhostActor actor = GetComponent<GhostActor>();
        if (!isActiveAndEnabled || owner == null || !owner.IsHunting || owner.Profile == null ||
            owner.ActivityPrefab == null || (actor != null && !actor.isActiveAndEnabled))
        { StopHuntSignal(); return; }
        if (huntSignal == null) huntSignal = Instantiate(owner.ActivityPrefab, transform.position, Quaternion.identity);
        huntSignal.transform.position = transform.position;
        huntSignal.Initialize(GhostActivityRules.EmfLevel(owner.Profile, owner.CurrentAnger, true), 0.5f);
    }
    private void Update() => RefreshHuntSignal();
    public void StopHuntSignal() { RemoveSignal(huntSignal); huntSignal = null; }
    private static void RemoveSignal(GhostActivityEvent signal)
    {
        if (signal == null) return;
        signal.gameObject.SetActive(false); Destroy(signal.gameObject);
    }
    private void OnDisable() => StopHuntSignal();
    private void OnDestroy()
    {
        StopHuntSignal();
        foreach (var signal in signals) RemoveSignal(signal);
        foreach (var trace in traces) if (trace != null) Destroy(trace.gameObject);
    }
}
