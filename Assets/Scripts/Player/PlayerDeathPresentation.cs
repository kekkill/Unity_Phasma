using UnityEngine;

[RequireComponent(typeof(PlayerLifeState))]
public class PlayerDeathPresentation : MonoBehaviour
{
    [SerializeField] private Behaviour[] controlsToDisable = new Behaviour[0];
    [SerializeField] private Collider[] collisionsToDisable = new Collider[0];
    [SerializeField] private GameObject[] livingOnlyUI = new GameObject[0];
    private PlayerLifeState life;

    private void OnEnable()
    {
        life = GetComponent<PlayerLifeState>();
        life.Died += OnDied;
        if (!life.IsAlive) OnDied(life);
    }
    private void OnDisable()
    {
        if (life != null) life.Died -= OnDied;
    }
    private void OnDied(PlayerLifeState player)
    {
        PlayerItemHolder holder = GetComponent<PlayerItemHolder>();
        if (holder != null) holder.DropCurrentItem();
        foreach (Behaviour control in controlsToDisable)
            if (control != null) control.enabled = false;
        foreach (Collider collision in collisionsToDisable)
            if (collision != null) collision.enabled = false;
        foreach (GameObject ui in livingOnlyUI)
            if (ui != null) ui.SetActive(false);
        // Only this player's representation is hidden; dropped equipment remains in the world.
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        foreach (AudioSource source in GetComponentsInChildren<AudioSource>(true))
        {
            source.Stop();
            source.enabled = false;
        }
        // Keep the local camera/AudioListener alive. A later spectator system can take over.
    }
}
