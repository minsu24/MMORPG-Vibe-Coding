using System;
using System.Collections;
using System.IO;
using System.Linq;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class LoadingScreenValidation
    {
        private const string Folder = "Artifacts/LoadingScreenValidation";
        private const string Key = "EasternFantasy.LoadingScreenValidation";
        private static bool started;

        [InitializeOnLoadMethod]
        private static void Install() => EditorApplication.update += StartWhenReady;

        public static void Prepare()
        {
            Directory.CreateDirectory(Folder + "/Saves");
            SessionState.SetString(Key + ".PreviousDirectory", SessionState.GetString("EasternFantasy.SaveTestDirectory", ""));
            SessionState.SetBool(Key + ".PreviousFreeze", SessionState.GetBool("EasternFantasy.SaveValidationFreeze", false));
            SessionState.SetBool(Key + ".PreviousBackground", Application.runInBackground);
            var id = CharacterSelectionState.SelectedClass;
            string source = SaveGameService.PathFor(id);
            SessionState.SetString(Key + ".OriginalPath", source);
            SessionState.SetString(Key + ".OriginalContents", File.Exists(source) ? File.ReadAllText(source) : "");
            var data = File.Exists(source) ? JsonUtility.FromJson<SaveGameData>(File.ReadAllText(source)) : new SaveGameData();
            data.characterClass = (int)id;
            data.scene = "PrologueVilage";
            data.position = new Vector3(0f, 1f, 0f);
            data.inventoryTutorial = data.statsTutorial = data.mapTutorial = data.shopTutorial = true;
            data.dungeonTutorial = data.upgradeTutorial = data.companionTutorial = data.companionGuide = true;
            data.firstDeathGuide = true;
            data.playedDialogues = Resources.Load<SaveAssetCatalog>("SaveAssetCatalog").entries
                .Where(e => e.asset is DialogueSequence).Select(e => e.id).ToArray();
            SessionState.SetString("EasternFantasy.SaveTestDirectory", Path.GetFullPath(Folder + "/Saves"));
            SessionState.SetBool("EasternFantasy.SaveValidationFreeze", false);
            File.WriteAllText(SaveGameService.PathFor(id), JsonUtility.ToJson(data, true));
            File.WriteAllText(Folder + "/Validation.txt", "Loading screen integration validation\n");
            SessionState.SetBool(Key, true);
            started = false;
        }

        public static void Cleanup()
        {
            string path = SessionState.GetString(Key + ".OriginalPath", "");
            string original = SessionState.GetString(Key + ".OriginalContents", "");
            bool untouched = string.IsNullOrEmpty(path) || (File.Exists(path) ? File.ReadAllText(path) : "") == original;
            Check(untouched, "original player save is unchanged");
            SessionState.SetString("EasternFantasy.SaveTestDirectory", SessionState.GetString(Key + ".PreviousDirectory", ""));
            SessionState.SetBool("EasternFantasy.SaveValidationFreeze", SessionState.GetBool(Key + ".PreviousFreeze", false));
            Application.runInBackground = SessionState.GetBool(Key + ".PreviousBackground", false);
            SessionState.SetBool(Key, false);
        }

        private static void StartWhenReady()
        {
            if (!Application.isPlaying || started || !SessionState.GetBool(Key, false) || SaveGameService.Instance == null) return;
            started = true;
            Application.runInBackground = true;
            SaveGameService.Instance.StartCoroutine(Validate());
        }

        private static IEnumerator Validate()
        {
            yield return Observe("SaveRestore");
            Check(SceneManager.GetActiveScene().name == "PrologueVilage", "save restore reaches the saved scene");
            yield return CheckCamera("SaveRestore");
            foreach (var destination in new[] { "MovementPrototype", "PrologueVilage", "SpiderCave" })
            {
                bool completed = false, success = false;
                string spawn = destination == "MovementPrototype" ? "Out_Tuto_Portal"
                    : destination == "PrologueVilage" ? "In_Vilage_Portal" : "In_SpiderCave_Portal";
                Check(PortalSceneTransition.TryLoad(destination, spawn, ok => { completed = true; success = ok; }), destination + ": transition accepted");
                Check(!PortalSceneTransition.TryLoad(destination, spawn), destination + ": duplicate transfer blocked");
                yield return Observe(destination, false);
                Check(completed && success && SceneManager.GetActiveScene().name == destination, destination + ": transfer completed");
                yield return CheckCamera(destination);
            }
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            player.KillForTesting();
            yield return new WaitForSecondsRealtime(1.5f);
            player.GetComponent<PlayerDeathController>().Respawn();
            yield return Observe("Respawn", false);
            Check(!player.IsDead && SceneManager.GetActiveScene().name == "PrologueVilage", "respawn reaches village without the artwork loading screen");
            yield return CheckCamera("Respawn");

            Check(PortalSceneTransition.TryLoad("ChooseCharactor", null), "character menu transfer accepted");
            yield return Observe("CharacterMenu", false);
            string path = SaveGameService.PathFor(CharacterSelectionState.SelectedClass);
            var data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path));
            data.scene = "PrologueVilage";
            data.position = new Vector3(0f, 1f, 0f);
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            var controller = UnityEngine.Object.FindFirstObjectByType<CharacterSelectionController>();
            var card = UnityEngine.Object.FindObjectsByType<CharacterSelectionCard>(FindObjectsSortMode.None)
                .First(c => c.Definition.ClassId == CharacterSelectionState.SelectedClass);
            controller.SelectClass(card.Definition);
            yield return Observe("SavedCharacterEntry");
            Check(SceneManager.GetActiveScene().name == "PrologueVilage", "character entry keeps artwork through the second saved-scene load");
            yield return CheckCamera("SavedCharacterEntry");
            File.AppendAllText(Folder + "/Validation.txt", "COMPLETE\n");
            SessionState.SetBool(Key, false);
            GameTimeController.SetPaused(Key, true);
        }

        private static IEnumerator Observe(string label, bool expectArtwork = true)
        {
            float deadline = Time.realtimeSinceStartup + 25f;
            float previous = -1f;
            bool sawVisible = false, captured = false, sawPause = false, animatedDots = false;
            Vector3? firstDotHeights = null;
            while (SceneLoadingScreen.IsVisible || SaveGameService.IsRestoring || PortalSceneTransition.IsTransitioning)
            {
                Check(Time.realtimeSinceStartup < deadline, label + ": no loading timeout", false);
                if (SceneLoadingScreen.IsVisible)
                {
                    sawVisible = true;
                    Check(SceneLoadingScreen.IsArtworkVisible == expectArtwork, label + ": presentation matches first-entry-only rule", false);
                    sawPause |= Time.timeScale == 0f;
                    float progress = SceneLoadingScreen.Progress;
                    Check(progress + 0.0001f >= previous, label + ": progress never moves backwards", false);
                    previous = progress;
                    var root = UnityEngine.Object.FindFirstObjectByType<SceneLoadingScreen>();
                    var title = root.transform.Find("Loading Presentation/Loading Title");
                    var heights = new Vector3(((RectTransform)title.Find("Loading Dot 1")).anchoredPosition.y,
                        ((RectTransform)title.Find("Loading Dot 2")).anchoredPosition.y,
                        ((RectTransform)title.Find("Loading Dot 3")).anchoredPosition.y);
                    if (!firstDotHeights.HasValue) firstDotHeights = heights;
                    animatedDots |= (heights - firstDotHeights.Value).sqrMagnitude > 1f;
                    var group = root.GetComponent<CanvasGroup>();
                    Check(group.blocksRaycasts, label + ": input remains covered", false);
                    if (!captured && group.alpha > 0.99f && progress > 0.1f)
                    {
                        ScreenCapture.CaptureScreenshot(Folder + "/" + label + ".png");
                        captured = true;
                    }
                }
                yield return null;
            }
            Check(sawVisible && captured, label + ": " + (expectArtwork ? "artwork" : "black fade") + " covers loading and screenshot captured");
            Check(sawPause && (!expectArtwork || animatedDots), label + ": gameplay paused" + (expectArtwork ? " and dots animate" : " without loading artwork"));
            if (expectArtwork) Check(previous > 0.99f, label + ": loading reaches 100% before fade completes");
            Check(Time.timeScale > 0f, label + ": loading pause released");
        }

        private static IEnumerator CheckCamera(string label)
        {
            var camera = Camera.main;
            var vcam = UnityEngine.Object.FindFirstObjectByType<CinemachineCamera>();
            var confiner = vcam != null ? vcam.GetComponent<CinemachineConfiner2D>() : null;
            Check(camera != null && confiner != null && confiner.BoundingShapeIsBaked, label + ": confiner is baked before reveal");
            bool inside = true;
            for (int frame = 0; frame < 12; frame++)
            {
                foreach (float x in new[] { 0.001f, 0.999f })
                    foreach (float y in new[] { 0.001f, 0.999f })
                        inside &= confiner.BoundingShape2D.OverlapPoint(camera.ViewportToWorldPoint(new Vector3(x, y, -camera.transform.position.z)));
                yield return null;
            }
            Check(inside, label + ": all viewport corners stay inside CameraCollider after reveal");
        }

        private static void Check(bool ok, string message, bool logSuccess = true)
        {
            if (logSuccess || !ok) File.AppendAllText(Folder + "/Validation.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) throw new InvalidOperationException(message);
        }
    }
}
