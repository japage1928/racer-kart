using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CoinPickup : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 120f;
    [SerializeField] private RacerGameManager gameManager;

    private static AudioClip _coinClip;

    public void Initialize(RacerGameManager manager)
    {
        gameManager = manager;
    }

    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<RacerGameManager>();
        }

        if (other.GetComponentInParent<KartController>() == null || gameManager == null)
        {
            return;
        }

        gameManager.AddCoin();
        PlayCoinSound();
        gameObject.SetActive(false);
    }

    private void PlayCoinSound()
    {
        if (_coinClip == null)
        {
            _coinClip = BuildCoinClip();
        }
        AudioSource.PlayClipAtPoint(_coinClip, transform.position, 0.25f);
    }

    private static AudioClip BuildCoinClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.085f;
        var sampleCount = Mathf.RoundToInt(sampleRate * duration);
        var data = new float[sampleCount];

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            var freq = Mathf.Lerp(820f, 1260f, t / duration);
            var envelope = 1f - (t / duration);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.45f;
        }

        var clip = AudioClip.Create("CoinBeep", sampleCount, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
