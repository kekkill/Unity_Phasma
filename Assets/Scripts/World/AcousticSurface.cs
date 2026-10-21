using UnityEngine;

// One logical barrier may consist of several colliders. Transmission is applied once per surface.
public class AcousticSurface : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float transmission = 0.25f;
    [SerializeField, Range(0f, 1f)] private float closedDoorTransmission = 0.55f;
    [SerializeField, Range(0f, 1f)] private float openDoorTransmission = 0.9f;
    public float Transmission
    {
        get
        {
            Door door = GetComponent<Door>();
            return door == null ? Mathf.Clamp01(transmission) :
                Mathf.Lerp(closedDoorTransmission, openDoorTransmission, door.OpenFraction);
        }
    }
}
