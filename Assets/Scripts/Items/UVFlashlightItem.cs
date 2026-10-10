using UnityEngine;

public class UVFlashlightItem : MonoBehaviour, IUsableItem
{
    [Header("Light")]
    [SerializeField] private Light ultravioletLight;

    [Header("Detection")]
    [SerializeField] private Transform detectionOrigin;

    [SerializeField] private float detectionRadius = 5f;

    [Range(0f, 180f)]
    [SerializeField] private float detectionAngle = 45f;

    [Header("Debug")]
    [SerializeField] private bool isOn;

    private void Awake()
    {
        if (detectionOrigin == null)
        {
            detectionOrigin =
                transform;
        }

        UpdateLightState();
    }

    private void Update()
    {
        if (!isOn)
        {
            return;
        }

        RevealUltravioletTraces();
    }

    public void Use(GameObject user)
    {
        isOn =
            !isOn;

        UpdateLightState();
    }

    private void UpdateLightState()
    {
        if (ultravioletLight != null)
        {
            ultravioletLight.enabled =
                isOn;
        }
    }

    private void RevealUltravioletTraces()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                detectionOrigin.position,
                detectionRadius,
                ~0,
                QueryTriggerInteraction.Collide
            );

        foreach (Collider hit in hits)
        {
            UltravioletTrace trace =
                hit.GetComponentInParent<UltravioletTrace>();

            if (trace == null)
            {
                continue;
            }

            Vector3 directionToTrace =
                trace.transform.position -
                detectionOrigin.position;

            float angle =
                Vector3.Angle(
                    detectionOrigin.forward,
                    directionToTrace
                );

            if (angle >
                detectionAngle * 0.5f)
            {
                continue;
            }

            trace.Reveal();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin =
            detectionOrigin != null
                ? detectionOrigin
                : transform;

        Gizmos.DrawWireSphere(
            origin.position,
            detectionRadius
        );
    }
}