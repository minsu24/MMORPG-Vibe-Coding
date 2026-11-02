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
    private PlayerMovement2D player;
    private float nextPlayerSearch;

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
        nextPlayerSearch = 0f;
        if (source.clip != null) source.Play();
    }

    public float GetVolumeAt(Vector2 position)
    {
        float distance = Vector2.Distance(position, transform.position);
        float outer = Mathf.Max(fullVolumeRadius + .01f, audibleRadius);
        if (distance >= outer) return 0f;
        float blend = Mathf.InverseLerp(fullVolumeRadius, outer, distance);
        return maximumVolume * (1f - Mathf.SmoothStep(0f, 1f, blend));
    }

    private void Update()
    {
        if ((player == null || !player.isActiveAndEnabled) && Time.unscaledTime >= nextPlayerSearch)
        {
            player = FindFirstObjectByType<PlayerMovement2D>();
            nextPlayerSearch = Time.unscaledTime + .5f;
        }
        float target = player != null && player.isActiveAndEnabled ? GetVolumeAt(player.transform.position) : 0f;
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
