using UnityEngine;

public class ThermometerItem : MonoBehaviour, IUsableItem
{
    public bool HasReading { get; private set; }
    public float LastTemperature { get; private set; }
    public string LastRoomName { get; private set; }
    public void Use(GameObject user)
    {
        PlayerRoomTracker roomTracker =
            user.GetComponent<PlayerRoomTracker>();

        if (roomTracker == null)
        {
            HasReading = false;
            Debug.Log("У игрока нет PlayerRoomTracker.");
            return;
        }

        Room room = roomTracker.CurrentRoom;

        if (room == null)
        {
            HasReading = false;
            Debug.Log("Игрок сейчас не находится в комнате.");
            return;
        }

        MeasureRoom(room);
        Debug.Log(
            "Температура комнаты " +
            room.RoomName +
            ": " +
            room.Temperature +
            " °C"
        );
    }
    public bool MeasureRoom(Room room)
    {
        HasReading = room != null;
        if (!HasReading) return false;
        LastTemperature = room.Temperature;
        LastRoomName = room.RoomName;
        return true;
    }
}
