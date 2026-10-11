using UnityEngine;

[RequireComponent(typeof(PickupItem))]
public class SaltContainer : MonoBehaviour, IUsableItem
{
    [SerializeField, Min(0)] private int portions = 5;
    public int Portions => portions;
    public void Use(GameObject user)
    {
        PlayerItemPlacement placement = user.GetComponent<PlayerItemPlacement>();
        if (placement != null && portions > 0) placement.Begin();
    }
    public bool Pour(Vector3 point)
    {
        if (portions <= 0) return false;
        GameObject pile = new GameObject("Placed salt");
        pile.transform.position = point;
        pile.AddComponent<SaltPile>();
        portions--;
        return true;
    }
}
