using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerItemHolder))]
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
        if (inputReader.UsePressedThisFrame &&
            (placement == null || (!placement.IsPlacing && !placement.ConsumedUseThisFrame)))
        {
            itemHolder.UseCurrentItem(gameObject);
        }

        if (inputReader.DropPressedThisFrame)
        {
            itemHolder.DropCurrentItem();
        }
    }
}