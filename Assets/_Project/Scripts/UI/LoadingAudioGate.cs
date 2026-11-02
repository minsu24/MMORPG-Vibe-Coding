using System.Collections;
using UnityEngine;

namespace EasternFantasy.UI
{
    // Pause the listener before scene Awake/PlayOnAwake can emit any audio.
    // Keep this separate from master volume so settings cannot unmute loading.
    public sealed class LoadingAudioGate : MonoBehaviour
    {
        private static LoadingAudioGate instance;
        private static bool startupPending;
        private static bool loadingBlocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            instance = null;
            startupPending = true;
            loadingBlocked = false;
            ApplyPause();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var root = new GameObject("Loading Audio Gate");
            instance = root.AddComponent<LoadingAudioGate>();
            DontDestroyOnLoad(root);
            ApplyPause();
        }

        private IEnumerator Start()
        {
            // Let first-scene Awake/Start and save restoration acquire the loading
            // gate before releasing startup silence, including direct scene Play.
            yield return null;
            startupPending = false;
            ApplyPause();
        }

        public static void SetLoadingBlocked(bool blocked)
        {
            loadingBlocked = blocked;
            ApplyPause();
        }

        private static void ApplyPause()
        {
            AudioListener.pause = startupPending || loadingBlocked;
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            instance = null;
            startupPending = loadingBlocked = false;
            ApplyPause();
        }
    }
}
