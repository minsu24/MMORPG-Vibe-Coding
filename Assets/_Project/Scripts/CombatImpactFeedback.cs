using Unity.Cinemachine;
using UnityEngine;

// Shared combat feedback; the active Cinemachine camera receives a short impulse.
public sealed class CombatImpactFeedback : MonoBehaviour
{
    private static CombatImpactFeedback instance;
    private CinemachineImpulseSource impulseSource;
    private AudioSource impactAudio;
    private CombatImpactSounds impactSounds;
    private int lastSoundIndex = -1;
    private float nextSoundTime;

    public static void Play(Vector3 hitPosition, bool critical)
    {
        Play(hitPosition, critical, null, 1f);
    }

    public static void Play(Vector3 hitPosition, bool critical, AudioClip soundOverride, float soundVolume)
    {
        if (Time.timeScale <= 0f)
            return;

        GameTimeController.RequestHitStop(critical ? 0.06f : 0.04f);
        if (instance == null)
        {
            GameObject controller = new GameObject("Combat Impact Feedback");
            instance = controller.AddComponent<CombatImpactFeedback>();
            DontDestroyOnLoad(controller);
        }
        instance.PlayImpactSound(critical, soundOverride, soundVolume);
        instance.Shake(hitPosition, critical);
    }

    private void Awake()
    {
        impactSounds = Resources.Load<CombatImpactSounds>("CombatImpactSounds");
        impactAudio = gameObject.AddComponent<AudioSource>();
        impactAudio.playOnAwake = false;
        impactAudio.spatialBlend = 0f;
        impactAudio.loop = false;

        impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
        impulseSource.ImpulseDefinition.ImpulseType =
            CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        impulseSource.ImpulseDefinition.ImpulseShape =
            CinemachineImpulseDefinition.ImpulseShapes.Bump;
        impulseSource.ImpulseDefinition.ImpulseDuration = 0.12f;
        impulseSource.ImpulseDefinition.ImpulseChannel = 1;
    }

    // A detached source per target preserves simultaneous skill hits, including lethal ones.
    public static void PlayIndividualImpactSound(Vector3 position, AudioClip clip, float volume, bool critical = false)
    {
        if (clip == null || volume <= 0f) return;
        var root = new GameObject("Individual Skill Impact Audio");
        root.transform.position = position;
        var source = root.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.pitch = Random.Range(.98f, 1.02f);
        source.volume = Mathf.Clamp01(volume * (critical ? 1.15f : 1f));
        source.clip = clip;
        source.Play();
        Destroy(root, clip.length / source.pitch + .1f);
    }

    private void PlayImpactSound(bool critical, AudioClip soundOverride, float soundVolume)
    {
        if (Time.unscaledTime < nextSoundTime || soundVolume <= 0f) return;
        if (soundOverride != null)
        {
            nextSoundTime = Time.unscaledTime + 0.045f;
            impactAudio.enabled = true;
            impactAudio.pitch = Random.Range(0.98f, 1.02f);
            impactAudio.PlayOneShot(soundOverride, Mathf.Clamp01(soundVolume * (critical ? 1.15f : 1f)));
            return;
        }
        if (impactSounds == null || impactSounds.Clips == null
            || impactSounds.Clips.Length == 0 || Time.unscaledTime < nextSoundTime)
            return;

        // Simultaneous hits share one sound so area attacks do not overload the mix.
        nextSoundTime = Time.unscaledTime + 0.045f;
        int count = impactSounds.Clips.Length;
        int index = Random.Range(0, count);
        if (count > 1 && index == lastSoundIndex)
            index = (index + Random.Range(1, count)) % count;
        lastSoundIndex = index;
        impactAudio.pitch = Random.Range(0.96f, 1.04f);
        impactAudio.PlayOneShot(impactSounds.Clips[index],
            Mathf.Clamp01(impactSounds.Volume * (critical ? 1.15f : 1f)));
    }

    private void Shake(Vector3 hitPosition, bool critical)
    {
        CinemachineCamera camera = FindFirstObjectByType<CinemachineCamera>();
        if (camera == null)
            return;

        CinemachineImpulseListener listener = camera.GetComponent<CinemachineImpulseListener>();
        if (listener == null)
        {
            listener = camera.gameObject.AddComponent<CinemachineImpulseListener>();
            listener.ChannelMask = 1;
            listener.Gain = 1f;
            listener.Use2DDistance = true;
        }

        float strength = critical ? 0.14f : 0.07f;
        impulseSource.GenerateImpulseAtPositionWithVelocity(hitPosition,
            new Vector3(Random.Range(-1f, 1f), 1f, 0f).normalized * strength);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
