using UnityEngine;
using UnityEngine.EventSystems;

public class BrowserHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum ControlType
    {
        Steer,
        Throttle,
        Drift,
        Handbrake
    }

    [SerializeField] private ControlType controlType;
    [SerializeField] private float analogValue = 0f;

    public void Configure(ControlType type, float value = 0f)
    {
        controlType = type;
        analogValue = value;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Apply(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Apply(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Apply(false);
    }

    private void Apply(bool pressed)
    {
        switch (controlType)
        {
            case ControlType.Steer:
                BrowserInputState.SetSteering(pressed ? analogValue : 0f);
                break;
            case ControlType.Throttle:
                BrowserInputState.SetThrottle(pressed ? analogValue : 0f);
                break;
            case ControlType.Drift:
                BrowserInputState.DriftHeld = pressed;
                break;
            case ControlType.Handbrake:
                BrowserInputState.HandbrakeHeld = pressed;
                break;
        }
    }
}
