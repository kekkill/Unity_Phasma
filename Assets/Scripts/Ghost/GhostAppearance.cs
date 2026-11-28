using UnityEngine;

public sealed class GhostAppearance : ScriptableObject
{
    [SerializeField] private string modelResource;
    [SerializeField] private Material material;
    [SerializeField, Min(0.01f)] private float walkSpeed = 0.6f;
    [SerializeField, Min(0.01f)] private float chaseSpeed = 1.6f;

    public string ModelResource => modelResource;
    public Material Material => material;
    public float WalkSpeed => walkSpeed;
    public float ChaseSpeed => chaseSpeed;
}
