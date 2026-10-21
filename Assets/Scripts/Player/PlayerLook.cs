using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform headTransform;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float maxLookAngle = 85f;

    private PlayerInputReader inputReader;

    private float pitch;

    private void Awake()
    {
        inputReader =
            GetComponent<PlayerInputReader>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Vector2 lookInput =
            inputReader.LookInput * mouseSensitivity * GamePreferences.Sensitivity;

        transform.Rotate(
            Vector3.up * lookInput.x
        );

        pitch -= lookInput.y;

        pitch = Mathf.Clamp(
            pitch,
            -maxLookAngle,
            maxLookAngle
        );

        headTransform.localRotation =
            Quaternion.Euler(pitch, 0f, 0f);
    }
}
