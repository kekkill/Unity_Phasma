using UnityEngine;

public class GhostWritingBook : MonoBehaviour, IGhostInteractable
{
    [Header("Writing")]
    [SerializeField] private GameObject writingVisual;

    [Range(0f, 1f)]
    [SerializeField] private float writingChance = 1f;

    [Header("Debug")]
    [SerializeField] private bool hasWriting;

    public Vector3 GhostInteractionPosition =>
        transform.position;

    private void Awake()
    {
        if (writingVisual != null)
        {
            writingVisual.SetActive(
                hasWriting
            );
        }
    }

    public void GhostInteract(
        GhostInteractionContext context)
    {
        if (hasWriting)
        {
            return;
        }

        if (context.Profile == null)
        {
            Debug.LogWarning(
                "GhostWritingBook: Ghost Profile is missing."
            );

            return;
        }

        if (!context.Profile.HasWritingEvidence)
        {
            Debug.Log(
                "Ghost cannot leave writing evidence."
            );

            return;
        }

        float randomValue =
            Random.value;

        if (randomValue > writingChance)
        {
            Debug.Log(
                "Ghost interacted with the book but did not write."
            );

            return;
        }

        hasWriting =
            true;

        if (writingVisual != null)
        {
            writingVisual.SetActive(
                true
            );
        }

        Debug.Log(
            "Ghost wrote in the book."
        );
    }
}