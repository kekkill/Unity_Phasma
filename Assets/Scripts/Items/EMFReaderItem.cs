using UnityEngine;

public class EMFReaderItem : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 3f;

    [Header("Debug")]
    [SerializeField] private int currentLevel = 1;

    private int previousLevel = -1;

    public int CurrentLevel => currentLevel;
    public bool HasEmfFiveReading => currentLevel == 5;

    private void Update()
    {
        RefreshReading();
    }

    public void RefreshReading()
    {
        // По умолчанию прибор ничего не обнаруживает.
        currentLevel = 1;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            ~0,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider hit in hits)
        {
            GhostActivityEvent activity =
                hit.GetComponentInParent<GhostActivityEvent>();

            if (activity == null || !activity.IsActive)
                continue;

            if (activity.EmfLevel > currentLevel)
            {
                currentLevel = activity.EmfLevel;
            }
        }

        if (currentLevel != previousLevel)
        {
            Debug.Log(
                "Field Meter Level: " +
                currentLevel
            );

            previousLevel = currentLevel;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRadius
        );
    }
}
