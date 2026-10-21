using UnityEngine;

// Physical sound is independent of whether an impact is attributed to a living player for AI hearing.
public class ItemAudio : MonoBehaviour
{
    private PickupItem item;
    private float nextImpact;
    private void OnEnable() { Bind(); }
    private void Start() { Bind(); }
    private void Bind()
    {
        if (item != null) return;
        item = GetComponent<PickupItem>();
        if (item != null) item.Thrown += OnThrown;
    }
    private void OnThrown() => WorldAudio.Play(SoundCue.Throw, transform.position, 0.3f);
    private void OnCollisionEnter(Collision collision)
    {
        if (item == null || item.State != PickupItemState.Dropped || Time.time < nextImpact) return;
        float speed = collision.relativeVelocity.magnitude;
        if (speed < 0.8f || collision.collider.GetComponentInParent<PlayerLifeState>() != null) return;
        nextImpact = Time.time + 0.2f;
        Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        WorldAudio.Play(SoundCue.Impact, point, Mathf.Clamp01(speed / 6f));
    }
    private void OnDisable()
    {
        if (item != null) item.Thrown -= OnThrown;
        item = null;
    }
}
