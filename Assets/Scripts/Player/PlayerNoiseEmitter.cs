using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerNoiseEmitter : MonoBehaviour
{
    [Header("Movement Noise")]
    [SerializeField] private float minimumMovementSpeed = 0.1f;
    [SerializeField] private float speedForMaximumNoise = 6f;

    [Header("Debug")]
    [SerializeField]
    [Range(0f, 1f)]
    private float currentNoiseLevel;

    private CharacterController characterController;

    private float temporaryNoiseLevel;
    private float temporaryNoiseEndTime;

    public float CurrentNoiseLevel =>
        currentNoiseLevel;

    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();
    }

    private void Update()
    {
        UpdateNoiseLevel();
    }

    private void UpdateNoiseLevel()
    {
        Vector3 velocity =
            characterController.velocity;

        velocity.y = 0f;

        float movementSpeed =
            velocity.magnitude;

        float movementNoise = 0f;

        if (movementSpeed > minimumMovementSpeed)
        {
            movementNoise =
                Mathf.Clamp01(
                    movementSpeed /
                    speedForMaximumNoise
                );
        }

        if (Time.time >= temporaryNoiseEndTime)
        {
            temporaryNoiseLevel = 0f;
        }

        currentNoiseLevel =
            Mathf.Max(
                movementNoise,
                temporaryNoiseLevel
            );
    }

    public void EmitNoise(
        float noiseLevel,
        float duration)
    {
        temporaryNoiseLevel =
            Mathf.Clamp01(
                noiseLevel
            );

        temporaryNoiseEndTime =
            Time.time + duration;
    }
}