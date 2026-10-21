using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerFootstepAudio : MonoBehaviour
{
    private CharacterController body;
    private PlayerLifeState life;
    private Vector3 previousPosition;
    private float travelled;
    private void Awake() { body = GetComponent<CharacterController>(); life = GetComponent<PlayerLifeState>(); }
    private void OnEnable() { previousPosition = transform.position; travelled = 0f; }
    private void LateUpdate()
    {
        float moved = Vector3.ProjectOnPlane(transform.position - previousPosition, Vector3.up).magnitude;
        previousPosition = transform.position;
        var audioWorld = WorldAudio.Instance;
        if (audioWorld == null || audioWorld.Profile == null || !body.enabled || !body.isGrounded || moved > 2f ||
            (life != null && (!life.IsAlive || (life.Session != null && life.Session.HasEnded))))
        { travelled = 0f; return; }
        if (moved < 0.001f) return;
        travelled += moved;
        if (travelled < audioWorld.Profile.PlayerStride) return;
        travelled %= audioWorld.Profile.PlayerStride;
        float speed = Vector3.ProjectOnPlane(body.velocity, Vector3.up).magnitude;
        Vector3 foot = new Vector3(transform.position.x, body.bounds.min.y + 0.03f, transform.position.z);
        WorldAudio.Play(SoundCue.PlayerStep, foot, Mathf.Lerp(0.18f, 0.5f, Mathf.Clamp01(speed / 6.5f)));
    }
}
