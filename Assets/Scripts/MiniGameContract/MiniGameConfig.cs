using System;
using UnityEngine;

[Serializable]
public struct MiniGameConfig
{
    public string raceId;
    [Range(1, 3)] public int difficulty;
    [Min(1)] public int laps;
    public int seed;

    public static MiniGameConfig Default()
    {
        return new MiniGameConfig
        {
            raceId = "race_default",
            difficulty = 1,
            laps = 3,
            seed = Environment.TickCount
        };
    }

    public MiniGameConfig Sanitized()
    {
        return new MiniGameConfig
        {
            raceId = string.IsNullOrWhiteSpace(raceId) ? "race_default" : raceId,
            difficulty = Mathf.Clamp(difficulty <= 0 ? 1 : difficulty, 1, 3),
            laps = Mathf.Max(1, laps <= 0 ? 3 : laps),
            seed = seed == 0 ? Environment.TickCount : seed
        };
    }
}
