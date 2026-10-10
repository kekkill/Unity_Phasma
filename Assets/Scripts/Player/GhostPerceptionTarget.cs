using UnityEngine;

public class GhostPerceptionTarget : MonoBehaviour
{
    [SerializeField] private Transform visionPoint;

    private PlayerNoiseEmitter noiseEmitter;

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
            if (noiseEmitter == null)
            {
                return 0f;
            }

            return noiseEmitter.CurrentNoiseLevel;
        }
    }

    private void Awake()
    {
        noiseEmitter =
            GetComponent<PlayerNoiseEmitter>();
    }
}