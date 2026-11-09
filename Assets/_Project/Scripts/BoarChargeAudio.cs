using UnityEngine;

// Owned sources stop with the encounter and follow its scaled gameplay clock.
public sealed class BoarChargeAudio : MonoBehaviour
{
    private readonly AudioSource[] sources = new AudioSource[5];
    private readonly float[] pitches = { 1f, 1f, 1f, 1f, 1f };
    private readonly AudioClip[] steps = new AudioClip[4];
    private AudioClip scrape, grunt, impact, tuskSwing;
    private bool prepared, warningActive, chargingActive, paused, impactPlayed;
    private float nextFoot;
    private int footVoice;
    private bool locomotionActive;
    private float locomotionSpeed;

    public void SetLocomotion(bool moving, float speed)
    {
        if (warningActive || chargingActive) return;
        if (moving)
        {
            Prepare();
            if (!locomotionActive) nextFoot = 0f;
            locomotionSpeed = speed;
        }
        else if (locomotionActive)
            for (int i = 0; i < 2; i++) sources[i].Stop();
        locomotionActive = moving;
    }

    private void Prepare()
    {
        if (prepared) return;
        prepared = true;
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
            sources[i].loop = false;
            sources[i].spatialBlend = 0f;
        }
        for (int i = 0; i < steps.Length; i++) steps[i] = Resources.Load<AudioClip>("Audio/BoarStep" + i);
        scrape = Resources.Load<AudioClip>("Audio/BoarScrape");
        grunt = Resources.Load<AudioClip>("Audio/BoarGrunt");
        impact = Resources.Load<AudioClip>("Audio/ChargePlayerImpact");
        tuskSwing = Resources.Load<AudioClip>("Audio/FarmCartFlight");
    }

    public void BeginWarning()
    {
        Prepare();
        EndCharge();
        sources[3].Stop();
        impactPlayed = false;
        warningActive = true;
        // BoarWindup frame 4 plants and scrapes at 0.2s in each 0.5s cycle.
        nextFoot = .2f;
        Play(2, grunt, .38f, 1f, 2f);
    }

    public void BeginCharge()
    {
        Prepare();
        EndCharge();
        chargingActive = true;
        PlayFoot();
        nextFoot = Random.Range(.14f, .26f);
    }

    public void EndCharge()
    {
        warningActive = chargingActive = locomotionActive = false;
        for (int i = 0; i < 3; i++) if (sources[i] != null) sources[i].Stop();
    }

    public void PlayImpact()
    {
        Prepare();
        if (impactPlayed) return;
        impactPlayed = true;
        Play(3, impact, .45f, 1f);
    }

    public void PlayTuskSwing()
    {
        Prepare();
        // Skip the quiet lead-in so the supplied swish accompanies the upswing.
        Play(4, tuskSwing, .4f, 1f, .15f);
    }

    private void Play(int index, AudioClip clip, float volume, float pitch, float start = 0f)
    {
        if (clip == null) return;
        var source = sources[index];
        source.Stop();
        source.clip = clip;
        source.volume = volume;
        pitches[index] = pitch;
        source.pitch = Mathf.Clamp(pitch * Time.timeScale, .01f, 3f);
        source.time = Mathf.Clamp(start, 0f, Mathf.Max(0f, clip.length - .01f));
        source.Play();
        if (Time.timeScale <= 0f) { source.Pause(); paused = true; }
    }

    private void PlayFoot()
    {
        if (warningActive) Play(0, scrape, .28f, .85f);
        else Play(footVoice++ % 2, steps[Random.Range(0, steps.Length)], Random.Range(.22f, .3f), Random.Range(.9f, 1.1f));
    }

    private void Update() => Tick(Time.deltaTime);

    private void Tick(float deltaTime)
    {
        if (!prepared) return;
        if (Time.timeScale <= 0f)
        {
            if (!paused) { foreach (var source in sources) source.Pause(); paused = true; }
            return;
        }
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i].pitch = Mathf.Clamp(pitches[i] * Time.timeScale, .01f, 3f);
            if (paused) sources[i].UnPause();
        }
        paused = false;
        if (!warningActive && !chargingActive && !locomotionActive) return;
        nextFoot -= Mathf.Max(0f, deltaTime);
        if (nextFoot > 0f) return;
        PlayFoot();
        float walkInterval = Mathf.Lerp(.45f, .22f, Mathf.Clamp01(locomotionSpeed / 14f));
        nextFoot = warningActive ? .5f - Mathf.Repeat(-nextFoot, .5f) :
            chargingActive ? Random.Range(.14f, .26f) : walkInterval * Random.Range(.85f, 1.15f);
    }

    private void OnDisable()
    {
        EndCharge();
        for (int i = 3; i < sources.Length; i++) if (sources[i] != null) sources[i].Stop();
        paused = false;
    }
}
