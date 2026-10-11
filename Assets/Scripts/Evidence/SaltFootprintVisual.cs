using UnityEngine;

// Placeholder foot shape, shared by the lasting pile imprint and the temporary trail.
public static class SaltFootprintVisual
{
    public static GameObject Create(Transform parent, Vector3 localPosition, Quaternion localRotation, Color color)
    {
        var root = new GameObject("Salt footprint");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = localRotation;
        RuntimeVisuals.Primitive(PrimitiveType.Cube, root.transform, Vector3.zero,
            new Vector3(0.11f, 0.008f, 0.19f), color);
        RuntimeVisuals.Primitive(PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, 0.13f),
            new Vector3(0.12f, 0.008f, 0.055f), color);
        return root;
    }
}
