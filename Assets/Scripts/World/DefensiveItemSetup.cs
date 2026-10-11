using UnityEngine;

// Small test-scene inventory. Replace these primitive visuals with prefabs later.
public class DefensiveItemSetup : MonoBehaviour
{
    [SerializeField] private Vector3 crossPosition = new Vector3(-6f, 0.05f, 1.6f);
    [SerializeField] private Vector3 saltPosition = new Vector3(-5.2f, 0.05f, 1.6f);
    private void Awake()
    {
        GameObject cross = Item("Cross", crossPosition, new Vector3(0.55f, 0.84f, 0.12f), new Vector3(0f, 0.42f, 0f));
        cross.AddComponent<CrossItem>();
        GameObject salt = Item("Salt container", saltPosition, new Vector3(0.3f, 0.4f, 0.3f), new Vector3(0f, 0.2f, 0f));
        salt.AddComponent<SaltContainer>();
        RuntimeVisuals.Primitive(PrimitiveType.Cylinder, salt.transform, new Vector3(0f, 0.2f, 0f),
            new Vector3(0.25f, 0.2f, 0.25f), Color.white);
    }
    private GameObject Item(string itemName, Vector3 position, Vector3 size, Vector3 center)
    {
        var item = new GameObject(itemName);
        item.transform.position = position;
        // They become scene roots: dropping/placing must not retain a player or setup parent.
        BoxCollider box = item.AddComponent<BoxCollider>();
        box.size = size;
        box.center = center;
        item.AddComponent<Rigidbody>();
        item.AddComponent<PickupItem>();
        return item;
    }
}
