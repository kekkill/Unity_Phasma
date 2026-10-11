using UnityEngine;

public class GhostPerceptionTarget : MonoBehaviour
{
    [SerializeField] private Transform visionPoint;

    private PlayerNoiseEmitter noiseEmitter;
    public PlayerLifeState LifeState { get; private set; }
    public bool IsAlive => isActiveAndEnabled && (LifeState == null || LifeState.IsAlive);

    public bool CanBeDetected => IsAlive && !SafeZone.ContainsAny(transform.position);

    public Vector3 VisionPosition
    {
        get
        {
            if (visionPoint != null)
            {
                return visionPoint.position;
            }

            return transform.position;
        }
    }

    public float NoiseLevel
    {
        get
        {
            if (!CanBeDetected || noiseEmitter == null)
            {
                return 0f;
            }

            return noiseEmitter.CurrentNoiseLevel;
        }
    }

    private void Awake()
    {
        LifeState = GetComponent<PlayerLifeState>();
        noiseEmitter =
            GetComponent<PlayerNoiseEmitter>();
    }
}