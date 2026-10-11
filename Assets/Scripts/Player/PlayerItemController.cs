using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerItemHolder))]
[DefaultExecutionOrder(-60)] // After journal blocking, before placement can consume F.
public class PlayerItemController : MonoBehaviour
{
    private PlayerInputReader inputReader;
    private PlayerItemHolder itemHolder;
    private PlayerItemPlacement placement;

    private void Awake()
    {
        placement = GetComponent<PlayerItemPlacement>();
        inputReader =
            GetComponent<PlayerInputReader>();

        itemHolder =
            GetComponent<PlayerItemHolder>();
    }

    private void Update()
    {
        if (inputReader.GameplayBlocked || !itemHolder.Inventory.CanModify) return;
        int slot = inputReader.RequestedSlotThisFrame;
        if (slot >= 0 && TrySelectSlot(slot)) return;
        if (inputReader.DropPressedThisFrame)
        {
            if (placement != null) placement.Cancel();
            itemHolder.DropCurrentItem();
            inputReader.ConsumeItemInputForFrame();
            return;
        }
    }

    // Placement has already confirmed/cancelled during Update. A first F on salt
    // opens its preview here, without also confirming that preview in the same frame.
    private void LateUpdate()
    {
        if (inputReader.GameplayBlocked || !itemHolder.Inventory.CanModify) return;
        if (inputReader.UsePressedThisFrame &&
            (placement == null || (!placement.IsPlacing && !placement.ConsumedUseThisFrame)))
        {
            itemHolder.UseCurrentItem(gameObject);
        }

    }

    public bool TrySelectSlot(int slot)
    {
        if (!isActiveAndEnabled || inputReader.GameplayBlocked || !itemHolder.Inventory.SelectSlot(slot)) return false;
        if (placement != null) placement.Cancel();
        // 1/2/3 together with F/R/E/Q must not act on the newly selected item.
        inputReader.ConsumeItemInputForFrame();
        return true;
    }
}
