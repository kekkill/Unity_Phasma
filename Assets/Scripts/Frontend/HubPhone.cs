using System.Collections;
using UnityEngine;

public sealed class HubPhone : MonoBehaviour
{
    [SerializeField] private PlayerInputReader playerInput;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Collider phoneBody;
    [SerializeField] private AudioClip ring;
    [SerializeField] private MainMenuScreen menu;

    private const float Reach = 2.4f;
    private const float RingDuration = 2f;
    private bool aimedAtPhone;
    private bool busy;

    private void Update()
    {
        if (busy)
        {
            if (playerInput.CloseJournalPressedThisFrame && menu.IsOpen)
                menu.CloseToHub();
            return;
        }

        Transform view = playerCamera.transform;
        aimedAtPhone = Cursor.lockState == CursorLockMode.Locked &&
            Physics.Raycast(view.position, view.forward, out RaycastHit hit, Reach,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
            hit.collider == phoneBody;

        if (aimedAtPhone && playerInput.InteractPressedThisFrame)
            StartCoroutine(CallDispatch());
    }

    private IEnumerator CallDispatch()
    {
        busy = true;
        aimedAtPhone = false;
        playerInput.SetGameplayBlocked(this, true);
        if (ring != null)
            AudioSource.PlayClipAtPoint(ring, phoneBody.bounds.center, 0.8f);
        yield return new WaitForSecondsRealtime(RingDuration);
        menu.OpenFromPhone(this);
    }

    public void ResumeExploration()
    {
        busy = false;
        playerInput.SetGameplayBlocked(this, false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        busy = false;
        aimedAtPhone = false;
        if (playerInput != null) playerInput.SetGameplayBlocked(this, false);
    }

    private void OnGUI()
    {
        if (busy || Cursor.lockState != CursorLockMode.Locked) return;

        var crosshair = new Rect(Screen.width / 2f - 9f, Screen.height / 2f - 12f, 18f, 24f);
        GUI.Label(crosshair, "  •");
        if (aimedAtPhone)
        {
            var prompt = new Rect(Screen.width / 2f - 110f, Screen.height / 2f + 30f, 220f, 32f);
            GUI.Box(prompt, "[E] Позвонить диспетчеру");
        }
    }
}
