using UnityEngine;

public sealed class ArchiveRouteInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private ArchiveMissionRoute route;
    private bool isCar;

    public string InteractionPrompt => isCar ? "[E] Сесть в машину" : route.PhonePrompt;

    public void BindCar(ArchiveMissionRoute mission)
    {
        route = mission;
        isCar = true;
    }

    public void Interact(GameObject interactor)
    {
        var life = interactor.GetComponent<PlayerLifeState>();
        if (isCar) route.BoardCar(life);
        else route.Call(life);
    }
}
