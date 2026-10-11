using UnityEngine;

// Rules only: the sensor detects contact; pile and trail own their own state/presentation.
[RequireComponent(typeof(GhostController), typeof(GhostHuntController), typeof(GhostSaltContactSensor))]
[RequireComponent(typeof(SaltFootprintTrail))]
public class GhostSaltReaction : MonoBehaviour
{
    private GhostController controller;
    private GhostHuntController hunt;
    private GhostSaltContactSensor sensor;
    private SaltFootprintTrail trail;
    private void Awake()
    {
        controller = GetComponent<GhostController>();
        hunt = GetComponent<GhostHuntController>();
        sensor = GetComponent<GhostSaltContactSensor>();
        trail = GetComponent<SaltFootprintTrail>();
    }
    private void OnEnable() { sensor.Contacted += OnContact; }
    private void OnDisable() { if (sensor != null) sensor.Contacted -= OnContact; }
    private void OnContact(SaltContact contact)
    {
        SaltPile pile = contact.Pile;
        if (controller.Profile == null || pile == null || !pile.IsIntact) return;
        switch (controller.Profile.SaltResponse)
        {
            case GhostSaltResponse.AvoidUntilHunt:
                if (hunt.IsHunting) pile.Scatter();
                break;
            case GhostSaltResponse.LeaveFootprints:
                if (!contact.IsEntry) return;
                pile.MarkSteppedOn(contact.Position, transform.forward);
                trail.BeginTrail(sensor.FeetPosition);
                break;
            // Ignore: physical contact produces no observable salt reaction.
        }
    }
}
