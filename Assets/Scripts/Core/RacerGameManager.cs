using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
README - RacerGameManager
How to Play:
1) Press Play in Unity.
2) Countdown runs (3,2,1,GO), then drive with W/S (throttle/brake), A/D (steer), Left Shift (drift), Space (strong brake).
3) Collect coins, pass checkpoints in order, and cross finish line each lap.
4) Complete required laps to finish; use Return on finish panel to emit result payload and restart scene.

Overworld Integration:
- This class implements IMiniGame.
- Call StartMiniGame(MiniGameConfig config) after scene load.
- Subscribe to OnMiniGameFinished to receive RacerResult payload.
- Current Return button invokes the event and reloads Racer_Main/active scene for test loop behavior.
*/
public class RacerGameManager : MonoBehaviour, IMiniGame
{
    public event Action<RacerResult> OnMiniGameFinished;

    public event Action<int, int> OnLapDisplayChanged;
    public event Action<int> OnCoinsChanged;
    public event Action<float> OnTimerChanged;
    public event Action<string, float> OnCountdownChanged;
    public event Action<RacerResult> OnRaceFinished;

    [SerializeField] private MiniGameConfig startupConfig = new MiniGameConfig { raceId = "race_default", difficulty = 1, laps = 3, seed = 0 };

    private MiniGameConfig _activeConfig;
    private KartController _playerKart;
    private RacerLapTracker _lapTracker;
    private RacerHUD _hud;

    private bool _raceStarted;
    private bool _raceFinished;
    private float _raceTimer;
    private int _coins;
    private RacerResult _pendingResult;
    private bool _hasPendingResult;
    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;

    public void Initialize(RacerSceneBuildResult scene)
    {
        _playerKart = scene.playerKart;
        _lapTracker = scene.lapTracker;
        _hud = scene.hud;
        if (_playerKart != null)
        {
            _spawnPosition = _playerKart.transform.position;
            _spawnRotation = _playerKart.transform.rotation;
        }

        if (_lapTracker != null)
        {
            _lapTracker.OnLapAdvanced += HandleLapAdvanced;
            _lapTracker.OnRaceComplete += HandleRaceComplete;
        }

        if (_hud != null)
        {
            _hud.Bind(this);
        }

        StartMiniGame(startupConfig);
    }

    public void StartMiniGame(MiniGameConfig config)
    {
        _activeConfig = config.Sanitized();
        UnityEngine.Random.InitState(_activeConfig.seed);

        _raceStarted = false;
        _raceFinished = false;
        _raceTimer = 0f;
        _coins = 0;
        _hasPendingResult = false;
        BrowserInputState.Reset();

        if (_playerKart != null)
        {
            _playerKart.SetInputEnabled(false);
            _playerKart.ResetKartPhysics();
            _playerKart.transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
        }

        if (_lapTracker != null)
        {
            _lapTracker.Configure(_activeConfig.laps);
        }
        _hud?.ResetForRace();

        OnCoinsChanged?.Invoke(_coins);
        OnTimerChanged?.Invoke(_raceTimer);
        OnLapDisplayChanged?.Invoke(1, _activeConfig.laps);

        StopAllCoroutines();
        StartCoroutine(BeginRaceCountdown());
    }

    private void Update()
    {
        if (_raceStarted && !_raceFinished)
        {
            _raceTimer += Time.deltaTime;
            OnTimerChanged?.Invoke(_raceTimer);
        }
    }

    public void AddCoin()
    {
        if (_raceFinished)
        {
            return;
        }

        _coins++;
        OnCoinsChanged?.Invoke(_coins);
    }

    public void ReturnToOverworldMock()
    {
        if (!_raceFinished || !_hasPendingResult)
        {
            return;
        }

        OnMiniGameFinished?.Invoke(_pendingResult);
        var scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
            return;
        }

        ReactivateCoins();
        StartMiniGame(_activeConfig);
    }

    private IEnumerator BeginRaceCountdown()
    {
        yield return ShowCountdown("3", 1f);
        yield return ShowCountdown("2", 1f);
        yield return ShowCountdown("1", 1f);

        _raceStarted = true;
        if (_playerKart != null)
        {
            _playerKart.SetInputEnabled(true);
        }

        yield return ShowCountdown("GO!", 0.85f);
    }

    private IEnumerator ShowCountdown(string text, float duration)
    {
        OnCountdownChanged?.Invoke(text, duration);
        yield return new WaitForSeconds(duration);
    }

    private void HandleLapAdvanced(int completedLaps, int requiredLaps)
    {
        var displayLap = Mathf.Clamp(completedLaps + 1, 1, requiredLaps);
        OnLapDisplayChanged?.Invoke(displayLap, requiredLaps);
    }

    private void HandleRaceComplete()
    {
        if (_raceFinished)
        {
            return;
        }

        _raceFinished = true;
        _raceStarted = false;

        if (_playerKart != null)
        {
            _playerKart.SetInputEnabled(false);
        }

        _pendingResult = new RacerResult
        {
            won = true,
            place = 1,
            totalRacers = 1,
            coinsCollected = _coins,
            finishTimeSeconds = _raceTimer,
            xpEarned = CalculateXp(_coins, _raceTimer, _activeConfig.difficulty),
            raceId = _activeConfig.raceId
        };
        _hasPendingResult = true;
        OnRaceFinished?.Invoke(_pendingResult);
    }

    private static int CalculateXp(int coins, float finishTime, int difficulty)
    {
        var timeBonus = Mathf.Max(0, Mathf.RoundToInt(180f - finishTime));
        return 50 + (coins * 3) + (difficulty * 20) + timeBonus;
    }

    private static void ReactivateCoins()
    {
        var coins = FindObjectsByType<CoinPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < coins.Length; i++)
        {
            coins[i].gameObject.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (_lapTracker != null)
        {
            _lapTracker.OnLapAdvanced -= HandleLapAdvanced;
            _lapTracker.OnRaceComplete -= HandleRaceComplete;
        }
    }
}
