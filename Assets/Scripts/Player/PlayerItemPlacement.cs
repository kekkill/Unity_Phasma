using UnityEngine;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(PlayerInputReader), typeof(PlayerItemHolder))]
public class PlayerItemPlacement : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField, Min(0.5f)] private float maxDistance = 3f;
    private PlayerInputReader input;
    private PlayerItemHolder holder;
    private GameObject preview;
    private PickupItem selected;
    private RaycastHit surface;
    private Vector3 position;
    private Quaternion rotation;
    private bool valid;
    private int consumedUseFrame = -1;
    public bool IsPlacing => preview != null;
    public bool ConsumedUseThisFrame => consumedUseFrame == Time.frameCount;

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        holder = GetComponent<PlayerItemHolder>();
    }
    public void Begin()
    {
        if (input.GameplayBlocked || holder.CurrentItem == null) return;
        Cancel();
        selected = holder.CurrentItem;
        preview = RuntimeVisuals.Primitive(PrimitiveType.Cube, null, Vector3.zero,
            new Vector3(0.3f, 0.02f, 0.3f), Color.red);
        preview.name = "Placement preview";
    }
    public void Cancel()
    {
        if (preview != null) Destroy(preview);
        preview = null;
        selected = null;
    }
    private void OnDisable() { Cancel(); }
    private void Update()
    {
        if (input.GameplayBlocked) { if (IsPlacing) Cancel(); return; }
        if (input.PlacePressedThisFrame)
        {
            if (IsPlacing) Cancel(); else Begin();
        }
        if (!IsPlacing) return;
        if (selected == null || selected != holder.CurrentItem || input.DropPressedThisFrame)
        { Cancel(); return; }
        valid = Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
            out surface, maxDistance, ~0, QueryTriggerInteraction.Ignore) && surface.normal.y > 0.9f;
        SaltContainer salt = selected.GetComponent<SaltContainer>();
        if (salt != null) valid &= salt.Portions > 0 && surface.collider != null &&
            surface.collider.GetComponent<RuntimeNavigationWorld>() != null;
        Vector3 forward = playerCamera.transform.forward;
        forward.y = 0f;
        rotation = forward.sqrMagnitude > 0.001f ? Quaternion.LookRotation(forward) : Quaternion.identity;
        position = surface.point + Vector3.up * (salt != null ? 0.02f : selected.PlacementLift);
        if (valid)
        {
            Vector3 half = salt != null ? new Vector3(0.65f, 0.05f, 0.65f) : selected.PlacementHalfSize;
            half = new Vector3(Mathf.Max(0.02f, Mathf.Abs(half.x)), Mathf.Max(0.02f, Mathf.Abs(half.y)), Mathf.Max(0.02f, Mathf.Abs(half.z)));
            Vector3 center = salt != null ? position : position + rotation * selected.PlacementCenter;
            foreach (Collider hit in Physics.OverlapBox(center, half * 0.95f, rotation, ~0, QueryTriggerInteraction.Ignore))
                if (hit != surface.collider && hit.GetComponentInParent<PlayerItemHolder>() != holder &&
                    hit.GetComponentInParent<PickupItem>() != selected) valid = false;
            if (salt != null)
                foreach (SaltPile pile in SaltPile.Active)
                    if (pile != null && pile.IsIntact && Vector3.Distance(pile.transform.position, position) < pile.Radius * 2f) valid = false;
        }
        preview.transform.SetPositionAndRotation(position, rotation);
        preview.transform.localScale = salt != null ? new Vector3(1.3f, 0.02f, 1.3f) :
            new Vector3(Mathf.Max(0.1f, selected.PlacementHalfSize.x * 2f), 0.02f, Mathf.Max(0.1f, selected.PlacementHalfSize.z * 2f));
        preview.GetComponent<Renderer>().sharedMaterial.color = valid ? Color.green : Color.red;
        if (input.UsePressedThisFrame)
        {
            consumedUseFrame = Time.frameCount;
            if (!valid) return;
            if (salt != null) salt.Pour(position);
            else holder.PlaceCurrentItem(position, rotation);
            Cancel();
        }
    }
    private void OnGUI()
    {
        if (input.GameplayBlocked) return;
        SaltContainer salt = holder != null && holder.CurrentItem != null ? holder.CurrentItem.GetComponent<SaltContainer>() : null;
        if (salt != null) GUI.Label(new Rect(20f, Screen.height - 90f, 650f, 25f), "Соль: " + salt.Portions + " порций. F или R — выбрать место");
        if (IsPlacing) GUI.Label(new Rect(20f, Screen.height - 65f, 650f, 45f),
            valid ? "F — установить / рассыпать соль     R — отменить" : "Недопустимое место. Наведитесь на свободный пол. R — отменить");
    }
}
