using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float crouchSpeed = 2.2f;

    [SerializeField] private float gravity = -20f;

    private CharacterController characterController;
    private PlayerInputReader inputReader;

    private float verticalVelocity;

    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();

        inputReader =
            GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        Vector2 input = inputReader.MoveInput;

        Vector3 horizontalMovement =
            transform.right * input.x +
            transform.forward * input.y;

        if (horizontalMovement.sqrMagnitude > 1f)
        {
            horizontalMovement.Normalize();
        }

        float currentSpeed = moveSpeed;

        if (inputReader.CrouchIsPressed)
        {
            currentSpeed = crouchSpeed;
        }
        else if (inputReader.SprintIsPressed)
        {
            currentSpeed = sprintSpeed;
        }

        if (characterController.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity =
            horizontalMovement * currentSpeed;

        velocity.y = verticalVelocity;

        characterController.Move(
            velocity * Time.deltaTime
        );
    }
}