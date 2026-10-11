using UnityEngine;

// Presentation reads SaltPile state. It does not decide which ghosts leave marks.
public class SaltPileVisual : MonoBehaviour
{
    private SaltPile pile;
    private GameObject surface;
    private GameObject imprint;
    private bool subscribed;
    public void Bind(SaltPile value)
    {
        Unsubscribe();
        pile = value;
        if (pile == null) return;
        if (surface == null)
        {
            surface = RuntimeVisuals.Primitive(PrimitiveType.Cylinder, transform,
                new Vector3(0f, 0.01f, 0f), Vector3.one, Color.white);
            surface.name = "Salt surface";
            imprint = new GameObject("Persistent salt imprint");
            imprint.transform.SetParent(transform, false);
            Color dark = new Color(0.12f, 0.08f, 0.16f);
            SaltFootprintVisual.Create(imprint.transform, new Vector3(-0.10f, 0f, 0.03f), Quaternion.identity, dark);
            SaltFootprintVisual.Create(imprint.transform, new Vector3(0.10f, 0f, -0.07f), Quaternion.identity, dark);
        }
        Subscribe();
        Refresh(pile);
    }
    private void OnEnable() { Subscribe(); if (pile != null) Refresh(pile); }
    private void OnDisable()
    {
        Unsubscribe();
        if (surface != null) surface.SetActive(false);
        if (imprint != null) imprint.SetActive(false);
    }
    private void Subscribe()
    {
        if (subscribed || pile == null || !isActiveAndEnabled) return;
        pile.StateChanged += Refresh; subscribed = true;
    }
    private void Unsubscribe()
    {
        if (subscribed && pile != null) pile.StateChanged -= Refresh;
        subscribed = false;
    }
    private void Refresh(SaltPile state)
    {
        if (surface == null || imprint == null) return;
        surface.SetActive(isActiveAndEnabled);
        surface.transform.localScale = state.IsIntact ?
            new Vector3(state.Radius * 2f, 0.015f, state.Radius * 2f) :
            new Vector3(state.Radius * 2.5f, 0.002f, state.Radius * 2.5f);
        imprint.transform.localPosition = state.ImprintLocalPosition;
        imprint.transform.localRotation = Quaternion.Euler(0f, state.ImprintLocalYaw, 0f);
        imprint.SetActive(isActiveAndEnabled && state.WasSteppedOn && state.IsIntact);
    }
}
