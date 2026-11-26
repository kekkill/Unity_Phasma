using UnityEngine;

public sealed class HubStation : MonoBehaviour
{
    public enum StationKind { MapBoard, EquipmentRack, LaunchTerminal, ReturnDoor }

    [SerializeField] private StationKind kind;
    [SerializeField] private string prompt;

    public StationKind Kind => kind;
    public string Prompt => prompt;
}
