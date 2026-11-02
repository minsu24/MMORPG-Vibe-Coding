using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dungeon;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class DungeonHotkeyValidation
    {
        private const string Folder = "Artifacts/DungeonHotkeyValidation";
        private const string Report = Folder + "/Validation.txt";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticHidden = BindingFlags.Static | BindingFlags.NonPublic;
        private static Keyboard keyboard;
        private static void Check(bool condition, string name)
        {
            File.AppendAllText(Report, (condition ? "PASS " : "FAIL ") + name + "\n");
            if (!condition) throw new InvalidOperationException(name);
        }
        public static void Prepare()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Prepare outside Play Mode.");
            Directory.CreateDirectory(Folder);
            if (SessionState.GetString("EasternFantasy.SaveTestDirectory", "") != Path.GetFullPath(Folder + "/Saves"))
                SessionState.SetString("DungeonValidation.PreviousSaveDirectory", SessionState.GetString("EasternFantasy.SaveTestDirectory", ""));
            SessionState.SetString("EasternFantasy.SaveTestDirectory", Path.GetFullPath(Folder + "/Saves"));
            Directory.CreateDirectory(SaveGameService.StorageDirectory);
            var seed = new SaveGameData { characterClass = (int)CharacterSelectionState.SelectedClass,
                scene = "GapInSpace", position = new Vector3(0, 3, 0), level = 20,
                health = 500, mana = 500, mental = 500, energy = 100,
                inventoryTutorial = true, statsTutorial = true, mapTutorial = true,
                companionTutorial = true, companionGuide = true, shopTutorial = true, upgradeTutorial = true };
            File.WriteAllText(SaveGameService.PathFor(CharacterSelectionState.SelectedClass), JsonUtility.ToJson(seed));
            File.WriteAllText(Report, "Dungeon shortcut integration validation\n");
        }
        public static void RestoreDirectory() => SessionState.SetString("EasternFantasy.SaveTestDirectory", SessionState.GetString("DungeonValidation.PreviousSaveDirectory", ""));
        public static void RunFirstSession()
        {
            EditorApplication.LockReloadAssemblies();
            SaveGameService.Instance.StartCoroutine(FirstSession());
        }
        public static void RunSecondSession()
        {
            EditorApplication.LockReloadAssemblies();
            SaveGameService.Instance.StartCoroutine(SecondSession());
        }
        private static IEnumerator Ready()
        {
            float deadline = Time.realtimeSinceStartup + 45f;
            do { yield return null; }
            while ((SaveGameService.IsRestoring || PortalSceneTransition.IsTransitioning || SceneLoadingScreen.IsVisible)
                && Time.realtimeSinceStartup < deadline);
            Check(!SaveGameService.IsRestoring && !PortalSceneTransition.IsTransitioning && !SceneLoadingScreen.IsVisible, "scene transition finishes");
        }
        private static void ProtectPlayer()
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            player.isInvincible = true;
            player.GetComponent<Rigidbody2D>().simulated = false;
        }
        private static IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            InputSystem.Update();
            keyboard.MakeCurrent();
            // Drive the production input handler before native keyboard focus events can replace the test device.
            typeof(DungeonWindowUI).GetMethod("Update", Hidden).Invoke(DungeonWindowUI.Instance, null);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            yield return null; yield return null;
        }
        private static void RestoreUI(SaveGameData data) => typeof(DungeonWindowUI).GetMethod("RestoreSave", StaticHidden).Invoke(null, new object[] { data });
        private static SaveGameData CaptureUI()
        {
            var data = new SaveGameData();
            typeof(DungeonWindowUI).GetMethod("CaptureSave", StaticHidden).Invoke(null, new object[] { data });
            return data;
        }
        private static IEnumerator FirstSession()
        {
            Application.runInBackground = true;
            keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return SceneManager.LoadSceneAsync("MovementPrototype");
                yield return Ready(); ProtectPlayer();
                var window = DungeonWindowUI.Instance;
                Check(SceneManager.GetActiveScene().name == "GapInSpace" && window != null, "saved entrance scene loads and window bootstraps");
                Check(!DungeonWindowUI.IsUnlocked && !window.IsOpen, "new character starts locked");
                yield return Press(Key.O);
                Check(!window.IsOpen, "O does not open before discovery");
                if (EasternFantasy.Dialogue.DialogueManager.Instance != null && EasternFantasy.Dialogue.DialogueManager.Instance.IsDialogueActive)
                {
                    EasternFantasy.Dialogue.DialogueManager.Instance.EndDialogue();
                    yield return new WaitForSecondsRealtime(.3f);
                }
                var portal = UnityEngine.Object.FindFirstObjectByType<DungeonPortal>();
                var player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement2D>();
                Check(portal != null && portal.Interact(player), "first entrance interaction opens window");
                Check(DungeonWindowUI.IsUnlocked && DungeonWindowUI.IsTutorialRunning, "first interaction unlocks shortcut and starts tutorial");
                var partial = CaptureUI();
                Check(partial.dungeonDiscovered && !partial.dungeonTutorial, "discovery persists independently of incomplete tutorial");
                var tutorial = (PetCompanionTutorialUI)typeof(DungeonWindowUI).GetField("tutorial", Hidden).GetValue(window);
                var messages = (string[])typeof(PetCompanionTutorialUI).GetField("activeMessages", Hidden).GetValue(tutorial);
                Check(messages.Any(m => m.Contains("O키")), "tutorial explains O shortcut");
                for (int i = 0; i < 5; i++) yield return Press(Key.Space);
                Check(!DungeonWindowUI.IsTutorialRunning && player.GetComponent<PlayerInputReader>().enabled, "tutorial completes and restores player input");
                yield return Press(Key.O); Check(!window.IsOpen, "O closes window");
                yield return Press(Key.O); Check(window.IsOpen, "O reopens window");
                yield return Press(Key.Escape); Check(!window.IsOpen && Time.timeScale > 0f, "Escape closes and releases pause");

                RestoreUI(new SaveGameData());
                Check(!DungeonWindowUI.IsUnlocked, "fresh save clears shortcut");
                RestoreUI(new SaveGameData { dungeonTutorial = true });
                Check(DungeonWindowUI.IsUnlocked, "legacy completed tutorial grants discovery");
                RestoreUI(new SaveGameData { dungeonDiscovered = true, dungeonTutorial = true });
                yield return Press(Key.O); Check(window.IsOpen, "restored discovery opens with O");

                var definition = window.Selected;
                var blocked = UnityEngine.Object.Instantiate(definition);
                blocked.minimumLevel = 21;
                Check(!window.CanEnter(blocked), "insufficient level blocks entry");
                blocked.minimumLevel = 1; blocked.available = false;
                Check(!window.CanEnter(blocked), "unavailable dungeon blocks entry");
                blocked.available = true; blocked.sceneName = "";
                Check(!window.CanEnter(blocked), "unconfigured scene blocks entry");
                UnityEngine.Object.Destroy(blocked);
                Check(window.CanEnter(definition) && window.enterButton.interactable, "valid dungeon allows entry");
                window.Close();
                Check(PortalSceneTransition.TryLoad("PrologueVilage", "In_Vilage_Portal"), "travel away from entrance");
                yield return Ready(); ProtectPlayer();
                Check(window == DungeonWindowUI.Instance, "same window survives map transfer");
                yield return Press(Key.O);
                Check(window.IsOpen && !DungeonWindowUI.IsTutorialRunning, "O opens remotely without repeating tutorial");
                UnityEngine.ScreenCapture.CaptureScreenshot(Folder + "/RemoteDungeonWindow.png");
                yield return null;
                window.EnterSelected();
                yield return Ready(); ProtectPlayer();
                Check(SceneManager.GetActiveScene().name == definition.sceneName && DungeonRunController.Instance != null, "remote entry loads dungeon and starts run");
                Check(!window.IsOpen, "entry closes persistent window");
                yield return Press(Key.O); Check(!window.IsOpen, "O is blocked during dungeon run");
                var run = DungeonRunController.Instance;
                float before = run.RemainingSeconds;
                GameTimeController.SetPaused(window, true);
                yield return new WaitForSecondsRealtime(.3f);
                GameTimeController.SetPaused(window, false);
                Check(run.RemainingSeconds < before - .15f, "dungeon timer advances while menus pause gameplay");
                run.ExitToVillage();
                yield return Ready(); ProtectPlayer();
                Check(SceneManager.GetActiveScene().name == "PrologueVilage" && DungeonRunController.Instance == null, "exit returns to village and ends run");
                yield return Press(Key.O); Check(window.IsOpen, "shortcut works after exiting dungeon"); window.Close();
                Check(PortalSceneTransition.TryLoad("GapInSpace", ""), "return to entrance");
                yield return Ready(); ProtectPlayer();
                yield return null;
                Check(UnityEngine.Object.FindObjectsByType<DungeonWindowUI>(FindObjectsSortMode.None).Length == 1, "entrance revisit has one window instance");
                portal = UnityEngine.Object.FindFirstObjectByType<DungeonPortal>();
                Check(portal.Interact(player), "entrance uses surviving singleton after revisit"); window.Close();
                Check(PortalSceneTransition.TryLoad("PrologueVilage", "In_Vilage_Portal"), "return to village for restart save");
                yield return Ready(); ProtectPlayer();
                Check(SaveGameService.SaveNow(out string saveMessage), "write isolated save: " + saveMessage);
                File.AppendAllText(Report, "FIRST SESSION COMPLETE\n");
            }
            finally { if (keyboard != null) InputSystem.RemoveDevice(keyboard); keyboard = null; EditorApplication.UnlockReloadAssemblies(); }
        }
        private static IEnumerator SecondSession()
        {
            Application.runInBackground = true;
            keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return SceneManager.LoadSceneAsync("MovementPrototype");
                yield return Ready(); ProtectPlayer();
                Check(SceneManager.GetActiveScene().name == "PrologueVilage", "restart restores remote saved map");
                Check(DungeonWindowUI.IsUnlocked && DungeonWindowUI.Instance != null, "restart bootstraps window and restores discovery");
                yield return Press(Key.O);
                Check(DungeonWindowUI.Instance.IsOpen && !DungeonWindowUI.IsTutorialRunning, "O works after full Play Mode restart without replaying tutorial");
                DungeonWindowUI.Instance.Close();
                File.AppendAllText(Report, "SECOND SESSION COMPLETE\n");
            }
            finally { if (keyboard != null) InputSystem.RemoveDevice(keyboard); keyboard = null; EditorApplication.UnlockReloadAssemblies(); }
        }
    }
}
