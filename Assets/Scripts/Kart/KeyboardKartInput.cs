using UnityEngine;

public class KeyboardKartInput : MonoBehaviour, IKartInputProvider
{
    public float Throttle => Input.GetAxisRaw("Vertical");
    public float Steering => Input.GetAxisRaw("Horizontal");
    public bool DriftHeld => Input.GetKey(KeyCode.LeftShift);
    public bool HandbrakeHeld => Input.GetKey(KeyCode.Space);
}
