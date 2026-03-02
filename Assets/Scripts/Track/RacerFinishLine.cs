using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RacerFinishLine : MonoBehaviour
{
    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var lapTracker = other.GetComponentInParent<RacerLapTracker>();
        if (lapTracker == null)
        {
            return;
        }

        lapTracker.RegisterFinishLineCross();
    }
}
