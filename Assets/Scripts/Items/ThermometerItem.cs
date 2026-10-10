using UnityEngine;

public class ThermometerItem : MonoBehaviour, IUsableItem
{
    public void Use(GameObject user)
    {
        PlayerRoomTracker roomTracker =
            user.GetComponent<PlayerRoomTracker>();

        if (roomTracker == null)
        {
            Debug.Log("У игрока нет PlayerRoomTracker.");
            return;
        }

        Room room = roomTracker.CurrentRoom;

        if (room == null)
        {
            Debug.Log("Игрок сейчас не находится в комнате.");
            return;
        }

        Debug.Log(
            "Температура комнаты " +
            room.RoomName +
            ": " +
            room.Temperature +
            " °C"
        );
    }
}