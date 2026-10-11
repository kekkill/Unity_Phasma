using UnityEngine;

public class GhostDebugVisual : MonoBehaviour
{
    [SerializeField] private bool visible = true;
    private GameObject body;
    private void Awake()
    {
        body = RuntimeVisuals.Primitive(PrimitiveType.Capsule, transform,
            new Vector3(0f, 0.3f, 0f), new Vector3(0.5f, 0.9f, 0.5f), Color.cyan);
        body.name = "Debug ghost body";
    }
    private void Update() { if (body != null) body.SetActive(visible); }
    private void OnDisable() { if (body != null) body.SetActive(false); }
    private void OnDestroy() { if (body != null) Destroy(body); }
}
