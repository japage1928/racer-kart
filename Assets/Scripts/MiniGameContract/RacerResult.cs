using System;

[Serializable]
public struct RacerResult
{
    public bool won;
    public int place;
    public int totalRacers;
    public int coinsCollected;
    public float finishTimeSeconds;
    public int xpEarned;
    public string raceId;
}
