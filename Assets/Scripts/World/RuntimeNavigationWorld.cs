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
    private List<NavMeshBuildSource> staticSources;
    private Bounds buildBounds;
    private NavMeshBuildSettings buildSettings;
    private AsyncOperation saltUpdate;
    private bool saltDirty;

    private void OnEnable() { SaltPile.Changed += MarkSaltDirty; HidingZone.Changed += MarkSaltDirty; MarkSaltDirty(); }
    private void OnDisable() { SaltPile.Changed -= MarkSaltDirty; HidingZone.Changed -= MarkSaltDirty; }
    private void MarkSaltDirty() { saltDirty = true; }
    private void Update()
    {
        if (data == null || !saltDirty || (saltUpdate != null && !saltUpdate.isDone)) return;
        saltDirty = false;
        saltUpdate = NavMeshBuilder.UpdateNavMeshDataAsync(data, buildSettings, BuildSources(), buildBounds);
    }
    private List<NavMeshBuildSource> BuildSources()
    {
        var sources = new List<NavMeshBuildSource>(staticSources);
        int area = NavMesh.GetAreaFromName("Salt");
        foreach (SaltPile pile in SaltPile.Active)
        {
            if (area < 0 || pile == null || !pile.IsIntact || !buildBounds.Contains(pile.transform.position)) continue;
            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.ModifierBox,
                area = area,
                size = new Vector3(pile.Radius * 2f, 1f, pile.Radius * 2f),
                transform = Matrix4x4.TRS(pile.transform.position + Vector3.up * 0.2f,
                    Quaternion.identity, Vector3.one)
            });
        }
        int hidingArea = NavMesh.GetAreaFromName("Hiding");
        if (hidingArea >= 0)
            foreach (HidingZone zone in HidingZone.Active)
            {
                if (zone == null || !buildBounds.Contains(zone.transform.position)) continue;
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.ModifierBox, area = hidingArea,
                    size = new Vector3(zone.Volume.size.x, 1f, zone.Volume.size.z),
                    transform = zone.transform.localToWorldMatrix * Matrix4x4.Translate(new Vector3(
                        zone.Volume.center.x, zone.Volume.center.y - zone.Volume.size.y * 0.5f + 0.2f, zone.Volume.center.z))
                });
            }
        return sources;
    }

    private void Awake()
    {
        var sources = new List<NavMeshBuildSource>();
        Bounds bounds = default;
        bool hasBounds = false;
        foreach (Collider collider in geometry ?? new Collider[0])
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
        settings.minRegionArea = 0.1f; // Preserve small wardrobe interiors.
        settings.overrideVoxelSize = true;
        settings.voxelSize = 0.1f;
        staticSources = sources;
        buildBounds = bounds;
        buildSettings = settings;
        data = NavMeshBuilder.BuildNavMeshData(settings, BuildSources(), bounds, Vector3.zero, Quaternion.identity);
        if (data == null)
        {
            Debug.LogError("RuntimeNavigationWorld: NavMesh build failed.", this);
            return;
        }
        instance = NavMesh.AddNavMeshData(data);
        foreach (BoxCollider box in movingObstacles ?? new BoxCollider[0])
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
        if (data != null && saltUpdate != null && !saltUpdate.isDone) NavMeshBuilder.Cancel(data);
        if (instance.valid) instance.Remove();
        if (data != null) Destroy(data);
        foreach (NavMeshObstacle obstacle in ownedObstacles)
            if (obstacle != null) Destroy(obstacle);
    }
}
