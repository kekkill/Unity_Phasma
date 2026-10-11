using UnityEngine;

public class GhostActivityEvent : MonoBehaviour
{
    [SerializeField] private int emfLevel = 2;
    [SerializeField] private float lifetime = 100f;
    private float expiresAt;
    private bool initialized;

    public int EmfLevel => emfLevel;
    public bool IsActive => isActiveAndEnabled && (!initialized || Time.time < expiresAt);

    public void Initialize(int level, float eventLifetime)
    {
        emfLevel = Mathf.Clamp(level, 1, 5);
        lifetime = eventLifetime;
        expiresAt = lifetime > 0f ? Time.time + lifetime : float.PositiveInfinity;
        initialized = true;
    }

    private void Start()
    {
        if (!initialized) Initialize(emfLevel, lifetime);
    }
    private void Update()
    {
        if (!IsActive) { gameObject.SetActive(false); Destroy(gameObject); }
    }
}
