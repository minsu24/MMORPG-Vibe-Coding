using System;
using System.Collections;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;
using EasternFantasy.UI;

// One persistent overlay owns portal transitions across scene destruction.
public sealed class PortalSceneTransition : MonoBehaviour
{
    private static PortalSceneTransition instance;
    public static bool IsTransitioning => instance != null && instance.busy;
    private bool busy;

    public static bool TryLoad(string sceneName, string spawnPointName, Action<bool> completed = null,
        bool showLoadingScreen = false, bool playPortalAudio = false)
    {
        if (IsTransitioning || SceneLoadingScreen.IsVisible) return false;
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
        }
        SaveGameService.SaveNow(out _);
        instance.busy = true;
        if (playPortalAudio) PortalAudioCue.Play("Audio/PortalEnter", .081f, .7f, 1.2f, persistent: true);
        instance.StartCoroutine(instance.Transfer(sceneName, spawnPointName, completed, showLoadingScreen, playPortalAudio));
        return true;
    }

    private IEnumerator Transfer(string sceneName, string spawnPointName, Action<bool> completed, bool showLoadingScreen, bool playPortalAudio)
    {
        bool loaded = false;
        var player = FindFirstObjectByType<PlayerMovement2D>();
        MountainPassArrival arrival = null;
        float? fogOrigin = null;
        if (playPortalAudio && MountainPassArrival.ShouldPlay(SceneManager.GetActiveScene().name, sceneName, spawnPointName))
        {
            arrival = MountainPassArrival.Begin(transform);
            var camera = Camera.main;
            fogOrigin = camera != null && player != null ? camera.WorldToViewportPoint(player.transform.position).x : 0.5f;
        }
        SceneLoadingScreen.Begin(showArtwork: showLoadingScreen, mountainFogOrigin: fogOrigin);
        if (player != null) player.ClearInput();
        GameTimeController.SetPaused(this, true);
        try
        {
            yield return SceneLoadingScreen.WaitUntilCovered();
            // Render the fully covered artwork before starting any scene work.
            yield return null;
            SceneLoadingScreen.Report(0.08f);
            MapTransferData.SetTarget(spawnPointName);
            AsyncOperation operation = null;
            try
            {
                SceneWorldState.CaptureActiveScene();
                operation = SceneManager.LoadSceneAsync(sceneName);
            }
            catch (Exception exception) { Debug.LogException(exception); }
            if (operation != null)
            {
                while (!operation.isDone)
                {
                    SceneLoadingScreen.Report(0.08f + Mathf.Clamp01(operation.progress / 0.9f) * 0.57f);
                    yield return null;
                }
                loaded = true;
                yield return null;
                // Selecting a saved character may load a second scene and restore a saved position.
                while (SaveGameService.IsRestoring) yield return null;
                SceneLoadingScreen.Report(0.8f);
            }
            else
            {
                SceneWorldState.CancelCapture();
                MapTransferData.Clear();
            }
            yield return SceneLoadingScreen.Finish(() =>
            {
                if (!loaded) return;
                if (arrival != null) arrival.Reveal();
                else if (playPortalAudio) PortalAudioCue.Play("Audio/PortalExit", .072f, .05f, .5f, persistent: true);
            });
            if (loaded && arrival != null)
                MountainPassArrival.HasPlayed = true;
        }
        finally
        {
            Release();
            if (arrival != null)
            {
                if (loaded && MountainPassArrival.HasPlayed) SaveGameService.SaveNow(out _);
                else
                {
                    arrival.gameObject.SetActive(false);
                    Destroy(arrival.gameObject);
                }
            }
            completed?.Invoke(loaded);
        }
    }

    private void Release()
    {
        SceneLoadingScreen.Cancel();
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
