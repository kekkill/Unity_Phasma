using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(GameSession))]
public class RoundEndScreen : MonoBehaviour
{
    private GameSession session;
    private void OnEnable()
    {
        session = GetComponent<GameSession>();
        session.AllPlayersDead += Show;
    }
    private void OnDisable()
    {
        if (session != null) session.AllPlayersDead -= Show;
    }
    private void Show()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    private void OnGUI()
    {
        if (session == null || !session.HasEnded) return;
        Rect panel = new Rect((Screen.width - 360f) / 2f, (Screen.height - 150f) / 2f, 360f, 150f);
        GUI.Box(panel, "Раунд завершён");
        GUI.Label(new Rect(panel.x + 25f, panel.y + 45f, 310f, 30f), "Все игроки погибли.");
        if (GUI.Button(new Rect(panel.x + 80f, panel.y + 90f, 200f, 35f), "Начать заново"))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
