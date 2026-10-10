using UnityEngine;

public class MotionSensorItem : MonoBehaviour
{
    [Header("Indicator")]
    [SerializeField] private GameObject indicatorVisual;

    [SerializeField] private float activationDuration = 1f;

    [Header("Debug")]
    [SerializeField] private bool isTriggered;

    private float activeUntilTime;

    private void Awake()
    {
        SetIndicator(false);
    }

    private void Update()
    {
        if (!isTriggered)
        {
            return;
        }

        if (Time.time >= activeUntilTime)
        {
            isTriggered = false;

            SetIndicator(false);
        }
    }

    public void DetectGhost(
        GhostActor ghostActor)
    {
        if (ghostActor == null)
        {
            return;
        }

        isTriggered = true;

        activeUntilTime =
            Time.time + activationDuration;

        SetIndicator(true);

        Debug.Log(
            "Motion sensor detected the ghost."
        );
    }

    private void SetIndicator(
        bool active)
    {
        if (indicatorVisual != null)
        {
            indicatorVisual.SetActive(
                active
            );
        }
    }
}