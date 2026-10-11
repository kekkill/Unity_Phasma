using UnityEngine;

// Runs before placement and gameplay controls, including on the frame J is pressed.
[DefaultExecutionOrder(-75)]
[RequireComponent(typeof(PlayerInputReader), typeof(PlayerLifeState), typeof(JournalState))]
public class PlayerJournalController : MonoBehaviour
{
    private PlayerInputReader input;
    private PlayerLifeState life;
    private JournalState journal;
    private PlayerItemPlacement placement;
    private CursorLockMode previousLock;
    private bool previousVisibility;
    public bool IsOpen { get; private set; }
    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        life = GetComponent<PlayerLifeState>();
        journal = GetComponent<JournalState>();
        placement = GetComponent<PlayerItemPlacement>();
    }
    private void OnEnable() { life.Died += OnDied; }
    private void OnDisable()
    {
        if (life != null) life.Died -= OnDied;
        Close();
    }
    private void OnDied(PlayerLifeState _) { Close(); }
    private void Update()
    {
        if (!life.IsAlive || (life.Session != null && life.Session.HasEnded)) { Close(); return; }
        if (IsOpen && input.CloseJournalPressedThisFrame) Close();
        else if (input.JournalPressedThisFrame) { if (IsOpen) Close(); else Open(); }
    }
    public bool Open()
    {
        if (!isActiveAndEnabled || !life.IsAlive || (life.Session != null && life.Session.HasEnded)) return false;
        if (IsOpen) return true;
        if (journal.Catalog == null || !journal.Catalog.TryValidate(out _))
        {
            Debug.LogWarning("Assign a valid Journal Catalog to this player's JournalState.", this);
            return false;
        }
        if (placement != null) placement.Cancel();
        previousLock = Cursor.lockState;
        previousVisibility = Cursor.visible;
        input.SetGameplayBlocked(this, true);
        IsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        return true;
    }
    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        input.SetGameplayBlocked(this, false);
        if (life.IsAlive && (life.Session == null || !life.Session.HasEnded))
        {
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisibility;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
