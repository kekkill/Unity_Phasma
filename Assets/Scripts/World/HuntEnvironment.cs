using UnityEngine;

// Consumes the hunt lifecycle; doors/lights never decide when to start or end a hunt.
[DefaultExecutionOrder(20)]
public class HuntEnvironment : MonoBehaviour
{
    [SerializeField] private GhostHuntController hunt;
    [SerializeField] private GameSession session;
    [SerializeField] private Door entrance;
    [SerializeField] private Collider exitBarrier;
    [SerializeField] private LightSwitch[] roomLights = new LightSwitch[0];
    private bool originalBarrierEnabled;
    private float startedAt;
    private void OnEnable()
    {
        originalBarrierEnabled = exitBarrier != null && exitBarrier.enabled;
        if (hunt != null) hunt.HuntStateChanged += ApplyState;
        if (session != null) session.MissionEnded += OnMissionEnded;
        ApplyState(hunt != null && hunt.IsHunting && (session == null || !session.HasEnded));
    }
    private void ApplyState(bool hunting)
    {
        if (entrance != null) entrance.SetLocked(this, hunting);
        // Immediate physical seal while the visible leaf is still rotating shut.
        if (exitBarrier != null) exitBarrier.enabled = hunting || originalBarrierEnabled;
        startedAt = Time.time;
        RestoreLights();
    }
    private void Update()
    {
        bool hunting = hunt != null && hunt.IsHunting && (session == null || !session.HasEnded);
        float interval = hunt != null && hunt.SequenceProfile != null ? hunt.SequenceProfile.FlickerInterval : 0.18f;
        bool darkPulse = hunting && Mathf.FloorToInt((Time.time - startedAt) / interval) % 2 == 0;
        foreach (var lightSwitch in roomLights)
            if (lightSwitch != null) lightSwitch.SetSuppressed(this, darkPulse);
    }
    private void RestoreLights()
    {
        foreach (var lightSwitch in roomLights)
            if (lightSwitch != null) lightSwitch.SetSuppressed(this, false);
    }
    private void OnMissionEnded()
    {
        if (hunt != null) hunt.ForceEndHunt();
        ApplyState(false);
    }
    private void OnDisable()
    {
        if (hunt != null) hunt.HuntStateChanged -= ApplyState;
        if (session != null) session.MissionEnded -= OnMissionEnded;
        if (entrance != null) entrance.SetLocked(this, false);
        if (exitBarrier != null) exitBarrier.enabled = originalBarrierEnabled;
        RestoreLights();
    }
}
