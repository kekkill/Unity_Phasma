using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerItemHolder))]
public class PlayerItemController : MonoBehaviour
{
    private PlayerInputReader inputReader;
    private PlayerItemHolder itemHolder;

    private void Awake()
    {
        inputReader =
            GetComponent<PlayerInputReader>();

        itemHolder =
            GetComponent<PlayerItemHolder>();
    }

    private void Update()
    {
        if (inputReader.UsePressedThisFrame)
        {
            itemHolder.UseCurrentItem(gameObject);
        }

        if (inputReader.DropPressedThisFrame)
        {
            itemHolder.DropCurrentItem();
        }
    }
}