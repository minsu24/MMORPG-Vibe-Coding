using System;
using System.IO;
using System.Reflection;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EasternFantasy.EditorTools
{
    [InitializeOnLoad]
    public static class SoulstoneLiveValidation
    {
        private const string State = "Soulstone.LiveValidation";
        private const string Report = "Artifacts/Soulstone/Validation-report.txt";
        private static int phase;
        private static double started;
        private static string origin, destination;
        private static Keyboard keyboard;
        private static PlayerMovement2D player;

        static SoulstoneLiveValidation()
        {
            EditorApplication.playModeStateChanged += Changed;
            if (SessionState.GetBool(State, false) && EditorApplication.isPlaying)
            { started = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; }
        }

        public static void RunIfRequested()
        {
            if (Application.isPlaying || !File.Exists("Artifacts/Soulstone/Validate.request")) return;
            string request = File.ReadAllText("Artifacts/Soulstone/Validate.request");
            const string consumed = "Artifacts/Soulstone/Validate.completed";
            if ((File.Exists(consumed) && File.ReadAllText(consumed) == request)
                || SessionState.GetString("Soulstone.ValidationRequest", "") == request) return;
            File.WriteAllText(consumed, request);
            SessionState.SetString("Soulstone.ValidationRequest", request);
            string source = SceneManager.GetActiveScene().name;
            SessionState.SetString("Soulstone.ValidationOrigin", source == "TempleGate" ? "TempleGate" : "PrologueVilage");
            SessionState.SetString("Soulstone.PreviousSaveDirectory", SessionState.GetString("EasternFantasy.SaveTestDirectory", ""));
            SessionState.SetString("EasternFantasy.SaveTestDirectory", Path.GetFullPath("Artifacts/Soulstone/ValidationSaves"));
            SessionState.SetBool(State, true);
            File.WriteAllText(Report, "RUNNING: isolated G/ESC, near/far interaction, current-map restriction, and round-trip travel validation.\n");
            EditorApplication.isPlaying = true;
        }

        private static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(State, false))
            {
                phase = 0;
                started = EditorApplication.timeSinceStartup;
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
                return;
            }
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(State, false)) return;
            SessionState.SetString("EasternFantasy.SaveTestDirectory", SessionState.GetString("Soulstone.PreviousSaveDirectory", ""));
            SessionState.SetBool(State, false);
            EditorApplication.update -= Tick;
        }

        private static void Note(string message) => File.AppendAllText(Report, message + "\n");
        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); Note("PASS: " + message); }

        private static void Tick()
        {
            if (!Application.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                if (EditorApplication.timeSinceStartup - started > 150) throw new TimeoutException("Live travel validation timed out in phase " + phase);
                switch (phase)
                {
                    case 0:
                        Application.runInBackground = true;
                        origin = SessionState.GetString("Soulstone.ValidationOrigin", "PrologueVilage");
                        SceneManager.LoadScene("MovementPrototype");
                        phase = 10; return;
                    case 10:
                        if (SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible || PortalSceneTransition.IsTransitioning) return;
                        player = Object.FindFirstObjectByType<PlayerMovement2D>();
                        if (player == null)
                        {
                            if (EasternFantasy.Quest.QuestManager.Instance == null)
                                Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/QuestManager.prefab"));
                            var definition = AssetDatabase.LoadAssetAtPath<CharacterClassDefinition>("Assets/_Project/Data/Characters/Dosa.asset");
                            Object.Instantiate(definition.PlayerPrefab);
                            return;
                        }
                        DismissTutorials();
                        SceneManager.LoadSceneAsync(origin);
                        phase = 1; return;
                    case 1:
                        if (SceneManager.GetActiveScene().name != origin || SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible || PortalSceneTransition.IsTransitioning) return;
                        player = Object.FindFirstObjectByType<PlayerMovement2D>();
                        if (player == null || SoulstoneWindowUI.Instance == null) return;
                        DismissTutorials();
                        origin = SceneManager.GetActiveScene().name;
                        // This suite tests the unlocked travel window; the separate tutorial suite
                        // covers the fresh-character activation flow and its animation sequence.
                        typeof(SoulstoneProgress).GetMethod("RestoreSave", BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null,
                            new object[] { new SaveGameData { soulstoneActivated = true, soulstoneTutorial = true, soulstoneVillageAwakened = true } });
                        destination = origin == "TempleGate" ? "PrologueVilage" : "TempleGate";
                        var stone = SoulstoneWindowUI.FindArrivalStone(SceneManager.GetActiveScene());
                        Check(stone != null, "source soulstone resolved");
                        player.GetComponent<PlayerLocationSetter>().TeleportToAndRetarget(stone.transform);
                        var near = player.transform.position;
                        player.TeleportTo(near + Vector3.right * 30, Quaternion.identity);
                        Check(!SoulstoneWindowUI.Instance.TryOpen(), "G window cannot open far from a soulstone");
                        player.TeleportTo(near, Quaternion.identity);
                        keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G));
                        phase = 2; return;
                    case 2:
                        if (!SoulstoneWindowUI.IsOpen) return;
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        Check(Time.timeScale == 0 && player.GetComponent<PlayerInputReader>().IsInputBlocked, "G opens and pauses gameplay");
                        SoulstoneWindowUI.Instance.Select(origin);
                        Check(!SoulstoneWindowUI.Instance.Travel(), "current-map travel is blocked");
                        SoulstoneWindowUI.Instance.Select(destination);
                        Check(SoulstoneWindowUI.Instance.Travel(), "outbound soulstone travel starts");
                        phase = 3; return;
                    case 3:
                        if (PortalSceneTransition.IsTransitioning || SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible || SceneManager.GetActiveScene().name != destination) return;
                        Check(SoulstoneWindowUI.FindArrivalStone(SceneManager.GetActiveScene()) != null, "destination soulstone resolved after scene load");
                        DismissTutorials();
                        Check(SoulstoneWindowUI.Instance.TryOpen(), "player arrives within destination interaction range");
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                        phase = 4; return;
                    case 4:
                        if (SoulstoneWindowUI.IsOpen) return;
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        Check(Time.timeScale > 0 && !player.GetComponent<PlayerInputReader>().IsInputBlocked, "ESC restores time and player input");
                        Check(SoulstoneWindowUI.Instance.TryOpen(), "destination window reopens");
                        SoulstoneWindowUI.Instance.Select(origin);
                        Check(SoulstoneWindowUI.Instance.Travel(), "return soulstone travel starts");
                        phase = 5; return;
                    case 5:
                        if (PortalSceneTransition.IsTransitioning || SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible || SceneManager.GetActiveScene().name != origin) return;
                        Check(SoulstoneWindowUI.Instance.TryOpen(), "round trip returns to the original soulstone");
                        SoulstoneWindowUI.Instance.Close();
                        Check(Time.timeScale > 0 && !player.GetComponent<PlayerInputReader>().IsInputBlocked, "round-trip completion releases input and pause");
                        Note("PASS: ALL LIVE SOULSTONE CHECKS COMPLETED. Original editor scenes and save directory restored on exiting play mode.");
                        EditorApplication.update -= Tick;
                        EditorApplication.isPlaying = false;
                        return;
                }
            }
            catch (Exception exception)
            {
                Note("FAILED phase " + phase + ": " + exception);
                Debug.LogException(exception);
                EditorApplication.update -= Tick;
                EditorApplication.isPlaying = false;
            }
        }

        private static void DismissTutorials()
        {
            DialogueManager.Instance?.EndDialogue();
            foreach (var type in new[] { typeof(MapTutorialUI), typeof(StatsSkillTutorialUI), typeof(InventoryEquipmentTutorialUI) })
            {
                var active = type.GetField("active", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
                if (active != null)
                {
                    var end = type.GetMethod("End", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? type.GetMethod("Finish", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    end?.Invoke(active, null);
                }
                type.GetMethod("RestoreSave", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(null,
                    new object[] { new SaveGameData { mapTutorial = true, statsTutorial = true, inventoryTutorial = true } });
            }
        }
    }
}
