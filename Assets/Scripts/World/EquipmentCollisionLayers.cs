using UnityEngine;

internal static class EquipmentActorContacts
{
    public static void RefreshEquipment(PickupItem item)
    {
        foreach (var player in Object.FindObjectsByType<PlayerMovement>())
            item.IgnoreActorContacts(player.GetComponent<CharacterController>());

        foreach (var ghost in Object.FindObjectsByType<GhostNavigation>())
            foreach (var body in ghost.GetComponentsInChildren<Collider>())
                item.IgnoreActorContacts(body);
    }

    public static void RefreshActor(Collider body)
    {
        if (body == null || !body.enabled || body.isTrigger) return;
        foreach (var item in Object.FindObjectsByType<PickupItem>())
            item.IgnoreActorContacts(body);
    }
}
