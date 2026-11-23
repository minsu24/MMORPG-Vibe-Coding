using EasternFantasy.Player;
using UnityEngine;

// Use the player's map position: the 2D camera's listener is far behind the map on Z.
[DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
public sealed class LocalAmbientAudio2D : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float maximumVolume = .45f;
    [SerializeField, Min(0f)] private float fullVolumeRadius = 1.5f;
    [SerializeField, Min(.01f)] private float audibleRadius = 5f;
    [SerializeField, Min(.01f)] private float fadeSeconds = .4f;
    private AudioSource source;
    private static PlayerMovement2D player;
    private static float nextPlayerSearch;
    private bool continuousPlayback = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetListener()
    {
        player = null;
        nextPlayerSearch = 0f;
    }

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.volume = 0f;
    }

    private void OnEnable()
    {
        if (source == null) source = GetComponent<AudioSource>();
        source.volume = 0f;
        if (continuousPlayback && source.clip != null) source.Play();
    }

    public void PrepareOneShot(float volume, float nearRadius, float farRadius)
    {
        if (source == null) source = GetComponent<AudioSource>();
        continuousPlayback = false;
        source.loop = false;
        maximumVolume = Mathf.Clamp01(volume);
        fullVolumeRadius = Mathf.Max(0f, nearRadius);
        audibleRadius = Mathf.Max(fullVolumeRadius + .01f, farRadius);
        // Short effects need their attack immediately at the current distance.
        source.volume = GetPlayerVolume();
    }

    public float GetVolumeAt(Vector2 position)
        => CalculateVolume(transform.position, position, maximumVolume, fullVolumeRadius, audibleRadius);

    public static float CalculateVolume(Vector2 origin, Vector2 listener, float maximum, float nearRadius, float farRadius)
    {
        float distance = Vector2.Distance(listener, origin);
        float outer = Mathf.Max(nearRadius + .01f, farRadius);
        if (distance >= outer) return 0f;
        float blend = Mathf.InverseLerp(nearRadius, outer, distance);
        return Mathf.Clamp01(maximum) * (1f - Mathf.SmoothStep(0f, 1f, blend));
    }

    private float GetPlayerVolume()
        => GetPlayerVolumeAt(transform.position, maximumVolume, fullVolumeRadius, audibleRadius);

    public static float GetPlayerVolumeAt(Vector2 origin, float maximum, float nearRadius = 1.5f, float farRadius = 10f)
    {
        if ((player == null || !player.isActiveAndEnabled) && Time.unscaledTime >= nextPlayerSearch)
        {
            player = FindFirstObjectByType<PlayerMovement2D>();
            nextPlayerSearch = Time.unscaledTime + .5f;
        }
        return player != null && player.isActiveAndEnabled
            ? CalculateVolume(origin, player.transform.position, maximum, nearRadius, farRadius) : 0f;
    }

    private void Update()
    {
        float target = GetPlayerVolume();
        // Silence is exact outside the local area; camera movement cannot make it audible.
        source.volume = target <= 0f ? 0f : Mathf.MoveTowards(source.volume, target,
            maximumVolume * Time.unscaledDeltaTime / Mathf.Max(.01f, fadeSeconds));
    }

    private void OnDisable()
    {
        if (source != null) { source.Stop(); source.volume = 0f; }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(.3f, .8f, 1f, .8f);
        Gizmos.DrawWireSphere(transform.position, audibleRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, fullVolumeRadius);
    }
}
