using UnityEngine;
using UnityEngine.SceneManagement;

// A scaled-time cue; projectile loops follow their owner, impact tails are independent.
public sealed class GiantAudioCue : MonoBehaviour
{
    private AudioSource source;
    private float age, duration, volume;
    private bool paused, looping;

    public static GiantAudioCue Play(GameObject context, string resource, float volume, bool loop = false, float startSeconds = 0f)
    {
        var clip = Resources.Load<AudioClip>(resource);
        if (context == null || clip == null) return null;
        var go = new GameObject(context.name + " " + clip.name + " Audio");
        SceneManager.MoveGameObjectToScene(go, context.scene);
        go.transform.position = context.transform.position;
        if (loop) go.transform.SetParent(context.transform, true);
        var cue = go.AddComponent<GiantAudioCue>();
        cue.volume = volume;
        float start = Mathf.Clamp(startSeconds, 0f, Mathf.Max(0f, clip.length - .01f));
        cue.duration = clip.length - start;
        cue.looping = loop;
        cue.source = go.AddComponent<AudioSource>();
        cue.source.playOnAwake = false;
        cue.source.loop = loop;
        cue.source.spatialBlend = 0f;
        cue.source.volume = loop ? 0f : volume;
        cue.source.clip = clip;
        cue.source.time = start;
        cue.source.pitch = Mathf.Clamp(Time.timeScale, .01f, 3f);
        cue.source.Play();
        if (Time.timeScale <= 0f) { cue.source.Pause(); cue.paused = true; }
        return cue;
    }

    private void Update() => Tick(Time.deltaTime);

    private void Tick(float deltaTime)
    {
        if (source == null) return;
        if (Time.timeScale <= 0f)
        {
            if (!paused) { source.Pause(); paused = true; }
            return;
        }
        source.pitch = Mathf.Clamp(Time.timeScale, .01f, 3f);
        if (paused) { source.UnPause(); paused = false; }
        age += Mathf.Max(0f, deltaTime);
        if (looping) source.volume = volume * Mathf.Clamp01(age / .02f);
        else if (age >= duration) Stop();
    }

    public void Stop()
    {
        if (source != null) source.Stop();
        gameObject.SetActive(false);
        if (Application.isPlaying) Destroy(gameObject);
        else DestroyImmediate(gameObject);
    }

    private void OnDisable() { if (source != null) source.Stop(); }
}
