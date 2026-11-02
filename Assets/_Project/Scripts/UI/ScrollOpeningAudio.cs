using UnityEngine;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class ScrollOpeningAudio : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float volume = .25f;
        [SerializeField, Range(0f, 1f)] private float closeVolume = .2f;
        private AudioSource source;

        // Both prefab-based windows and independently authored scene windows
        // share the same opening cue without duplicating serialized setup.
        public static void PlayFor(Component owner)
        {
            var audio = owner.GetComponent<ScrollOpeningAudio>();
            if (audio == null) audio = owner.gameObject.AddComponent<ScrollOpeningAudio>();
            audio.Play();
        }

        public static void PlayClosingFor(Component owner)
        {
            var audio = owner.GetComponent<ScrollOpeningAudio>();
            if (audio == null) audio = owner.gameObject.AddComponent<ScrollOpeningAudio>();
            audio.Play(true);
        }

        private void Play(bool closing = false)
        {
            // The window's opening OnEnable may run before this component's
            // activation callback when reopening an existing window.
            if (!enabled || !gameObject.activeInHierarchy) return;
            var clip = Resources.Load<AudioClip>(closing ? "Audio/ScrollClose" : "Audio/ScrollOpen");
            if (clip == null) return;
            if (source == null)
            {
                var child = new GameObject("Scroll Opening Audio");
                child.transform.SetParent(transform, false);
                source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
            }
            source.clip = clip;
            source.volume = closing ? closeVolume : volume;
            source.pitch = closing ? 2f : 1f;
            source.Play();
        }

        public void Stop()
        {
            if (source != null) source.Stop();
        }

        private void OnDisable() => Stop();
    }
}
