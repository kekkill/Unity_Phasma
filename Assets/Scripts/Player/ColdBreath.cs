using UnityEngine;

// First-person condensation. A single small texture per puff, no particle camera or dynamic lights.
[RequireComponent(typeof(PlayerRoomTracker), typeof(PlayerInputReader))]
public sealed class ColdBreath : MonoBehaviour
{
    [SerializeField] private Texture2D vapor;
    [SerializeField] private float coldThreshold = 7f;
    private PlayerRoomTracker roomTracker;
    private PlayerInputReader input;
    private PlayerLifeState life;
    private float breathAt;
    private bool wasCold;
    private const float Interval = 3.8f;
    private const float Lifetime = 1.25f;

    private void Awake()
    {
        roomTracker = GetComponent<PlayerRoomTracker>();
        input = GetComponent<PlayerInputReader>();
        life = GetComponent<PlayerLifeState>();
    }

    private void LateUpdate()
    {
        Room room = roomTracker.CurrentRoom;
        bool cold = room != null && room.Temperature < coldThreshold && !input.GameplayBlocked &&
            (life == null || life.IsAlive && (life.Session == null || !life.Session.HasEnded));
        if (cold && !wasCold) breathAt = Time.time + 0.65f;
        if (cold && Time.time > breathAt + Interval) breathAt = Time.time;
        wasCold = cold;
    }

    private void OnGUI()
    {
        if (!wasCold || vapor == null || Event.current.type != EventType.Repaint) return;
        float elapsed = Time.time - breathAt;
        if (elapsed < 0f || elapsed >= Lifetime) return;

        float progress = elapsed / Lifetime;
        float scale = Mathf.Min(Screen.width, Screen.height);
        float width = scale * Mathf.Lerp(0.12f, 0.30f, progress);
        float x = Screen.width * 0.5f + scale * progress * 0.055f;
        float y = Screen.height * 0.76f - scale * progress * 0.20f;
        Color previous = GUI.color;
        GUI.color = new Color(0.87f, 0.94f, 1f, Mathf.Sin(progress * Mathf.PI) * 0.32f);
        GUI.DrawTexture(new Rect(x - width * 0.5f, y, width, width * 0.72f), vapor, ScaleMode.StretchToFill, true);
        GUI.color = previous;
    }
}
