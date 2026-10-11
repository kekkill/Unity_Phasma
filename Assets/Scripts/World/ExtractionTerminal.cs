using UnityEngine;

public class ExtractionTerminal : MonoBehaviour, IInteractable
{
    [SerializeField] private GameSession session;
    [SerializeField] private SafeZone safeZone;
    private string message;
    private float messageUntil;
    public string InteractionPrompt => "[E] Завершить расследование";
    public void Interact(GameObject interactor)
    {
        PlayerLifeState life = interactor != null ? interactor.GetComponent<PlayerLifeState>() : null;
        if (session == null || life == null || life.Session != session || safeZone == null ||
            !safeZone.Contains(life.transform.position)) return;
        if (!session.TryExtract(life, out message)) messageUntil = Time.unscaledTime + 5f;
    }
    private void OnGUI()
    {
        if (session == null || session.HasEnded || Time.unscaledTime > messageUntil || string.IsNullOrEmpty(message)) return;
        GUI.Box(new Rect((Screen.width - 570f) / 2f, Screen.height - 95f, 570f, 65f), message);
    }
}
