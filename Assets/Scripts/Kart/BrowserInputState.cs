using UnityEngine;

public static class BrowserInputState
{
    public static float Steering;
    public static float Throttle;
    public static bool DriftHeld;
    public static bool HandbrakeHeld;

    public static void Reset()
    {
        Steering = 0f;
        Throttle = 0f;
        DriftHeld = false;
        HandbrakeHeld = false;
    }

    public static void SetSteering(float value)
    {
        Steering = Mathf.Clamp(value, -1f, 1f);
    }

    public static void SetThrottle(float value)
    {
        Throttle = Mathf.Clamp(value, -1f, 1f);
    }
}
