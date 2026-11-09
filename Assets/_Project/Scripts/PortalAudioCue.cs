using UnityEngine;
using UnityEngine.SceneManagement;

// Portal cinematics and loading run on real time while gameplay/listener are paused.
public sealed class PortalAudioCue : MonoBehaviour
{
    private AudioSource source;
    private float age, duration, volume;

    public static PortalAudioCue Play(string resource, float volume, float startSeconds = 0f,
        float maximumSeconds = 0f, Transform owner = null, bool persistent = false)
    {
        var clip = Resources.Load<AudioClip>(resource);
        if (clip == null) return null;
        var go = new GameObject("Portal Audio - " + clip.name);
        if (owner != null)
        {
            SceneManager.MoveGameObjectToScene(go, owner.gameObject.scene);
            go.transform.SetParent(owner, false);
        }
        else if (persistent && Application.isPlaying) DontDestroyOnLoad(go);
        var cue = go.AddComponent<PortalAudioCue>();
        float start = Mathf.Clamp(startSeconds, 0f, Mathf.Max(0f, clip.length - .01f));
        cue.duration = maximumSeconds > 0f ? Mathf.Min(maximumSeconds, clip.length - start) : clip.length - start;
        cue.volume = Mathf.Clamp01(volume);
        cue.source = go.AddComponent<AudioSource>();
        cue.source.playOnAwake = false;
        cue.source.loop = false;
        cue.source.spatialBlend = 0f;
        cue.source.ignoreListenerPause = true;
        cue.source.clip = clip;
        cue.source.time = start;
        cue.source.volume = 0f;
        cue.source.Play();
        return cue;
    }

    private void Update() => Advance(Time.unscaledDeltaTime);

    private void Advance(float seconds)
    {
        age += Mathf.Max(0f, seconds);
        if (age >= duration) { Stop(); return; }
        source.volume = volume * Mathf.Clamp01(age / .015f) * Mathf.Clamp01((duration - age) / .025f);
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
