using System;
using System.Collections;
using System.IO;
using System.Linq;
using EasternFantasy.Saving;
using EasternFantasy.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class LoadingUIVisibilityValidation
    {
        private const string Report = "Artifacts/LoadingUIVisibility/Validation.txt";
        public static void Run()
        {
            Directory.CreateDirectory("Artifacts/LoadingUIVisibility");
            File.WriteAllText(Report, "Loading UI visibility native validation\n");
            Application.runInBackground = true;
            SaveGameService.Instance.StartCoroutine(Validate());
        }

        private static IEnumerator Validate()
        {
            while (SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible) yield return null;
            SaveGameService.BeginAdminSession();
            var root = new GameObject("Loading UI Test Fixtures");
            UnityEngine.Object.DontDestroyOnLoad(root);
            var shown = MakeCanvas(root, "Shown overlay", RenderMode.ScreenSpaceOverlay);
            shown.sortingOrder = short.MaxValue;
            var inactive = MakeCanvas(root, "Inactive camera UI", RenderMode.ScreenSpaceCamera);
            inactive.gameObject.SetActive(false);
            var disabled = MakeCanvas(root, "Already disabled UI", RenderMode.WorldSpace);
            disabled.enabled = false;
            var text = new GameObject("World text", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            text.transform.SetParent(root.transform);
            text.text = "Loading visibility test";
            var renderer = text.GetComponent<Renderer>();
            try
            {
                SceneLoadingScreen.Begin(true);
                Check(!shown.enabled && !shown.GetComponent<GraphicRaycaster>().enabled, "existing UI and raycasts suppressed immediately");
                Check(renderer.forceRenderingOff, "world-space TMP text hidden immediately");
                var late = MakeCanvas(root, "Late world UI", RenderMode.WorldSpace);
                var nested = MakeCanvas(late.gameObject, "Nested sorting override", RenderMode.WorldSpace);
                nested.overrideSorting = true;
                nested.sortingOrder = short.MaxValue;
                inactive.gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                Check(!late.enabled && !nested.enabled && !inactive.enabled, "new, nested, and newly activated UI hidden before rendering");
                shown.enabled = true;
                Canvas.ForceUpdateCanvases();
                Check(!shown.enabled, "UI re-enabled by another controller is suppressed again");
                yield return SceneManager.LoadSceneAsync("MovementPrototype");
                yield return null;
                Canvas.ForceUpdateCanvases();
                CheckOnlyLoadingUI();
                Check(SceneLoadingScreen.IsArtworkVisible, "loading artwork remains visible across scene load");
                SceneLoadingScreen.Report(1f);
                yield return SceneLoadingScreen.Finish();
                Check(!SceneLoadingScreen.IsVisible && shown.enabled && late.enabled && nested.enabled && inactive.enabled,
                    "UI restored after loading fade completes");
                Check(!disabled.enabled && !renderer.forceRenderingOff && shown.GetComponent<GraphicRaycaster>().enabled,
                    "original disabled state retained; text and raycasts restored");
                SceneLoadingScreen.Begin(false, false);
                Check(!shown.enabled, "ordinary black-fade transitions hide UI immediately");
                yield return null;
                CheckOnlyLoadingUI();
                SceneLoadingScreen.Cancel();
                Check(shown.enabled && !disabled.enabled, "cancel restores UI correctly");
                UnityEngine.Object.Destroy(shown.gameObject);
                yield return null;
                SceneLoadingScreen.Begin(true, false);
                SceneLoadingScreen.Cancel();
                Check(!SceneLoadingScreen.IsVisible, "destroyed UI references safely ignored on subsequent loads");
                File.AppendAllText(Report, "COMPLETE\n");
            }
            finally
            {
                SceneLoadingScreen.Cancel();
                UnityEngine.Object.Destroy(root);
            }
        }

        private static Canvas MakeCanvas(GameObject root, string name, RenderMode mode)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.transform.SetParent(root.transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = mode;
            return canvas;
        }

        private static void CheckOnlyLoadingUI()
        {
            var loading = UnityEngine.Object.FindFirstObjectByType<SceneLoadingScreen>();
            Check(loading.GetComponent<Canvas>().enabled, "loading Canvas still renders");
            Check(!UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Any(c => c.enabled && !c.transform.IsChildOf(loading.transform)), "all scene and persistent game Canvases hidden");
        }

        private static void Check(bool ok, string message)
        {
            File.AppendAllText(Report, (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) throw new InvalidOperationException(message);
        }
    }
}
