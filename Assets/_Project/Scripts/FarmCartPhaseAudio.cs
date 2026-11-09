using UnityEngine;

// Attached to the warning/projectile so its audio cannot outlive that phase.
public sealed class FarmCartPhaseAudio : MonoBehaviour
{
    private AudioSource source;
    private float volume, elapsed;
    private bool paused;

    public static FarmCartPhaseAudio Play(GameObject context, string resource, float volume, bool loop = false)
    {
        var clip = Resources.Load<AudioClip>(resource);
        if (clip == null) return null;
        var effect = context.AddComponent<FarmCartPhaseAudio>();
        effect.volume = volume;
        effect.source = context.AddComponent<AudioSource>();
        effect.source.playOnAwake = false;
        effect.source.loop = loop;
        effect.source.spatialBlend = 0f;
        effect.source.volume = 0f;
        effect.source.clip = clip;
        effect.source.pitch = Mathf.Clamp(Time.timeScale, .01f, 3f);
        effect.source.Play();
        effect.Update();
        return effect;
    }

    private void Update()
    {
        if (source == null) return;
        if (Time.timeScale <= 0f)
        {
            if (!paused) { source.Pause(); paused = true; }
            return;
        }
        source.pitch = Mathf.Clamp(Time.timeScale, .01f, 3f);
        if (paused) { source.UnPause(); paused = false; }
        elapsed += Time.deltaTime;
        source.volume = volume * Mathf.Clamp01(elapsed / .025f);
    }

    public void Stop()
    {
        if (source != null) source.Stop();
        paused = false;
    }

    private void OnDisable() => Stop();
}
