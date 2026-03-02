using System;
using UnityEngine;

public class RacerLapTracker : MonoBehaviour
{
    public event Action<int, int> OnLapAdvanced;
    public event Action OnRaceComplete;

    [SerializeField] private int requiredLaps = 3;
    [SerializeField] private int checkpointCount = 1;

    private int _completedLaps;
    private int _nextCheckpointIndex;
    private bool _armedForLap;

    public void Configure(int laps)
    {
        requiredLaps = Mathf.Max(1, laps);
        _completedLaps = 0;
        _nextCheckpointIndex = 0;
        _armedForLap = false;
    }

    public void SetCheckpointCount(int count)
    {
        checkpointCount = Mathf.Max(1, count);
        _nextCheckpointIndex = 0;
        _armedForLap = false;
    }

    public void RegisterCheckpoint(int checkpointIndex)
    {
        if (checkpointIndex != _nextCheckpointIndex)
        {
            return;
        }

        _nextCheckpointIndex++;
        if (_nextCheckpointIndex >= checkpointCount)
        {
            _nextCheckpointIndex = 0;
            _armedForLap = true;
        }
    }

    public void RegisterFinishLineCross()
    {
        if (!_armedForLap)
        {
            return;
        }

        _armedForLap = false;
        _completedLaps++;
        OnLapAdvanced?.Invoke(_completedLaps, requiredLaps);

        if (_completedLaps >= requiredLaps)
        {
            OnRaceComplete?.Invoke();
        }
    }
}
