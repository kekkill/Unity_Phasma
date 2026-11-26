using System.Collections;
using UnityEngine;

public sealed class HubPhone : MonoBehaviour
{
    [SerializeField] private PlayerInputReader playerInput;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Collider phoneBody;
    [SerializeField] private AudioClip ring;
    [SerializeField] private MainMenuScreen menu;
    [SerializeField] private Transform preparationArrival;
    [SerializeField] private Transform hubArrival;

    private const float Reach = 2.4f;
    private const float RingDuration = 2f;
    private const float FadeDuration = 0.34f;
    private string prompt;
    private bool busy;
    private bool inPreparation;
    private float blackout;

    private void Update()
    {
        if (busy)
        {
            if (playerInput.CloseJournalPressedThisFrame && menu.IsOpen)
                menu.CloseToHub();
            return;
        }

        prompt = null;
        Transform view = playerCamera.transform;
        if (Cursor.lockState != CursorLockMode.Locked ||
            !Physics.Raycast(view.position, view.forward, out RaycastHit hit, Reach,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        if (!inPreparation && hit.collider == phoneBody)
        {
            prompt = "Позвонить диспетчеру";
            if (playerInput.InteractPressedThisFrame)
                StartCoroutine(Travel(preparationArrival, true));
            return;
        }

        if (!inPreparation || !hit.collider.TryGetComponent(out HubStation station)) return;
        prompt = station.Prompt;
        if (!playerInput.InteractPressedThisFrame) return;

        if (station.Kind == HubStation.StationKind.ReturnDoor)
            StartCoroutine(Travel(hubArrival, false));
        else
        {
            BlockExploration();
            menu.OpenStation(this, station.Kind);
        }
    }

    private void BlockExploration()
    {
        busy = true;
        prompt = null;
        playerInput.SetGameplayBlocked(this, true);
    }

    private IEnumerator Travel(Transform arrival, bool call)
    {
        BlockExploration();
        if (call)
        {
            if (ring != null) AudioSource.PlayClipAtPoint(ring, phoneBody.bounds.center, 0.8f);
            yield return new WaitForSecondsRealtime(RingDuration - FadeDuration);
        }

        yield return FadeTo(1f);
        var controller = playerInput.GetComponent<CharacterController>();
        controller.enabled = false;
        controller.transform.SetPositionAndRotation(arrival.position, arrival.rotation);
        playerInput.GetComponent<PlayerLook>().ResetPitch();
        controller.enabled = true;
        Physics.SyncTransforms();
        inPreparation = call;
        yield return FadeTo(0f);
        ResumeExploration();
    }

    private IEnumerator FadeTo(float target)
    {
        while (blackout != target)
        {
            blackout = Mathf.MoveTowards(blackout, target, Time.unscaledDeltaTime / FadeDuration);
            yield return null;
        }
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
        blackout = 0f;
        prompt = null;
        if (playerInput != null) playerInput.SetGameplayBlocked(this, false);
    }

    private void OnGUI()
    {
        if (blackout > 0f && Event.current.type == EventType.Repaint)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, blackout);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }
        if (busy || Cursor.lockState != CursorLockMode.Locked) return;

        var crosshair = new Rect(Screen.width / 2f - 9f, Screen.height / 2f - 12f, 18f, 24f);
        GUI.Label(crosshair, "  •");
        if (!string.IsNullOrEmpty(prompt))
        {
            var label = new Rect(Screen.width / 2f - 130f, Screen.height / 2f + 30f, 260f, 32f);
            GUI.Box(label, "[E] " + prompt);
        }
    }
}
