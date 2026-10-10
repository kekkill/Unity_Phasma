using UnityEngine;

public class PlayerRoomTracker : MonoBehaviour
{
    private Room currentRoom;

    public Room CurrentRoom => currentRoom;

    private void OnTriggerEnter(Collider other)
    {
        Room room = other.GetComponent<Room>();

        if (room == null)
            return;

        currentRoom = room;

        Debug.Log(
            "Player entered room: " +
            currentRoom.RoomName
        );
    }

    private void OnTriggerExit(Collider other)
    {
        Room room = other.GetComponent<Room>();

        if (room == null)
            return;

        if (currentRoom == room)
        {
            Debug.Log(
                "Player left room: " +
                currentRoom.RoomName
            );

            currentRoom = null;
        }
    }
}