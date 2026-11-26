using UnityEngine;

internal static class EquipmentCollisionLayers
{
    private static bool checkedSettings;
    private static bool configured;
    private static int actorLayer;
    private static int equipmentLayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => checkedSettings = false;

    public static bool TryGet(out int actor, out int equipment)
    {
        if (!checkedSettings)
        {
            actorLayer = LayerMask.NameToLayer("ActorBody");
            equipmentLayer = LayerMask.NameToLayer("Equipment");
            configured = actorLayer >= 0 && equipmentLayer >= 0 &&
                Physics.GetIgnoreLayerCollision(actorLayer, equipmentLayer);
            checkedSettings = true;
            if (!configured)
                Debug.LogWarning("Фильтр столкновений снаряжения отключён. Обновите ProjectSettings/TagManager.asset и ProjectSettings/DynamicsManager.asset из репозитория.");
        }

        actor = actorLayer;
        equipment = equipmentLayer;
        return configured;
    }
}
