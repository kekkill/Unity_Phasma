using UnityEngine;

// Scene wiring and introductory HUD; GameSession owns all completion decisions.
[DefaultExecutionOrder(-125)]
public class MissionDirector : MonoBehaviour
{
    [SerializeField] private GameSession session;
    [SerializeField] private GhostController ghost;
    [SerializeField] private JournalCatalog catalog;
    [SerializeField] private SafeZone safeZone;
    [SerializeField] private BuildingPower buildingPower;
    private bool configured;
    private void Start()
    {
        string reason = "Assign GameSession and GhostController.";
        configured = session != null && ghost != null && session.ConfigureMission(ghost.Profile, catalog, safeZone, out reason);
        if (!configured) Debug.LogError("MissionDirector: " + reason, this);
    }
    private void Update()
    {
        if (!configured || session.Phase != MissionPhase.Preparation) return;
        foreach (PlayerLifeState player in session.Participants)
            if (player != null && player.IsAlive && !safeZone.Contains(player.transform.position) &&
                ghost.FindRoomAt(player.transform.position) != null)
            { session.BeginInvestigation(player); break; }
    }
    private void OnGUI()
    {
        if (!configured || session.HasEnded) return;
        bool needsPower = buildingPower != null && !buildingPower.IsPowered;
        if (needsPower) GUI.Label(new Rect(30f, 88f, 465f, 24f), "Питание выключено. Щиток — в коридоре у входа (E).");
        GUI.Box(new Rect(15f, 15f, 490f, 72f), session.Phase == MissionPhase.Preparation ? "Подготовка" : "Расследование");
        GUI.Label(new Rect(30f, 42f, 465f, 40f), session.Phase == MissionPhase.Preparation ?
            "Возьмите оборудование и войдите в дом. J — журнал.\nСиняя пристройка — безопасная зона." :
            "Соберите улики и выберите тип в журнале (J).\nДля завершения вернитесь к терминалу в безопасной зоне.");
    }
}
