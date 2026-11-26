using UnityEngine;

// One low-resolution feed, sampled only while the player is using the monitor.
public sealed class ArchiveMonitor : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform[] viewpoints;
    private PlayerInputReader viewer;
    private Camera feed;
    private RenderTexture picture;
    private int channel;
    private float nextFrame;

    public string InteractionPrompt => "[E] Посмотреть камеры";

    public void Interact(GameObject interactor)
    {
        if (viewer != null || viewpoints.Length == 0 ||
            !interactor.TryGetComponent(out PlayerInputReader input)) return;

        if (feed == null)
        {
            picture = new RenderTexture(320, 180, 16);
            var cameraObject = new GameObject("Archive security feed");
            cameraObject.transform.SetParent(transform, false);
            feed = cameraObject.AddComponent<Camera>();
            feed.enabled = false;
            feed.targetTexture = picture;
            feed.farClipPlane = 30f;
            feed.allowHDR = false;
            feed.allowMSAA = false;
        }

        viewer = input;
        viewer.SetGameplayBlocked(this, true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetChannel(channel);
    }

    private void SetChannel(int index)
    {
        channel = index;
        Transform point = viewpoints[channel];
        feed.transform.SetPositionAndRotation(point.position, point.rotation);
        nextFrame = 0f;
    }

    private void LateUpdate()
    {
        if (feed == null) return;
        feed.enabled = false;
        if (viewer == null) return;
        if (viewer.CloseJournalPressedThisFrame)
        {
            Close();
            return;
        }

        if (Time.unscaledTime < nextFrame) return;
        feed.enabled = true;
        nextFrame = Time.unscaledTime + 0.2f;
    }

    private void Close()
    {
        if (feed != null) feed.enabled = false;
        if (viewer == null) return;
        viewer.SetGameplayBlocked(this, false);
        viewer = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable() => Close();

    private void OnDestroy()
    {
        if (picture != null) Destroy(picture);
    }

    private void OnGUI()
    {
        if (viewer == null || picture == null) return;
        float width = Mathf.Min(620f, Screen.width - 32f, (Screen.height - 100f) * 16f / 9f);
        float height = width * 9f / 16f;
        float left = (Screen.width - width) * 0.5f;
        float top = (Screen.height - height) * 0.5f;
        GUI.Box(new Rect(left - 8f, top - 34f, width + 16f, height + 78f), $"Камера {channel + 1}/{viewpoints.Length}");
        GUI.DrawTexture(new Rect(left, top, width, height), picture);
        if (GUI.Button(new Rect(left, top + height + 8f, width * 0.48f, 27f), "Следующая камера"))
            SetChannel((channel + 1) % viewpoints.Length);
        if (GUI.Button(new Rect(left + width * 0.52f, top + height + 8f, width * 0.48f, 27f), "Закрыть"))
            Close();
    }
}
