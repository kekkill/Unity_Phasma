using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerCrouch : MonoBehaviour
{
    [SerializeField] private Transform headTransform;

    [Header("Standing")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float standingHeadY = 0.7f;

    [Header("Crouching")]
    [SerializeField] private float crouchingHeight = 1.2f;
    [SerializeField] private float crouchingHeadY = 0.1f;

    [Header("Speed")]
    [SerializeField] private float crouchTransitionSpeed = 6f;

    private CharacterController characterController;
    private PlayerInputReader inputReader;

    private float standingCenterY;
    private float crouchingCenterY;

    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();

        inputReader =
            GetComponent<PlayerInputReader>();

        standingCenterY =
            characterController.center.y;

        float standingBottom =
            standingCenterY - standingHeight / 2f;

        crouchingCenterY =
            standingBottom + crouchingHeight / 2f;
    }

    private void Update()
    {
        if (inputReader.GameplayBlocked) return; // Preserve the current posture while writing notes.
        bool wantsToCrouch =
            inputReader.CrouchIsPressed;

        float targetHeight =
            wantsToCrouch
                ? crouchingHeight
                : standingHeight;

        float targetCenterY =
            wantsToCrouch
                ? crouchingCenterY
                : standingCenterY;

        float targetHeadY =
            wantsToCrouch
                ? crouchingHeadY
                : standingHeadY;

        characterController.height =
            Mathf.MoveTowards(
                characterController.height,
                targetHeight,
                crouchTransitionSpeed * Time.deltaTime
            );

        Vector3 center =
            characterController.center;

        center.y =
            Mathf.MoveTowards(
                center.y,
                targetCenterY,
                crouchTransitionSpeed * Time.deltaTime
            );

        characterController.center = center;

        Vector3 headPosition =
            headTransform.localPosition;

        headPosition.y =
            Mathf.MoveTowards(
                headPosition.y,
                targetHeadY,
                crouchTransitionSpeed * Time.deltaTime
            );

        headTransform.localPosition =
            headPosition;
    }
}