using System.Collections.Generic;
using UnityEngine;

public enum GhostSelectionMode { Manual, RandomFromCatalog }

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(GhostActivityEmitter))]
public class GhostController : MonoBehaviour
{
    [SerializeField] private Room[] rooms;
    [SerializeField] private GhostActor ghostActor;

    [Header("Ghost Profile")]
    [SerializeField] private GhostProfile ghostProfile;
    [SerializeField] private GhostSelectionMode selectionMode;
    [SerializeField] private JournalCatalog selectionCatalog;
    [Header("Runtime selection (Play Mode)")]
    [SerializeField] private GhostProfile activeProfile;

    private void Awake() { activeProfile = null; TryInitializeProfile(out _); }
    public bool TryInitializeProfile(out string reason)
    {
        if (activeProfile != null) { reason = null; return true; }
        if (selectionMode == GhostSelectionMode.Manual)
        {
            activeProfile = ghostProfile;
            reason = activeProfile != null ? null : "Assign a manual Ghost Profile.";
            return activeProfile != null;
        }
        if (selectionCatalog == null || !selectionCatalog.TryValidate(out _))
        { reason = "Assign a valid selection catalog."; return false; }
        activeProfile = selectionCatalog.Entries[Random.Range(0, selectionCatalog.Entries.Count)].Profile;
        reason = null;
        return true;
    }

    [Header("EMF Activity")]
    [SerializeField] private GhostActivityEvent activityPrefab;
    [SerializeField] private float activityLifetime = 8f;

    [Header("Ultraviolet Evidence")]
    [SerializeField] private UltravioletTrace ultravioletTracePrefab;
    [SerializeField] private float ultravioletTraceLifetime = 60f;

    private Room ghostRoom;

    public Room GhostRoom =>
        ghostRoom;

    public GhostProfile Profile =>
        activeProfile != null ? activeProfile : selectionMode == GhostSelectionMode.Manual ? ghostProfile : null;

    public Room FindRoomAt(Vector3 position)
    {
        if (rooms == null) return null;
        foreach (Room room in rooms)
            if (room != null && room.ContainsPoint(position)) return room;
        return null;
    }

    private void Start()
    {
        if (!TryInitializeProfile(out string selectionError))
        {
            Debug.LogError(
                "GhostController: " + selectionError
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
            Profile.GhostName
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

        var eligibleRooms = new List<Room>();
        foreach (Room room in rooms)
            if (room != null && room.CanBeGhostRoom) eligibleRooms.Add(room);
        if (eligibleRooms.Count == 0)
        {
            Debug.LogError("GhostController: no room allows ghost spawning.", this);
            return;
        }
        ghostRoom = eligibleRooms[Random.Range(0, eligibleRooms.Count)];

        foreach (Room room in rooms)
        {
            if (room == null) continue;
            bool isSelectedRoom =
                room == ghostRoom;

            room.SetGhostRoom(
                isSelectedRoom,
                Profile.HasFreezingEvidence
            );
        }

        Debug.Log(
            "Ghost room: " +
            ghostRoom.RoomName
        );
    }

    public bool IsHunting => GetComponent<GhostHuntController>() != null && GetComponent<GhostHuntController>().IsHunting;
    public float CurrentAnger => GetComponent<GhostAggression>() != null ? GetComponent<GhostAggression>().CurrentAnger : Profile != null ? Profile.InitialAnger : 0f;
    public GhostActivityEvent ActivityPrefab => activityPrefab;
    public float ActivityLifetime => Mathf.Max(0.1f, activityLifetime);
    public UltravioletTrace UltravioletTracePrefab => ultravioletTracePrefab;
    public float UltravioletTraceLifetime => Mathf.Max(0.1f, ultravioletTraceLifetime);

    // Preserve existing entry points and Inspector references; emission belongs to its own component.
    public void CreateInteractionEvidenceAt(Vector3 position)
    {
        TryCreateEmfActivityAt(position);
        TryCreateUltravioletTraceAt(position);
    }
    public void TryCreateEmfActivityAt(Vector3 position) => GetComponent<GhostActivityEmitter>().EmitEmfAt(position);
    public void TryCreateUltravioletTraceAt(Vector3 position) => GetComponent<GhostActivityEmitter>().EmitUltravioletAt(position);
}
