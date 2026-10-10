using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Explicit geometry prevents room/item triggers and the player from entering the bake.
[DefaultExecutionOrder(-200)]
public class RuntimeNavigationWorld : MonoBehaviour
{
    [SerializeField] private Collider[] geometry;
    [SerializeField] private BoxCollider[] movingObstacles;
    private NavMeshData data;
    private NavMeshDataInstance instance;
    private readonly List<NavMeshObstacle> ownedObstacles = new();

    private void Awake()
    {
        var sources = new List<NavMeshBuildSource>();
        Bounds bounds = default;
        bool hasBounds = false;
        foreach (Collider collider in geometry)
        {
            if (collider == null || !collider.enabled || collider.isTrigger) continue;
            NavMeshBuildSource source = new NavMeshBuildSource { area = 0 };
            if (collider is BoxCollider box)
            {
                source.shape = NavMeshBuildSourceShape.Box;
                source.size = box.size;
                source.transform = box.transform.localToWorldMatrix * Matrix4x4.Translate(box.center);
            }
            else if (collider is MeshCollider mesh && mesh.sharedMesh != null)
            {
                source.shape = NavMeshBuildSourceShape.Mesh;
                source.sourceObject = mesh.sharedMesh;
                source.transform = mesh.transform.localToWorldMatrix;
            }
            else continue;
            sources.Add(source);
            if (!hasBounds) { bounds = collider.bounds; hasBounds = true; }
            else bounds.Encapsulate(collider.bounds);
        }
        if (!hasBounds)
        {
            Debug.LogError("RuntimeNavigationWorld: assign floor and wall colliders.", this);
            return;
        }
        bounds.Expand(4f);
        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
        settings.agentRadius = 0.35f;
        settings.agentHeight = 1.8f;
        settings.overrideVoxelSize = true;
        settings.voxelSize = 0.1f;
        data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
        if (data == null)
        {
            Debug.LogError("RuntimeNavigationWorld: NavMesh build failed.", this);
            return;
        }
        instance = NavMesh.AddNavMeshData(data);
        foreach (BoxCollider box in movingObstacles)
        {
            if (box == null || box.GetComponent<NavMeshObstacle>() != null) continue;
            NavMeshObstacle obstacle = box.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = box.center;
            obstacle.size = box.size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
            ownedObstacles.Add(obstacle);
        }
    }

    private void OnDestroy()
    {
        if (instance.valid) instance.Remove();
        if (data != null) Destroy(data);
        foreach (NavMeshObstacle obstacle in ownedObstacles)
            if (obstacle != null) Destroy(obstacle);
    }
}
