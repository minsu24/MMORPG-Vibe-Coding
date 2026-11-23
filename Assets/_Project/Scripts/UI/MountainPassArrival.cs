using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.UI
{
    internal sealed class MountainPassArrival : MonoBehaviour
    {
        internal const string Destination = "SmogMountain";
        private const float RainVolume = 0.25f;
        private static MountainPassArrival current;
        private AudioSource rain;
        private float startedAt;
        private float revealAt = -1f;

        internal static bool HasPlayed { get; set; }
        internal static bool IsPending => current != null && !HasPlayed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            current = null;
            HasPlayed = false;
        }

        internal static bool ShouldPlay(string source, string destination, string spawnPoint) =>
            !HasPlayed && source == "BrokenSeal" && destination == Destination
            && spawnPoint == "In_SmogMountain_Portal";

        internal static MountainPassArrival Begin(Transform parent)
        {
            var root = new GameObject("Mountain Pass Arrival");
            root.transform.SetParent(parent, false);
            var arrival = root.AddComponent<MountainPassArrival>();
            current = arrival;
            arrival.startedAt = Time.unscaledTime;
            var clip = Resources.Load<AudioClip>("Audio/MountainPassRain");
            if (clip != null)
            {
                arrival.rain = root.AddComponent<AudioSource>();
                arrival.rain.playOnAwake = false;
                arrival.rain.ignoreListenerPause = true;
                arrival.rain.loop = true;
                arrival.rain.clip = clip;
                arrival.rain.volume = 0f;
                arrival.rain.Play();
            }
            return arrival;
        }

        internal void Reveal()
        {
            revealAt = Time.unscaledTime;
            PortalAudioCue.Play("Audio/MountainPassWindChime", 0.12f, owner: transform);
        }

        private void Update()
        {
            if (revealAt < 0f && rain != null)
                rain.volume = RainVolume * Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - startedAt) / 0.2f);
            if (revealAt < 0f) return;
            float elapsed = Time.unscaledTime - revealAt;
            if (rain != null && rain.isPlaying)
            {
                rain.volume = RainVolume * (1f - Mathf.SmoothStep(0f, 1f, elapsed / 1.1f));
                if (rain.volume <= 0f) rain.Stop();
            }

            if (elapsed >= 3.6f || SceneManager.GetActiveScene().name != Destination)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }

        private void OnDisable()
        {
            if (current == this) current = null;
        }
    }
}
