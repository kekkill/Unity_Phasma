using System;
using UnityEngine;

// The house circuit is independent of portable equipment and the safe-area light.
[DefaultExecutionOrder(-275)]
public class BuildingPower : MonoBehaviour, IInteractable
{
    [SerializeField] private bool initiallyPowered;
    [SerializeField] private Renderer indicator;
    private MaterialPropertyBlock indicatorProperties;
    public bool IsPowered { get; private set; }
    public event Action<bool> PowerChanged;
    public string InteractionPrompt => IsPowered ? "[E] Выключить щиток" : "[E] Включить щиток";

    private void Awake()
    {
        indicatorProperties = new MaterialPropertyBlock();
        SetPowered(initiallyPowered);
    }
    public void Interact(GameObject interactor) => SetPowered(!IsPowered);
    public void SetPowered(bool value)
    {
        bool changed = IsPowered != value;
        IsPowered = value;
        if (indicator != null)
        {
            if (indicatorProperties == null) indicatorProperties = new MaterialPropertyBlock();
            Color color = value ? Color.green : Color.red;
            indicator.GetPropertyBlock(indicatorProperties);
            indicatorProperties.SetColor("_BaseColor", color);
            indicatorProperties.SetColor("_Color", color);
            indicator.SetPropertyBlock(indicatorProperties);
        }
        if (changed) PowerChanged?.Invoke(value);
    }
}
