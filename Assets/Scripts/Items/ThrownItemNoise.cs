using UnityEngine;

// Only a player-attributed throw arms impacts. Held/stored/dead/ghost-thrown items stay silent to AI.
public class ThrownItemNoise : MonoBehaviour
{
    [SerializeField, Min(0f)] private float minimumImpactSpeed = 1f;
    [SerializeField, Min(0.05f)] private float impactCooldown = 0.2f;
    [SerializeField, Min(0.1f)] private float speedForMaximumNoise = 6f;
    private GhostPerceptionTarget source;
    private int remainingImpacts;
    private float nextImpact;
    public bool IsArmed => source != null && remainingImpacts > 0;
    public void Arm(GhostPerceptionTarget value) { source = value; remainingImpacts = value != null ? 3 : 0; nextImpact = 0f; }
    public void Disarm() { source = null; remainingImpacts = 0; }
    private void OnDisable() => Disarm();
    private void OnCollisionEnter(Collision collision)
    {
        if (remainingImpacts <= 0 || source == null || !source.CanBeDetected || Time.time < nextImpact ||
            collision.relativeVelocity.magnitude < minimumImpactSpeed ||
            collision.collider.GetComponentInParent<GhostPerceptionTarget>() == source) return;
        Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point + Vector3.up * 0.08f : transform.position;
        if (WorldNoise.Publish(point, source, Mathf.Clamp01(collision.relativeVelocity.magnitude / Mathf.Max(0.1f, speedForMaximumNoise)), 0.5f, WorldNoiseKind.Impact))
        { remainingImpacts--; nextImpact = Time.time + impactCooldown; }
    }
}
