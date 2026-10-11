using UnityEngine;

// Thin trigger broad phase, circular refinement. This never blocks the player or agent.
[RequireComponent(typeof(BoxCollider))]
public class SaltContactArea : MonoBehaviour
{
    private SaltPile pile;
    private BoxCollider volume;
    public SaltPile Pile => pile;
    public void Configure(SaltPile owner)
    {
        pile = owner;
        volume = GetComponent<BoxCollider>();
        volume.isTrigger = true;
        volume.center = new Vector3(0f, 0.06f, 0f);
        volume.size = new Vector3(owner.Radius * 2f, 0.12f, owner.Radius * 2f);
    }
    public bool IntersectsFootPath(Vector3 from, Vector3 to, float footRadius, out Vector3 contact)
    {
        contact = default;
        if (!isActiveAndEnabled || pile == null || !pile.isActiveAndEnabled || volume == null || !volume.enabled) return false;
        Vector3 a = transform.InverseTransformPoint(from), b = transform.InverseTransformPoint(to);
        Vector2 segment = new Vector2(b.x - a.x, b.z - a.z);
        float t = segment.sqrMagnitude > 0.000001f ? Mathf.Clamp01(-Vector2.Dot(new Vector2(a.x, a.z), segment) / segment.sqrMagnitude) : 0f;
        Vector3 scale = transform.lossyScale;
        float horizontalRadius = footRadius / Mathf.Max(0.001f, Mathf.Min(Mathf.Abs(scale.x), Mathf.Abs(scale.z)));
        float verticalRadius = footRadius / Mathf.Max(0.001f, Mathf.Abs(scale.y));
        float low = volume.center.y - volume.size.y * 0.5f - verticalRadius;
        float high = volume.center.y + volume.size.y * 0.5f + verticalRadius;
        float minT = 0f, maxT = 1f;
        if (Mathf.Abs(b.y - a.y) > 0.000001f)
        {
            float first = (low - a.y) / (b.y - a.y), last = (high - a.y) / (b.y - a.y);
            minT = Mathf.Max(0f, Mathf.Min(first, last));
            maxT = Mathf.Min(1f, Mathf.Max(first, last));
            if (minT > maxT) return false;
        }
        else if (a.y < low || a.y > high) return false;
        Vector3 closest = Vector3.Lerp(a, b, Mathf.Clamp(t, minT, maxT));
        if (new Vector2(closest.x, closest.z).sqrMagnitude > Mathf.Pow(pile.Radius + horizontalRadius, 2f)) return false;
        contact = transform.TransformPoint(closest);
        return true;
    }
}
