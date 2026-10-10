using UnityEngine;

public class UltravioletTrace : MonoBehaviour
{
    [SerializeField] private float lifetime = 60f;

    [Header("Visibility")]
    [SerializeField] private Renderer traceRenderer;

    [SerializeField] private float revealDuration = 0.1f;

    private float revealedUntilTime;

    public void Initialize(float traceLifetime)
    {
        lifetime = traceLifetime;
    }

    private void Awake()
    {
        if (traceRenderer == null)
        {
            traceRenderer =
                GetComponentInChildren<Renderer>();
        }

        SetVisible(false);
    }

    private void Start()
    {
        if (lifetime > 0f)
        {
            Destroy(
                gameObject,
                lifetime
            );
        }
    }

    private void Update()
    {
        if (Time.time > revealedUntilTime)
        {
            SetVisible(false);
        }
    }

    public void Reveal()
    {
        revealedUntilTime =
            Time.time + revealDuration;

        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (traceRenderer != null)
        {
            traceRenderer.enabled =
                visible;
        }
    }
}