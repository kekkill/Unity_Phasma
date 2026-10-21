using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GameSession))]
public class RoundEndScreen : MonoBehaviour
{
    private GameSession session;
    private GameObject screen;
    private CanvasGroup controls;
    private Text status;
    private void OnEnable()
    {
        session = GetComponent<GameSession>();
        session.MissionEnded += Show;
        if (session.HasEnded) Show();
    }
    private void OnDisable()
    {
        if (session != null) session.MissionEnded -= Show;
        if (screen != null) Destroy(screen);
    }
    private void Show()
    {
        if (screen != null || session.Result == null) return;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        screen = MenuUi.Create(transform, "Миссия завершена", out RectTransform content, out controls);
        MissionResult result = session.Result;
        MenuUi.Label(content, result.Reason == MissionEndReason.AllPlayersDead ? "Все игроки погибли." :
            result.TeamSuccess ? "Вы вернулись и верно определили призрака." : "Вы вернулись, но версия оказалась неверной.");
        MenuUi.Label(content, "Настоящий тип: " + result.ActualGhostName);
        foreach (ParticipantMissionResult player in result.Participants)
        {
            MenuUi.Label(content, player.PlayerName + (player.Survived ? " — выжил" : " — погиб"));
            MenuUi.Label(content, "Версия: " + player.SelectedGhostName + (player.Correct ? " (верно)" : " (неверно)"), 20);
        }
        status = MenuUi.Label(content, "");
        MenuUi.Select(MenuUi.Button(content, "Начать заново", Retry));
        MenuUi.Button(content, "К подготовке миссии", ReturnToPreparation);
    }
    private void Retry() => ReportLoad(MissionFlow.TryRetry(out string reason), reason);
    private void ReturnToPreparation() => ReportLoad(MissionFlow.TryReturnToPreparation(out string reason), reason);
    private void ReportLoad(bool started, string reason)
    {
        controls.interactable = !started;
        status.text = started ? "Загрузка…" : reason;
    }
}
