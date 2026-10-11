using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(GameSession))]
public class RoundEndScreen : MonoBehaviour
{
    private GameSession session;
    private Vector2 scroll;
    private void OnEnable()
    {
        session = GetComponent<GameSession>();
        session.MissionEnded += Show;
        if (session.HasEnded) Show();
    }
    private void OnDisable()
    {
        if (session != null) session.MissionEnded -= Show;
    }
    private void Show()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    private void OnGUI()
    {
        if (session == null || session.Result == null) return;
        MissionResult result = session.Result;
        float width = Mathf.Min(520f, Screen.width - 30f), height = Mathf.Min(380f, Screen.height - 30f);
        Rect panel = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
        GUI.Box(panel, "Миссия завершена");
        GUILayout.BeginArea(new Rect(panel.x + 20f, panel.y + 35f, width - 40f, height - 95f));
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label(result.Reason == MissionEndReason.AllPlayersDead ? "Все игроки погибли." :
            result.TeamSuccess ? "Вы вернулись и верно определили призрака." : "Вы вернулись, но версия оказалась неверной.");
        GUILayout.Label("Настоящий тип: " + result.ActualGhostName);
        GUILayout.Space(15f);
        foreach (ParticipantMissionResult player in result.Participants)
        {
            GUILayout.Label(player.PlayerName + (player.Survived ? " — выжил" : " — погиб"));
            GUILayout.Label("Версия: " + player.SelectedGhostName + (player.Correct ? " (верно)" : " (неверно)"));
            GUILayout.Space(10f);
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        if (GUI.Button(new Rect(panel.x + (width - 200f) / 2f, panel.yMax - 50f, 200f, 35f), "Начать заново"))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
