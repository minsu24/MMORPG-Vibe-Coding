using System;
using System.Collections;
using EasternFantasy.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One persistent overlay owns portal transitions across scene destruction.
public sealed class PortalSceneTransition : MonoBehaviour
{
    private const float FadeOutSeconds = 0.28f;
    private const float FadeInSeconds = 0.35f;
    private static PortalSceneTransition instance;
    private Canvas overlay;
    private CanvasGroup shade;
    public static bool IsTransitioning => instance != null && instance.busy;
    private bool busy;

    public static bool TryLoad(string sceneName, string spawnPointName, Action<bool> completed = null)
    {
        if (IsTransitioning) return false;
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Portal transition cannot load scene '{sceneName}'. Check Build Settings.");
            return false;
        }
        if (instance == null)
        {
            var root = new GameObject("Portal Scene Transition", typeof(RectTransform));
            instance = root.AddComponent<PortalSceneTransition>();
            DontDestroyOnLoad(root);
            instance.BuildOverlay();
        }
        instance.busy = true;
        instance.StartCoroutine(instance.Transfer(sceneName, spawnPointName, completed));
        return true;
    }

    private void BuildOverlay()
    {
        overlay = gameObject.AddComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = short.MaxValue;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        shade = gameObject.AddComponent<CanvasGroup>();
        shade.alpha = 0f;
        shade.blocksRaycasts = false;
        gameObject.AddComponent<GraphicRaycaster>();
        var image = gameObject.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;
        overlay.enabled = false;
    }

    private IEnumerator Transfer(string sceneName, string spawnPointName, Action<bool> completed)
    {
        bool loaded = false;
        overlay.enabled = true;
        shade.blocksRaycasts = true;
        var player = FindFirstObjectByType<PlayerMovement2D>();
        if (player != null) player.ClearInput();
        GameTimeController.SetPaused(this, true);
        try
        {
            yield return Fade(1f, FadeOutSeconds);
            // Render a fully black frame before starting any scene work.
            yield return null;
            MapTransferData.SetTarget(spawnPointName);
            AsyncOperation operation = null;
            try { operation = SceneManager.LoadSceneAsync(sceneName); }
            catch (Exception exception) { Debug.LogException(exception); }
            if (operation != null)
            {
                while (!operation.isDone) yield return null;
                loaded = true;
                // Let sceneLoaded handlers, Start and the camera settle beneath the overlay.
                yield return null;
                yield return null;
            }
            else MapTransferData.Clear();
            yield return Fade(0f, FadeInSeconds);
        }
        finally
        {
            Release();
            completed?.Invoke(loaded);
        }
    }

    private IEnumerator Fade(float target, float duration)
    {
        float start = shade.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            shade.alpha = Mathf.Lerp(start, target, progress);
            yield return null;
        }
        shade.alpha = target;
    }

    private void Release()
    {
        if (shade != null) { shade.alpha = 0f; shade.blocksRaycasts = false; }
        if (overlay != null) overlay.enabled = false;
        GameTimeController.SetPaused(this, false);
        busy = false;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        Release();
        instance = null;
    }
}
