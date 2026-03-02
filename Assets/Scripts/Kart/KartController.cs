using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KartController : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float maxSpeed = 22f;
    [SerializeField] private float acceleration = 42f;
    [SerializeField] private float reverseSpeed = 9f;
    [SerializeField] private float brakeForce = 35f;

    [Header("Steering")]
    [SerializeField] private float lowSpeedSteer = 105f;
    [SerializeField] private float highSpeedSteer = 60f;
    [SerializeField] private float normalGrip = 8f;
    [SerializeField] private float driftGrip = 2.25f;
    [SerializeField] private float driftSteerMultiplier = 1.35f;

    [Header("Drift + Boost")]
    [SerializeField] private float driftChargeRate = 1.25f;
    [SerializeField] private float driftSlipThreshold = 10f;
    [SerializeField] private float boostTier1 = 0.75f;
    [SerializeField] private float boostTier2 = 1.25f;
    [SerializeField] private float boostTier3 = 1.9f;
    [SerializeField] private float boostSpeedBonus = 9f;

    [Header("Collision")]
    [SerializeField] private float wallSpeedLoss = 0.72f;
    [SerializeField] private float reboundStrength = 2f;

    [Header("VFX")]
    [SerializeField] private TrailRenderer boostTrail;

    private Rigidbody _rb;
    private IKartInputProvider _input;

    private bool _inputEnabled;
    private bool _drifting;
    private float _driftCharge;
    private float _boostTimer;

    private bool _useOffRoadEllipse;
    private Vector2 _trackCenter;
    private Vector2 _innerRadii;
    private Vector2 _outerRadii;

    public float CurrentSpeed => Vector3.Dot(_rb.linearVelocity, transform.forward);

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        var providers = GetComponents<MonoBehaviour>();
        for (var i = 0; i < providers.Length; i++)
        {
            if (providers[i] is IKartInputProvider provider)
            {
                _input = provider;
                break;
            }
        }
        _rb.centerOfMass = new Vector3(0f, -0.4f, 0f);

        if (boostTrail != null)
        {
            boostTrail.emitting = false;
        }
    }

    private void FixedUpdate()
    {
        var throttle = _inputEnabled && _input != null ? _input.Throttle : 0f;
        var steering = _inputEnabled && _input != null ? _input.Steering : 0f;
        var driftHeld = _inputEnabled && _input != null && _input.DriftHeld;
        var handbrake = _inputEnabled && _input != null && _input.HandbrakeHeld;

        HandleDriftState(driftHeld);
        HandleMovement(throttle, steering, handbrake, EvaluateOffRoad());
        UpdateBoostState();
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        if (!enabled)
        {
            _drifting = false;
            _driftCharge = 0f;
        }
    }

    public void ConfigureOffRoadEllipse(Vector2 center, Vector2 innerRadii, Vector2 outerRadii)
    {
        _useOffRoadEllipse = true;
        _trackCenter = center;
        _innerRadii = innerRadii;
        _outerRadii = outerRadii;
    }

    public void ApplyPadBoost(float duration, float bonusSpeed)
    {
        _boostTimer = Mathf.Max(_boostTimer, duration);
        _rb.linearVelocity += transform.forward * bonusSpeed;
        if (boostTrail != null)
        {
            boostTrail.emitting = true;
        }
    }

    public void ResetKartPhysics()
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _boostTimer = 0f;
        _driftCharge = 0f;
        _drifting = false;
    }

    public void SetBoostTrail(TrailRenderer trail)
    {
        boostTrail = trail;
        if (boostTrail != null)
        {
            boostTrail.emitting = false;
        }
    }

    private void HandleMovement(float throttle, float steering, bool handbrake, bool onOffRoad)
    {
        var velocity = _rb.linearVelocity;
        var localVelocity = transform.InverseTransformDirection(velocity);
        var absForward = Mathf.Abs(localVelocity.z);

        var currentMaxSpeed = maxSpeed;
        var currentAcceleration = acceleration;

        if (onOffRoad)
        {
            currentMaxSpeed *= 0.6f;
            currentAcceleration *= 0.55f;
        }

        if (_boostTimer > 0f)
        {
            currentMaxSpeed += boostSpeedBonus;
        }

        if (throttle > 0f)
        {
            var speedRatio = Mathf.Clamp01(absForward / Mathf.Max(0.01f, currentMaxSpeed));
            var punchCurve = 1f - Mathf.Pow(speedRatio, 0.65f);
            _rb.AddForce(transform.forward * (throttle * currentAcceleration * punchCurve), ForceMode.Acceleration);
        }
        else if (throttle < -0.1f)
        {
            var reverseRatio = Mathf.Clamp01(Mathf.Abs(localVelocity.z) / reverseSpeed);
            var reverseCurve = 1f - reverseRatio;
            _rb.AddForce(transform.forward * (throttle * currentAcceleration * 0.8f * reverseCurve), ForceMode.Acceleration);
        }

        var braking = handbrake ? 1.5f : Mathf.Clamp01(-throttle);
        if (braking > 0f)
        {
            _rb.AddForce(-_rb.linearVelocity * (brakeForce * braking * 0.04f), ForceMode.Acceleration);
        }

        var speedForSteer = Mathf.Clamp01(absForward / currentMaxSpeed);
        var steerStrength = Mathf.Lerp(lowSpeedSteer, highSpeedSteer, speedForSteer);
        if (_drifting)
        {
            steerStrength *= driftSteerMultiplier;
        }

        var turnAmount = steering * steerStrength * Time.fixedDeltaTime;
        _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, turnAmount, 0f));

        var grip = _drifting ? driftGrip : normalGrip;
        localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, grip * Time.fixedDeltaTime);

        var clampedForward = Mathf.Clamp(localVelocity.z, -reverseSpeed, currentMaxSpeed);
        _rb.linearVelocity = transform.TransformDirection(new Vector3(localVelocity.x, _rb.linearVelocity.y, clampedForward));
    }

    private void HandleDriftState(bool driftHeld)
    {
        if (driftHeld)
        {
            _drifting = true;
            var flatVelocity = _rb.linearVelocity;
            flatVelocity.y = 0f;
            if (flatVelocity.sqrMagnitude > 1f)
            {
                var angle = Vector3.Angle(transform.forward, flatVelocity.normalized);
                if (angle > driftSlipThreshold)
                {
                    _driftCharge += driftChargeRate * Time.fixedDeltaTime;
                }
            }
            return;
        }

        if (_drifting)
        {
            ReleaseDriftBoost();
        }

        _drifting = false;
        _driftCharge = 0f;
    }

    private void ReleaseDriftBoost()
    {
        if (_driftCharge > boostTier3)
        {
            ApplyPadBoost(1.45f, boostSpeedBonus * 1.45f);
        }
        else if (_driftCharge > boostTier2)
        {
            ApplyPadBoost(1.0f, boostSpeedBonus * 1.1f);
        }
        else if (_driftCharge > boostTier1)
        {
            ApplyPadBoost(0.6f, boostSpeedBonus * 0.75f);
        }
    }

    private void UpdateBoostState()
    {
        if (_boostTimer > 0f)
        {
            _boostTimer -= Time.fixedDeltaTime;
            if (_boostTimer <= 0f && boostTrail != null)
            {
                boostTrail.emitting = false;
            }
        }
    }

    private bool EvaluateOffRoad()
    {
        if (!_useOffRoadEllipse)
        {
            return false;
        }

        var p = new Vector2(transform.position.x - _trackCenter.x, transform.position.z - _trackCenter.y);
        var innerEval = (p.x * p.x) / (_innerRadii.x * _innerRadii.x) + (p.y * p.y) / (_innerRadii.y * _innerRadii.y);
        var outerEval = (p.x * p.x) / (_outerRadii.x * _outerRadii.x) + (p.y * p.y) / (_outerRadii.y * _outerRadii.y);
        return innerEval < 1f || outerEval > 1f;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.GetComponent<WallSurface>() == null)
        {
            return;
        }

        var normal = collision.contacts[0].normal;
        var reflected = Vector3.Reflect(_rb.linearVelocity, normal) * wallSpeedLoss;
        reflected.y = _rb.linearVelocity.y;
        _rb.linearVelocity = reflected;
        _rb.AddForce(normal * reboundStrength, ForceMode.VelocityChange);
    }
}
