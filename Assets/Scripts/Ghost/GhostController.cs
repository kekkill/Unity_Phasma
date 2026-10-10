using UnityEngine;

public class GhostController : MonoBehaviour
{
    [SerializeField] private Room[] rooms;
    [SerializeField] private GhostActor ghostActor;

    [Header("Ghost Profile")]
    [SerializeField] private GhostProfile ghostProfile;

    [Header("EMF Evidence")]
    [SerializeField] private GhostActivityEvent activityPrefab;
    [SerializeField] private float activityLifetime = 8f;

    [Header("Ultraviolet Evidence")]
    [SerializeField] private UltravioletTrace ultravioletTracePrefab;
    [SerializeField] private float ultravioletTraceLifetime = 60f;

    private Room ghostRoom;

    public Room GhostRoom =>
        ghostRoom;

    public GhostProfile Profile =>
        ghostProfile;

    private void Start()
    {
        if (ghostProfile == null)
        {
            Debug.LogError(
                "GhostController: Ghost Profile is not assigned."
            );

            return;
        }

        ChooseGhostRoom();

        if (ghostActor != null)
        {
            ghostActor.Initialize(
                ghostRoom
            );
        }
        else
        {
            Debug.LogError(
                "GhostController: GhostActor is not assigned."
            );
        }

        Debug.Log(
            "Ghost type: " +
            ghostProfile.GhostName
        );
    }

    private void ChooseGhostRoom()
    {
        if (rooms == null ||
            rooms.Length == 0)
        {
            Debug.LogError(
                "GhostController: rooms are not assigned."
            );

            return;
        }

        int randomIndex =
            Random.Range(
                0,
                rooms.Length
            );

        ghostRoom =
            rooms[randomIndex];

        foreach (Room room in rooms)
        {
            bool isSelectedRoom =
                room == ghostRoom;

            room.SetGhostRoom(
                isSelectedRoom,
                ghostProfile.HasFreezingEvidence
            );
        }

        Debug.Log(
            "Ghost room: " +
            ghostRoom.RoomName
        );
    }

    public void CreateInteractionEvidenceAt(
        Vector3 position)
    {
        TryCreateEmfActivityAt(
            position
        );

        TryCreateUltravioletTraceAt(
            position
        );
    }

    public void TryCreateEmfActivityAt(
        Vector3 position)
    {
        if (!ghostProfile.HasEmfEvidence)
        {
            Debug.Log(
                "Ghost interaction left no EMF evidence."
            );

            return;
        }

        if (activityPrefab == null)
        {
            Debug.LogError(
                "GhostController: Activity Prefab is not assigned."
            );

            return;
        }

        GhostActivityEvent newActivity =
            Instantiate(
                activityPrefab,
                position,
                Quaternion.identity
            );

        int level =
            Random.Range(2, 5);

        newActivity.Initialize(
            level,
            activityLifetime
        );

        Debug.Log(
            "Ghost left EMF evidence. Level: " +
            level
        );
    }

    public void TryCreateUltravioletTraceAt(
        Vector3 position)
    {
        if (!ghostProfile.HasUltravioletEvidence)
        {
            Debug.Log(
                "Ghost interaction left no ultraviolet evidence."
            );

            return;
        }

        if (ultravioletTracePrefab == null)
        {
            Debug.LogError(
                "GhostController: Ultraviolet Trace Prefab is not assigned."
            );

            return;
        }

        UltravioletTrace newTrace =
            Instantiate(
                ultravioletTracePrefab,
                position,
                Quaternion.identity
            );

        newTrace.Initialize(
            ultravioletTraceLifetime
        );

        Debug.Log(
            "Ghost left ultraviolet evidence."
        );
    }
}