using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BoostPad : MonoBehaviour
{
    [SerializeField] private float boostDuration = 0.9f;
    [SerializeField] private float bonusSpeed = 8f;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var kart = other.GetComponentInParent<KartController>();
        if (kart == null)
        {
            return;
        }

        kart.ApplyPadBoost(boostDuration, bonusSpeed);
    }
}
