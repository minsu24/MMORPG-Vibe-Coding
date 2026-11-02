using System.Collections;
using System.Collections.Generic;
using EasternFantasy.Saving;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EasternFantasy.UI
{
    // Shared by portal transfers and save restoration, including a second saved-scene load.
    public sealed class SceneLoadingScreen : MonoBehaviour
    {
        private const float MinimumArtworkSeconds = 5f;
        private static SceneLoadingScreen instance;
        private Canvas canvas;
        private CanvasGroup group;
        private RectTransform artworkRoot;
        private Image progressFill;
        private TMP_Text percentage;
        private readonly RectTransform[] dots = new RectTransform[3];
        private float targetProgress, displayedProgress, shownAt;
        private bool visible;
        private bool artworkVisible;
        private Coroutine fadeIn;
        private Texture2D gradient;
        private TMP_FontAsset font;
        private readonly HashSet<Canvas> hiddenCanvases = new HashSet<Canvas>();
        private readonly HashSet<GraphicRaycaster> hiddenRaycasters = new HashSet<GraphicRaycaster>();
        private readonly HashSet<Renderer> hiddenTextRenderers = new HashSet<Renderer>();
        private static readonly Color Gold = new Color(0.96f, 0.82f, 0.56f);

        public static bool IsVisible => instance != null && instance.visible;
        public static float Progress => instance != null ? instance.displayedProgress : 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => instance = null;

        public static bool IsArtworkVisible => IsVisible && instance.artworkVisible;

        public static void Begin(bool immediately = false, bool showArtwork = true)
        {
            LoadingAudioGate.SetLoadingBlocked(true);
            if (instance == null)
            {
                var root = new GameObject("Scene Loading Screen", typeof(RectTransform));
                instance = root.AddComponent<SceneLoadingScreen>();
                DontDestroyOnLoad(root);
                instance.Build();
            }
            if (instance.visible)
            {
                if (showArtwork)
                {
                    instance.artworkVisible = true;
                    instance.artworkRoot.gameObject.SetActive(true);
                }
                return;
            }
            instance.visible = true;
            instance.HideOtherUI();
            instance.artworkVisible = showArtwork;
            instance.artworkRoot.gameObject.SetActive(showArtwork);
            instance.targetProgress = instance.displayedProgress = 0f;
            instance.progressFill.fillAmount = 0f;
            instance.shownAt = Time.unscaledTime;
            instance.canvas.enabled = true;
            instance.group.blocksRaycasts = true;
            instance.group.alpha = immediately ? 1f : 0f;
            GameTimeController.SetPaused(instance, true);
            if (!immediately) instance.fadeIn = instance.StartCoroutine(instance.Fade(1f, 0.28f));
        }

        public static IEnumerator WaitUntilCovered()
        {
            while (IsVisible && instance.group.alpha < 0.999f) yield return null;
        }

        public static void Report(float progress)
        {
            if (IsVisible) instance.targetProgress = Mathf.Max(instance.targetProgress, Mathf.Clamp01(progress));
        }

        public static IEnumerator Finish(System.Action onReveal = null)
        {
            if (!IsVisible) yield break;
            yield return WaitUntilCovered();
            Report(0.82f);
            yield return instance.SettleCamera();
            if (instance.artworkVisible)
            {
                Report(1f);
                // Keep the first-entry illustration visible even when loading finishes quickly.
                while (instance.displayedProgress < 0.999f || Time.unscaledTime - instance.shownAt < MinimumArtworkSeconds)
                    yield return null;
                yield return new WaitForSecondsRealtime(0.12f);
            }
            onReveal?.Invoke();
            yield return instance.Fade(0f, 0.4f);
            Cancel();
        }

        public static void Cancel()
        {
            if (instance == null)
            {
                LoadingAudioGate.SetLoadingBlocked(false);
                return;
            }
            if (instance.fadeIn != null) instance.StopCoroutine(instance.fadeIn);
            instance.fadeIn = null;
            instance.group.alpha = 0f;
            instance.group.blocksRaycasts = false;
            instance.canvas.enabled = false;
            instance.visible = false;
            instance.RestoreOtherUI();
            instance.artworkVisible = false;
            GameTimeController.SetPaused(instance, false);
            LoadingAudioGate.SetLoadingBlocked(false);
        }

        private void OnEnable()
        {
            Canvas.preWillRenderCanvases += HideOtherUI;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => HideOtherUI();

        private void LateUpdate() => HideOtherUI();

        private void HideOtherUI()
        {
            if (!visible) return;
            // Include inactive objects so activating a panel cannot flash it during loading.
            // Leave GameObjects and UI controllers running; only suppress drawing/input.
            foreach (Canvas other in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!other.enabled || other.transform.IsChildOf(transform)) continue;
                hiddenCanvases.Add(other);
                other.enabled = false;
            }
            foreach (GraphicRaycaster other in FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!other.enabled || other.transform.IsChildOf(transform)) continue;
                hiddenRaycasters.Add(other);
                other.enabled = false;
            }
            // Damage numbers and some nameplates use TMP meshes instead of a Canvas.
            foreach (TextMeshPro text in FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text.transform.IsChildOf(transform)) continue;
                foreach (Renderer renderer in text.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.forceRenderingOff) continue;
                    hiddenTextRenderers.Add(renderer);
                    renderer.forceRenderingOff = true;
                }
            }
        }

        private void RestoreOtherUI()
        {
            foreach (Canvas other in hiddenCanvases) if (other != null) other.enabled = true;
            foreach (GraphicRaycaster other in hiddenRaycasters) if (other != null) other.enabled = true;
            foreach (Renderer other in hiddenTextRenderers) if (other != null) other.forceRenderingOff = false;
            hiddenCanvases.Clear();
            hiddenRaycasters.Clear();
            hiddenTextRenderers.Clear();
        }

        private void OnDisable()
        {
            if (visible)
            {
                visible = false;
                LoadingAudioGate.SetLoadingBlocked(false);
                GameTimeController.SetPaused(this, false);
            }
            Canvas.preWillRenderCanvases -= HideOtherUI;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreOtherUI();
        }

        private void Update()
        {
            if (!visible) return;
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.unscaledDeltaTime * 1.15f);
            progressFill.fillAmount = displayedProgress;
            progressFill.rectTransform.anchorMax = new Vector2(displayedProgress, 1f);
            percentage.text = Mathf.FloorToInt(displayedProgress * 100f) + "%";
            // Three separate hops in order, then a short rest before repeating.
            float cycle = (Time.unscaledTime - shownAt) % 1.35f;
            for (int i = 0; i < dots.Length; i++)
            {
                float phase = (cycle - i * 0.27f) / 0.35f;
                float height = phase > 0f && phase < 1f ? Mathf.Sin(phase * Mathf.PI) * 13f : 0f;
                dots[i].anchoredPosition = new Vector2(66f + i * 17f, height);
            }
        }

        private IEnumerator Fade(float target, float seconds)
        {
            float from = group.alpha;
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, elapsed / seconds));
                yield return null;
            }
            group.alpha = target;
        }

        private IEnumerator SettleCamera()
        {
            // Let sceneLoaded, Start, save restoration, and teleports finish first.
            yield return null;
            yield return null;
            while (SaveGameService.IsRestoring) yield return null;
            Physics2D.SyncTransforms();
            Camera output = Camera.main;
            var brain = output != null ? output.GetComponent<CinemachineBrain>() : null;
            var vcam = FindFirstObjectByType<CinemachineCamera>();
            var confiner = vcam != null ? vcam.GetComponent<CinemachineConfiner2D>() : null;
            var oldMode = brain != null ? brain.UpdateMethod : default;
            bool oldIgnoreTimeScale = brain != null && brain.IgnoreTimeScale;
            float oldDamping = confiner != null ? confiner.Damping : 0f;
            float oldSlowing = confiner != null ? confiner.SlowingDistance : 0f;
            try
            {
                if (brain != null)
                {
                    brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
                    brain.IgnoreTimeScale = true;
                    brain.ResetState();
                }
                if (confiner != null)
                {
                    confiner.Damping = 0f;
                    confiner.SlowingDistance = 0f;
                }
                if (vcam != null) vcam.PreviousStateIsValid = false;
                if (brain != null) brain.ManualUpdate(Time.frameCount, -1f);
                yield return null;

                // Confiner baking is spread over frames so the loading UI keeps animating.
                if (confiner != null && confiner.isActiveAndEnabled && confiner.BoundingShape2D != null)
                {
                    while (!confiner.BoundingShapeIsBaked)
                    {
                        if (confiner.BakeBoundingShape(vcam, 0.004f)) break;
                        if (brain != null) brain.ManualUpdate(Time.frameCount, -1f);
                        yield return null;
                    }
                }
                if (vcam != null) vcam.PreviousStateIsValid = false;
                if (brain != null) brain.ResetState();
                int stableFrames = 0;
                Vector3 previousPosition = output != null ? output.transform.position : Vector3.zero;
                Quaternion previousRotation = output != null ? output.transform.rotation : Quaternion.identity;
                float previousSize = output != null ? output.orthographicSize : 0f;
                while (stableFrames < 8)
                {
                    if (brain != null) brain.ManualUpdate(Time.frameCount, stableFrames == 0 ? -1f : Time.unscaledDeltaTime);
                    yield return null;
                    bool stable = output == null || ((output.transform.position - previousPosition).sqrMagnitude < 0.000001f
                        && Quaternion.Angle(output.transform.rotation, previousRotation) < 0.01f
                        && Mathf.Abs(output.orthographicSize - previousSize) < 0.0001f);
                    stableFrames = stable ? stableFrames + 1 : 0;
                    if (output != null)
                    {
                        previousPosition = output.transform.position;
                        previousRotation = output.transform.rotation;
                        previousSize = output.orthographicSize;
                    }
                    Report(Mathf.Lerp(0.88f, 0.98f, stableFrames / 8f));
                }
            }
            finally
            {
                if (brain != null) { brain.UpdateMethod = oldMode; brain.IgnoreTimeScale = oldIgnoreTimeScale; }
                if (confiner != null) { confiner.Damping = oldDamping; confiner.SlowingDistance = oldSlowing; }
            }
        }

        private void Build()
        {
            var assets = Resources.Load<SceneLoadingAssets>("Loading/LoadingScreenAssets");
            font = assets != null ? assets.font : TMP_Settings.defaultFontAsset;
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            group = gameObject.AddComponent<CanvasGroup>();
            gameObject.AddComponent<GraphicRaycaster>();
            var background = gameObject.AddComponent<Image>();
            background.color = Color.black;
            background.raycastTarget = true;
            artworkRoot = Child("Loading Presentation", transform);
            Stretch(artworkRoot, Vector2.zero, Vector2.zero);

            var art = Child("Loading Artwork", artworkRoot).gameObject.AddComponent<RawImage>();
            art.texture = Resources.Load<Texture2D>("Loading/LoadingArtwork");
            art.raycastTarget = false;
            var aspect = art.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = art.texture != null ? (float)art.texture.width / art.texture.height : 16f / 9f;
            gradient = new Texture2D(1, 64, TextureFormat.RGBA32, false) { name = "Loading Shade" };
            gradient.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 64; y++) gradient.SetPixel(0, y, new Color(0.02f, 0.015f, 0.01f, Mathf.Lerp(0f, 0.82f, y / 63f)));
            gradient.Apply();
            Shade("Top Shade", new Vector2(0f, 0.71f), Vector2.one, false);
            Shade("Bottom Shade", Vector2.zero, new Vector2(1f, 0.25f), true);

            var title = Child("Loading Title", artworkRoot);
            Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(280f, 70f));
            var text = Label("Title", title, "로딩중", 44f);
            Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-27f, 0f), new Vector2(180f, 66f));
            for (int i = 0; i < 3; i++)
            {
                var dot = Label("Loading Dot " + (i + 1), title, ".", 44f);
                Place(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(66f + i * 17f, 0f), new Vector2(20f, 66f));
                dots[i] = dot.rectTransform;
            }

            var track = Child("Progress Track", artworkRoot);
            track.anchorMin = new Vector2(0.14f, 0f);
            track.anchorMax = new Vector2(0.86f, 0f);
            track.anchoredPosition = new Vector2(0f, 76f);
            track.sizeDelta = new Vector2(0f, 10f);
            var border = track.gameObject.AddComponent<Image>();
            border.color = new Color(0.74f, 0.57f, 0.31f, 0.8f);
            border.raycastTarget = false;
            var inner = Child("Progress Background", track);
            Stretch(inner, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            var dark = inner.gameObject.AddComponent<Image>();
            dark.color = new Color(0.08f, 0.06f, 0.04f, 0.95f);
            dark.raycastTarget = false;
            var fill = Child("Progress Fill", inner);
            Stretch(fill, Vector2.zero, Vector2.zero);
            progressFill = fill.gameObject.AddComponent<Image>();
            progressFill.color = Gold;
            progressFill.type = Image.Type.Simple;
            fill.anchorMax = new Vector2(0f, 1f);
            progressFill.fillAmount = 0f;
            progressFill.raycastTarget = false;
            percentage = Label("Progress Percentage", artworkRoot, "0%", 22f);
            Place(percentage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(130f, 34f));
            group.alpha = 0f;
            group.blocksRaycasts = false;
            canvas.enabled = false;
        }

        private void Shade(string name, Vector2 min, Vector2 max, bool flip)
        {
            var rect = Child(name, artworkRoot);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var shade = rect.gameObject.AddComponent<RawImage>();
            shade.texture = gradient;
            shade.uvRect = flip ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);
            shade.raycastTarget = false;
        }

        private static RectTransform Child(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return (RectTransform)child.transform;
        }

        private static TMP_Text Label(string name, Transform parent, string value, float size)
        {
            var text = Child(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = instance.font;
            text.text = value;
            text.fontSize = size;
            text.color = Gold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = min; rect.offsetMax = max;
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            GameTimeController.SetPaused(this, false);
            if (gradient != null) Destroy(gradient);
            instance = null;
        }
    }
}
