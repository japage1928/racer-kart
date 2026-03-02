using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RacerCheckpoint : MonoBehaviour
{
    [SerializeField] private int checkpointIndex;

    public void SetIndex(int index)
    {
        checkpointIndex = Mathf.Max(0, index);
    }

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

        lapTracker.RegisterCheckpoint(checkpointIndex);
    }
}
