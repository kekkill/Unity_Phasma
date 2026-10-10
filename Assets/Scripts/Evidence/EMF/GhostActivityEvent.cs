using UnityEngine;

public class GhostActivityEvent : MonoBehaviour
{
    [SerializeField] private int emfLevel = 2;
    [SerializeField] private float lifetime = 100f;

    public int EmfLevel => emfLevel;

    public void Initialize(int level, float eventLifetime)
    {
        emfLevel = level;
        lifetime = eventLifetime;
    }

    private void Start()
    {
        if (lifetime > 0f)
        {
            Destroy(gameObject, lifetime);
        }
    }
}