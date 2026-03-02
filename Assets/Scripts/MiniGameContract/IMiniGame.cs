using System;

public interface IMiniGame
{
    void StartMiniGame(MiniGameConfig config);
    event Action<RacerResult> OnMiniGameFinished;
}
