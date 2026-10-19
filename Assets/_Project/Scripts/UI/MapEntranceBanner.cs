using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class MapEntranceBanner : MonoBehaviour
    {
        [SerializeField] private Canvas bannerCanvas;
        [SerializeField] private CanvasGroup bannerGroup;
        [SerializeField] private TMP_Text mapName;
        [SerializeField] private RectTransform card;
        [SerializeField, Min(0.01f)] private float fadeInSeconds = 0.3f;
        [SerializeField, Min(0f)] private float holdSeconds = 1.8f;
        [SerializeField, Min(0.01f)] private float fadeOutSeconds = 0.5f;

        private static MapEntranceBanner instance;
        private Coroutine animationRoutine;
        private Vector2 restingPosition;
        public bool IsShowing => bannerCanvas != null && bannerCanvas.enabled && bannerGroup.alpha > 0f;
        public string CurrentMapName => mapName != null ? mapName.text : string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;
            var prefab = Resources.Load<GameObject>("MapEntranceBanner");
            if (prefab != null) Instantiate(prefab);
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            restingPosition = card.anchoredPosition;
            bannerGroup.blocksRaycasts = false;
            bannerGroup.interactable = false;
            Hide();
        }
        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void Start() => ShowScene(SceneManager.GetActiveScene());
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Loading a supporting additive scene is not entering a new map.
            if (mode == LoadSceneMode.Additive && scene != SceneManager.GetActiveScene()) return;
            ShowScene(scene);
        }
        private void ShowScene(Scene scene)
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
            Hide();
            string title = MapLocationNames.GetDisplayName(scene.name);
            mapName.text = title;
            if (!string.IsNullOrEmpty(title)) animationRoutine = StartCoroutine(ShowAfterTransition());
        }
        private IEnumerator ShowAfterTransition()
        {
            // Start the readable duration after the portal's black overlay disappears.
            yield return null;
            while (PortalSceneTransition.IsTransitioning) yield return null;
            bannerCanvas.enabled = true;
            yield return Fade(0f, 1f, fadeInSeconds);
            yield return new WaitForSecondsRealtime(holdSeconds);
            yield return Fade(1f, 0f, fadeOutSeconds);
            Hide();
            animationRoutine = null;
        }
        private IEnumerator Fade(float from, float to, float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds));
                bannerGroup.alpha = Mathf.Lerp(from, to, t);
                card.anchoredPosition = restingPosition + Vector2.up * ((1f - bannerGroup.alpha) * 8f);
                yield return null;
            }
            bannerGroup.alpha = to;
        }
        private void Hide()
        {
            bannerCanvas.enabled = false;
            bannerGroup.alpha = 0f;
            card.anchoredPosition = restingPosition;
        }
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
            if (bannerCanvas != null) Hide();
        }
        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
