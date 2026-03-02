using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RacerHUD : MonoBehaviour
{
    [SerializeField] private Text timerText;
    [SerializeField] private Text coinsText;
    [SerializeField] private Text lapText;
    [SerializeField] private Text countdownText;
    [SerializeField] private GameObject finishPanel;
    [SerializeField] private Text finishSummaryText;
    [SerializeField] private Button returnButton;
    [SerializeField] private RawImage minimapImage;

    private RacerGameManager _manager;
    private Coroutine _countdownRoutine;

    public void Initialize(
        Text timer,
        Text coins,
        Text lap,
        Text countdown,
        GameObject finish,
        Text finishSummary,
        Button retBtn,
        RawImage minimap)
    {
        timerText = timer;
        coinsText = coins;
        lapText = lap;
        countdownText = countdown;
        finishPanel = finish;
        finishSummaryText = finishSummary;
        returnButton = retBtn;
        minimapImage = minimap;
    }

    public void Bind(RacerGameManager manager)
    {
        _manager = manager;
        _manager.OnTimerChanged += HandleTimerChanged;
        _manager.OnCoinsChanged += HandleCoinsChanged;
        _manager.OnLapDisplayChanged += HandleLapChanged;
        _manager.OnCountdownChanged += HandleCountdownChanged;
        _manager.OnRaceFinished += HandleRaceFinished;
        returnButton.onClick.AddListener(_manager.ReturnToOverworldMock);
    }

    public void SetMinimapTexture(RenderTexture texture)
    {
        minimapImage.texture = texture;
    }

    public void ResetForRace()
    {
        if (finishPanel != null)
        {
            finishPanel.SetActive(false);
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private void HandleTimerChanged(float timeSeconds)
    {
        var minutes = Mathf.FloorToInt(timeSeconds / 60f);
        var seconds = Mathf.FloorToInt(timeSeconds % 60f);
        var milliseconds = Mathf.FloorToInt((timeSeconds * 1000f) % 1000f);
        timerText.text = $"{minutes:00}:{seconds:00}:{milliseconds:000}";
    }

    private void HandleCoinsChanged(int coins)
    {
        coinsText.text = $"Coins {coins}";
    }

    private void HandleLapChanged(int lap, int total)
    {
        lapText.text = $"Lap {lap}/{total}";
    }

    private void HandleCountdownChanged(string value, float duration)
    {
        if (_countdownRoutine != null)
        {
            StopCoroutine(_countdownRoutine);
        }
        _countdownRoutine = StartCoroutine(AnimateCountdown(value, duration));
    }

    private void HandleRaceFinished(RacerResult result)
    {
        finishPanel.SetActive(true);
        finishSummaryText.text =
            $"Time: {result.finishTimeSeconds:0.000}s\n" +
            $"Coins: {result.coinsCollected}\n" +
            $"XP: {result.xpEarned}\n" +
            $"Place: {result.place}/{result.totalRacers}";
    }

    private IEnumerator AnimateCountdown(string value, float duration)
    {
        countdownText.gameObject.SetActive(true);
        countdownText.text = value;
        countdownText.color = Color.white;
        countdownText.transform.localScale = Vector3.one * 1.7f;

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var scale = Mathf.Lerp(1.7f, 1f, t);
            var alpha = Mathf.Lerp(1f, 0f, t);
            countdownText.transform.localScale = Vector3.one * scale;
            countdownText.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        countdownText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_manager == null)
        {
            return;
        }

        _manager.OnTimerChanged -= HandleTimerChanged;
        _manager.OnCoinsChanged -= HandleCoinsChanged;
        _manager.OnLapDisplayChanged -= HandleLapChanged;
        _manager.OnCountdownChanged -= HandleCountdownChanged;
        _manager.OnRaceFinished -= HandleRaceFinished;
    }
}
