using UnityEngine;

public class FlashlightItem : MonoBehaviour, IUsableItem
{
    [SerializeField] private Light flashlightLight;

    private bool isOn;

    private void Awake()
    {
        if (flashlightLight != null)
        {
            isOn = flashlightLight.enabled;
        }
    }

    public void Use(GameObject user)
    {
        isOn = !isOn;

        if (flashlightLight != null)
        {
            flashlightLight.enabled = isOn;
        }
    }
}