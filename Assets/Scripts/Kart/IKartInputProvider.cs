public interface IKartInputProvider
{
    float Throttle { get; }
    float Steering { get; }
    bool DriftHeld { get; }
    bool HandbrakeHeld { get; }
}
