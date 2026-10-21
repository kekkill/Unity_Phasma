using UnityEngine;

public class DoorAudio : MonoBehaviour
{
    private Door door;
    private void OnEnable() { Bind(); }
    private void Start() { Bind(); } // Required components on newly created objects may awaken before Door exists.
    private void Bind()
    {
        if (door != null) return;
        door = GetComponent<Door>();
        if (door != null) door.OpenStateChanged += OnChanged;
    }
    private void OnChanged(bool open) => WorldAudio.Play(open ? SoundCue.DoorOpen : SoundCue.DoorClose, door.NoisePosition, 0.55f);
    private void OnDisable()
    {
        if (door != null) door.OpenStateChanged -= OnChanged;
        door = null;
    }
}
