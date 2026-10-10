using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Room : MonoBehaviour
{
    [SerializeField] private string roomName = "Room";

    [Header("Temperature")]
    [SerializeField] private float normalTemperature = 20f;
    [SerializeField] private float ghostTemperature = 5f;
    [SerializeField] private float freezingTemperature = -5f;

    [SerializeField] private float coolingSpeed = 1f;
    [SerializeField] private float warmingSpeed = 0.5f;

    private float currentTemperature;
    private float targetTemperature;

    private bool isGhostRoom;

    public string RoomName => roomName;
    public float Temperature => currentTemperature;
    public bool IsGhostRoom => isGhostRoom;
    private BoxCollider roomCollider;

    private void Awake()
    {
        roomCollider = GetComponent<BoxCollider>();
        currentTemperature = normalTemperature;
        targetTemperature = normalTemperature;
    }

    private void Update()
    {
        float speed =
            currentTemperature > targetTemperature
                ? coolingSpeed
                : warmingSpeed;

        currentTemperature = Mathf.MoveTowards(
            currentTemperature,
            targetTemperature,
            speed * Time.deltaTime
        );
    }

    public void SetGhostRoom(
        bool value,
        bool hasFreezingEvidence)
    {
        isGhostRoom = value;

        if (!isGhostRoom)
        {
            targetTemperature = normalTemperature;
            return;
        }

        targetTemperature =
            hasFreezingEvidence
                ? freezingTemperature
                : ghostTemperature;
    }
    public Vector3 GetRandomPoint()
    {
        Vector3 localPoint = new Vector3(
            Random.Range(-0.5f, 0.5f) * roomCollider.size.x,
            Random.Range(-0.5f, 0.5f) * roomCollider.size.y,
            Random.Range(-0.5f, 0.5f) * roomCollider.size.z
        );

        localPoint += roomCollider.center;

        return transform.TransformPoint(localPoint);
    }
}