using UnityEngine;

public static class RuntimeVisuals
{
    public static GameObject Primitive(PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        GameObject visual = GameObject.CreatePrimitive(type);
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = position;
        visual.transform.localScale = scale;
        Collider collider = visual.GetComponent<Collider>();
        collider.enabled = false;
        Object.Destroy(collider);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material material = shader != null ? new Material(shader) :
            new Material(visual.GetComponent<Renderer>().sharedMaterial);
        material.color = color;
        visual.GetComponent<Renderer>().sharedMaterial = material;
        visual.AddComponent<RuntimeMaterialOwner>().Material = material;
        return visual;
    }
}
