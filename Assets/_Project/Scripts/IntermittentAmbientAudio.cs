using UnityEngine;

// Scene-owned 2D ambience: short excerpts separated by random quiet intervals.
[DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
public sealed class IntermittentAmbientAudio : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float maximumVolume = .25f;
    [SerializeField] private Vector2 initialDelayRange = new Vector2(3f, 8f);
    [SerializeField] private Vector2 quietIntervalRange = new Vector2(8f, 16f);
    [SerializeField] private Vector2 burstDurationRange = new Vector2(2.5f, 5f);
    [SerializeField, Min(.01f)] private float fadeSeconds = .5f;

    private AudioSource source;
    private float waitRemaining;
    private float burstElapsed;
    private float burstDuration;
    private float burstVolume;
    private bool playingBurst;
    private bool paused;
    public bool IsPlayingBurst => playingBurst;
    public AudioSource Source => source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
    }

    private void OnEnable()
    {
        if (source == null) Awake();
        source.Stop();
        source.volume = 0f;
        playingBurst = paused = false;
        waitRemaining = RandomSeconds(initialDelayRange);
    }

    private void Update()
    {
        // Freeze the excerpt and its timers together when gameplay is paused.
        if (Time.timeScale <= 0f)
        {
            if (playingBurst && !paused) source.Pause();
            paused = playingBurst;
            return;
        }
        if (paused) { source.UnPause(); paused = false; }
        Tick(Time.deltaTime);
    }

    private void Tick(float deltaTime)
    {
        if (source.clip == null || deltaTime <= 0f) return;
        if (!playingBurst)
        {
            waitRemaining -= deltaTime;
            if (waitRemaining <= 0f) BeginBurst();
            return;
        }

        burstElapsed += deltaTime;
        if (burstElapsed >= burstDuration)
        {
            source.Stop();
            source.volume = 0f;
            playingBurst = false;
            waitRemaining = RandomSeconds(quietIntervalRange);
            return;
        }
        source.volume = GetBurstVolume(burstElapsed, burstDuration);
    }

    private void BeginBurst()
    {
        burstDuration = Mathf.Min(RandomSeconds(burstDurationRange),
            Mathf.Max(.01f, source.clip.length - .05f));
        burstElapsed = 0f;
        burstVolume = maximumVolume * Random.Range(.9f, 1f);
        source.volume = 0f;
        source.pitch = 1f;
        source.time = Random.Range(0f, Mathf.Max(0f, source.clip.length - burstDuration - .05f));
        source.Play();
        playingBurst = true;
    }

    private float GetBurstVolume(float elapsed, float duration)
    {
        float fade = Mathf.Min(Mathf.Max(.01f, fadeSeconds), duration * .5f);
        return burstVolume * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fade))
            * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((duration - elapsed) / fade));
    }

    private static float RandomSeconds(Vector2 range)
    {
        float minimum = Mathf.Max(.01f, Mathf.Min(range.x, range.y));
        return Random.Range(minimum, Mathf.Max(minimum, Mathf.Max(range.x, range.y)));
    }

    private void OnDisable()
    {
        if (source != null) { source.Stop(); source.volume = 0f; }
        playingBurst = paused = false;
    }
}
