using UnityEngine;

public class RuntimeMaterialOwner : MonoBehaviour
{
    public Material Material;
    private void OnDestroy() { if (Material != null) Destroy(Material); }
}
