using System;
using System.Collections.Generic;
using UnityEngine;

public class SaltPile : MonoBehaviour
{
    private static List<SaltPile> active = new List<SaltPile>();
    public static IReadOnlyList<SaltPile> Active => active;
    public static event Action Changed;
    [SerializeField, Min(0.1f)] private float radius = 0.65f;
    [SerializeField] private bool scattered;
    private GameObject visual;
    public float Radius => radius;
    public bool IsIntact => !scattered;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() { active = new List<SaltPile>(); Changed = null; }
    private void Awake()
    {
        visual = RuntimeVisuals.Primitive(PrimitiveType.Cylinder, transform,
            new Vector3(0f, 0.01f, 0f), new Vector3(radius * 2f, 0.015f, radius * 2f), Color.white);
    }
    private void OnEnable() { if (!active.Contains(this)) active.Add(this); Changed?.Invoke(); }
    private void OnDisable() { active.Remove(this); Changed?.Invoke(); }
    public bool Contains(Vector3 point)
    {
        Vector3 delta = point - transform.position;
        return Mathf.Abs(delta.y) < 2f && new Vector2(delta.x, delta.z).sqrMagnitude <= radius * radius;
    }
    public void Scatter()
    {
        if (scattered) return;
        scattered = true;
        if (visual != null) visual.transform.localScale = new Vector3(radius * 2.5f, 0.002f, radius * 2.5f);
        Changed?.Invoke();
        Debug.Log("Ghost scattered salt during hunt", this);
    }
}
