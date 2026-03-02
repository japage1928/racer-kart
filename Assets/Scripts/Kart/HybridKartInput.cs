using UnityEngine;

public class HybridKartInput : MonoBehaviour, IKartInputProvider
{
    public float Throttle
    {
        get
        {
            var keyboard = Input.GetAxisRaw("Vertical");
            return SelectDominant(keyboard, BrowserInputState.Throttle);
        }
    }

    public float Steering
    {
        get
        {
            var keyboard = Input.GetAxisRaw("Horizontal");
            return SelectDominant(keyboard, BrowserInputState.Steering);
        }
    }

    public bool DriftHeld => Input.GetKey(KeyCode.LeftShift) || BrowserInputState.DriftHeld;
    public bool HandbrakeHeld => Input.GetKey(KeyCode.Space) || BrowserInputState.HandbrakeHeld;

    private static float SelectDominant(float a, float b)
    {
        return Mathf.Abs(b) > Mathf.Abs(a) ? b : a;
    }
}
