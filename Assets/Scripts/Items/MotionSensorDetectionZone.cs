using UnityEngine;

public class MotionSensorDetectionZone : MonoBehaviour
{
    [SerializeField] private MotionSensorItem sensor;

    private void Awake()
    {
        if (sensor == null)
        {
            sensor =
                GetComponentInParent<MotionSensorItem>();
        }
    }

    private void OnTriggerEnter(
        Collider other)
    {
        GhostActor ghostActor =
            other.GetComponentInParent<GhostActor>();

        if (ghostActor == null)
        {
            return;
        }

        if (sensor != null)
        {
            sensor.DetectGhost(
                ghostActor
            );
        }
    }
}