using UnityEngine;

public class DoorAudio : MonoBehaviour
{
    private Door door;
    private bool isCabinetDoor;
    private bool waitingForClose;
    private void OnEnable() { Bind(); }
    private void Start() { Bind(); } // Required components on newly created objects may awaken before Door exists.
    private void Bind()
    {
        if (door != null) return;
        door = GetComponent<Door>();
        if (door == null) return;
        var hidingPlace = GetComponentInParent<HidingZone>();
        isCabinetDoor = hidingPlace != null && hidingPlace.Door == door;
        door.OpenStateChanged += OnChanged;
    }

    private void OnChanged(bool open)
    {
        waitingForClose = !open && !door.IsFullyClosed;
        if (open) WorldAudio.Play(SoundCue.DoorOpen, door.NoisePosition, 0.55f);
    }

    private void LateUpdate()
    {
        if (!waitingForClose || door == null || !door.IsFullyClosed) return;
        waitingForClose = false;
        SoundCue closeCue = isCabinetDoor ? SoundCue.CabinetClose : SoundCue.DoorClose;
        WorldAudio.Play(closeCue, door.NoisePosition, 0.55f);
    }
    private void OnDisable()
    {
        if (door != null) door.OpenStateChanged -= OnChanged;
        door = null;
        waitingForClose = false;
    }
}
