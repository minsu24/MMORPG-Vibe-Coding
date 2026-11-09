using System;
using System.IO;
using System.Reflection;
using EasternFantasy.CharacterSelection;
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
    public static class SoulstoneTutorialValidation
    {
        private const string State = "Soulstone.TutorialValidation";
        private const string Report = "Artifacts/Soulstone/Tutorial-validation.txt";
        private static int phase;
        private static double started, sampleAt;
        private static PlayerMovement2D player;
        private static Keyboard keyboard;
        private static Sprite dormantSprite;
        private static bool sawVillageActivation;
        private static float originalCameraSize;
        private static Vector3 originalCameraPosition;
        private static bool capturedCloseup;

        static SoulstoneTutorialValidation()
        {
            EditorApplication.playModeStateChanged += Changed;
            if (SessionState.GetBool(State, false) && EditorApplication.isPlaying)
            { started = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; }
        }
        public static void Run()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            Directory.CreateDirectory("Artifacts/Soulstone");
            SessionState.SetString(State + ".Save", SessionState.GetString("EasternFantasy.SaveTestDirectory", ""));
            SessionState.SetString("EasternFantasy.SaveTestDirectory", Path.GetFullPath("Artifacts/Soulstone/TutorialTest-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")));
            SessionState.SetBool(State, true);
            File.WriteAllText(Report, "RUNNING: isolated fresh-character Soulstone tutorial and animation verification.\n");
            EditorApplication.isPlaying = true;
        }
        private static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(State, false))
            {
                phase = 0; sawVillageActivation = false; capturedCloseup = false; started = EditorApplication.timeSinceStartup;
                EditorApplication.update -= Tick; EditorApplication.update += Tick;
            }
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(State, false)) return;
            SessionState.SetString("EasternFantasy.SaveTestDirectory", SessionState.GetString(State + ".Save", ""));
            SessionState.SetBool(State, false); EditorApplication.update -= Tick;
        }
        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); File.AppendAllText(Report, "PASS: " + message + "\n"); }
        private static bool Loading => SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible || PortalSceneTransition.IsTransitioning;
        private static SoulstoneActivationVisual Visual => SoulstoneWindowUI.FindArrivalStone(SceneManager.GetActiveScene())?.GetComponent<SoulstoneActivationVisual>();
        private static PetCompanionTutorialUI Guide => (PetCompanionTutorialUI)typeof(SoulstoneTutorialUI).GetField("guide", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(SoulstoneTutorialUI.Instance);
        private static SoulstoneWindowView View => SoulstoneWindowUI.Instance.GetComponentInChildren<SoulstoneWindowView>(true);
        private static void QuietOtherGuides() => typeof(SoulstoneLiveValidation).GetMethod("DismissTutorials", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        private static void RestoreProgress(SaveGameData data) => typeof(SoulstoneProgress).GetMethod("RestoreSave", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { data });
        private static void TeleportNear() => player.GetComponent<PlayerLocationSetter>().TeleportToAndRetarget(SoulstoneWindowUI.FindArrivalStone(SceneManager.GetActiveScene()).transform);
        private static string OpenDiagnostic()
        {
            var reader = player.GetComponent<PlayerInputReader>();
            return JsonUtility.ToJson(new OpenState {
                control = reader.CanControlGameplay, blocked = reader.IsInputBlocked,
                focused = (bool)typeof(PlayerInputReader).GetField("hasFocus", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(reader),
                otherWindow = (bool)typeof(PlayerInputReader).GetProperty("IsGameplayWindowOpen", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null),
                near = (bool)typeof(SoulstoneWindowUI).GetMethod("IsNearCurrentStone", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(SoulstoneWindowUI.Instance, new object[] { player }),
                time = Time.timeScale, restoring = SaveGameService.IsRestoring, loading = SceneLoadingScreen.IsVisible
            });
        }
        [Serializable] private sealed class OpenState
        { public bool control, blocked, focused, otherWindow, near, restoring, loading; public float time; }

        private static void Tick()
        {
            if (!Application.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                if (EditorApplication.timeSinceStartup - started > 160) throw new TimeoutException("Tutorial validation timed out in phase " + phase);
                switch (phase)
                {
                    case 0:
                        Application.runInBackground = true; SceneManager.LoadScene("MovementPrototype"); phase = 10; return;
                    case 10:
                        if (Loading) return;
                        player = Object.FindFirstObjectByType<PlayerMovement2D>();
                        if (player == null)
                        {
                            if (EasternFantasy.Quest.QuestManager.Instance == null)
                                Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/QuestManager.prefab"));
                            var definition = AssetDatabase.LoadAssetAtPath<CharacterClassDefinition>("Assets/_Project/Data/Characters/Dosa.asset");
                            Object.Instantiate(definition.PlayerPrefab); return;
                        }
                        QuietOtherGuides(); RestoreProgress(new SaveGameData());
                        SceneManager.LoadSceneAsync("PrologueVilage"); phase = 1; return;
                    case 1:
                        if (Loading || SceneManager.GetActiveScene().name != "PrologueVilage" || Visual == null) return;
                        QuietOtherGuides(); TeleportNear();
                        Check(Visual.IsDormant && Visual.PlayingClip == "RespornStone", "village starts dormant in the activation clip");
                        dormantSprite = Visual.GetComponent<SpriteRenderer>().sprite;
                        var clip = Resources.Load<SoulstoneCatalog>("SoulstoneCatalog").villageActivation;
                        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                        Check(bindings.Length > 0 && dormantSprite == AnimationUtility.GetObjectReferenceCurve(clip, bindings[0])[0].value,
                            "village dormant sprite is exactly activation frame zero");
                        sampleAt = EditorApplication.timeSinceStartup; phase = 2; return;
                    case 2:
                        if (EditorApplication.timeSinceStartup - sampleAt < 1.6) return;
                        Check(Visual.IsDormant && Visual.GetComponent<SpriteRenderer>().sprite == dormantSprite, "inactive village remains on frame zero over time");
                        Check(!SoulstoneWindowUI.Instance.TryOpen(), "village cannot travel before TempleGate activation");
                        SceneManager.LoadSceneAsync("TempleGate"); phase = 3; return;
                    case 3:
                        if (Loading || SceneManager.GetActiveScene().name != "TempleGate" || Visual == null) return;
                        QuietOtherGuides(); TeleportNear(); phase = 4; return;
                    case 4:
                        if (!SoulstoneTutorialUI.IsRunning) return;
                        Check(SoulstoneTutorialUI.Instance.CurrentStep == 0, "entering TempleGate stone range starts the introduction");
                        Guide.Advance();
                        Check(SoulstoneTutorialUI.Instance.CurrentStep == 1 && Visual.IsDormant, "activation step waits with TempleGate stone dormant");
                        Guide.Advance();
                        Check(SoulstoneTutorialUI.Instance.CurrentStep == 1, "next/Space cannot bypass the required G press");
                        var hint = Guide.transform.Find("Floating Tutorial Key Hint");
                        Check(hint != null && hint.gameObject.activeSelf && hint.GetComponentInChildren<UnityEngine.UI.Image>().sprite == null,
                            "floating G instruction stays visible before activation");
                        // The root hint image is its background; inspect the actual supplied key child too.
                        Check(hint.Find("Shortcut Key").GetComponent<UnityEngine.UI.Image>().sprite == Resources.Load<Sprite>("UI/InputPrompts/keyboard_g"),
                            "floating prompt uses the supplied keyboard_g image");
                        originalCameraSize = Camera.main.orthographicSize;
                        originalCameraPosition = Camera.main.transform.position;
                        ScreenCapture.CaptureScreenshot("Artifacts/Soulstone/Tutorial-G.png");
                        sampleAt = EditorApplication.timeSinceStartup; phase = 40; return;
                    case 40:
                        if (EditorApplication.timeSinceStartup - sampleAt < .5) return;
                        var worldPrompt = (GameObject)typeof(SoulstoneWindowUI).GetField("prompt", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(SoulstoneWindowUI.Instance);
                        Check(worldPrompt != null && worldPrompt.activeSelf
                            && worldPrompt.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace
                            && worldPrompt.GetComponentInChildren<TMPro.TMP_Text>(true).text == "G",
                            "G key is shown above the stone using a world-space prompt");
                        var npcPrompt = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/InteractionPrompt.prefab");
                        Check(worldPrompt.transform.localScale == npcPrompt.transform.localScale
                            && worldPrompt.GetComponent<Canvas>().sortingOrder == npcPrompt.GetComponent<Canvas>().sortingOrder,
                            "stone G prompt matches NPC T prompt scale and sorting");
                        keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G)); phase = 41; return;
                    case 41:
                        if (!SoulstoneTutorialUI.Instance.IsActivationCloseup) return;
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        SoulstoneTutorialUI.Instance.Cancel();
                        Check(!SoulstoneProgress.IsActivated && !SoulstoneTutorialUI.IsRunning,
                            "cancelling during zoom-in does not unlock the stone prematurely");
                        Check(Mathf.Abs(Camera.main.orthographicSize-originalCameraSize)<.01f
                            && Vector3.Distance(Camera.main.transform.position,originalCameraPosition)<.03f
                            && Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>().enabled,
                            "cancelling a close-up restores camera position, zoom, and Cinemachine");
                        phase = 42; return;
                    case 42:
                        if (!SoulstoneTutorialUI.IsRunning || SoulstoneTutorialUI.Instance.CurrentStep != 0) return;
                        Guide.Advance(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G)); phase = 5; return;
                    case 5:
                        if (!SoulstoneProgress.IsActivated) return;
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        Check(Visual.IsActivating && Visual.PlayingClip == "InActiveTempleRespornStone", "G plays the TempleGate activation once");
                        Check(SoulstoneTutorialUI.Instance.IsActivationCloseup && Camera.main.orthographicSize < originalCameraSize
                            && !Guide.gameObject.activeSelf, "activation is shown in a close-up with the tutorial card hidden");
                        Check(!Guide.transform.Find("Floating Tutorial Key Hint").gameObject.activeSelf, "floating G prompt disappears immediately after G");
                        Check(!SoulstoneWindowUI.IsOpen, "travel window waits for the activation animation");
                        sampleAt = EditorApplication.timeSinceStartup; phase = 6; return;
                    case 6:
                        if (SoulstoneTutorialUI.Instance.CurrentStep != 2)
                        {
                            if (!capturedCloseup && Visual.IsActivating && EditorApplication.timeSinceStartup-sampleAt>.4)
                            { ScreenCapture.CaptureScreenshot("Artifacts/Soulstone/Tutorial-ActivationCloseup.png"); capturedCloseup = true; }
                            return;
                        }
                        Check(!SoulstoneTutorialUI.Instance.IsActivationCloseup && Mathf.Abs(Camera.main.orthographicSize-originalCameraSize)<.01f
                            && Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>().enabled,
                            "camera and Cinemachine are restored before opening the map tutorial");
                        Check(!View.LayoutDrivenByTutorial && View.panel.localScale.x>.9f,
                            "map tutorial uses the full window rather than a reduced side preview");
                        Check(!Visual.IsActivating && Visual.PlayingClip == "TempleRespornStone", "TempleGate switches to its repeating active clip");
                        Check(SoulstoneWindowUI.IsOpen && Time.timeScale == 0 && player.GetComponent<PlayerInputReader>().IsInputBlocked,
                            "map selection opens paused with gameplay input owned by the tutorial");
                        Guide.Advance(); Check(SoulstoneTutorialUI.Instance.CurrentStep == 2, "map choice cannot be skipped with Next");
                        View.destinationRows[1].onClick.Invoke();
                        Check(SoulstoneTutorialUI.Instance.CurrentStep == 2 && !View.travel.interactable, "current-map selection stays on the selection step");
                        ScreenCapture.CaptureScreenshot("Artifacts/Soulstone/Tutorial-MapSelection.png");
                        sampleAt = EditorApplication.timeSinceStartup; phase = 60; return;
                    case 60:
                        if (EditorApplication.timeSinceStartup - sampleAt < .3) return;
                        View.destinationRows[0].onClick.Invoke();
                        Check(SoulstoneTutorialUI.Instance.CurrentStep == 3 && View.travel.interactable, "clicking village advances to the travel-button step");
                        ScreenCapture.CaptureScreenshot("Artifacts/Soulstone/Tutorial-Travel.png");
                        sampleAt = EditorApplication.timeSinceStartup; phase = 7; return;
                    case 7:
                        if (EditorApplication.timeSinceStartup - sampleAt < .3) return;
                        View.travel.onClick.Invoke();
                        Check(SoulstoneProgress.TutorialCompleted && !SoulstoneTutorialUI.IsRunning && PortalSceneTransition.IsTransitioning,
                            "travel-button click completes the guide and starts actual travel");
                        phase = 8; return;
                    case 8:
                        if (SceneManager.GetActiveScene().name != "PrologueVilage" || Visual == null) return;
                        if (Visual.IsActivating && Visual.PlayingClip == "RespornStone") sawVillageActivation = true;
                        if (Loading || Visual.IsActivating) return;
                        Check(sawVillageActivation && SoulstoneProgress.VillageAwakened && Visual.PlayingClip == "ActiveRespornStone",
                            "village plays RespornStone once on first return and then loops ActiveRespornStone");
                        Check(Time.timeScale > 0 && !player.GetComponent<PlayerInputReader>().IsInputBlocked, "tutorial and travel release pause/input owners");
                        Check(SaveGameService.SaveNow(out _), "activated state and completed guide can be saved");
                        var data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(SaveGameService.PathFor(CharacterSelectionState.SelectedClass)));
                        Check(data.soulstoneActivated && data.soulstoneTutorial && data.soulstoneVillageAwakened, "save JSON contains all three Soulstone progress flags");
                        RestoreProgress(new SaveGameData()); Check(!SoulstoneProgress.IsActivated && Visual.IsDormant, "restoring a fresh character resets activation");
                        RestoreProgress(data);
                        Check(SoulstoneProgress.TutorialCompleted && !Visual.IsActivating && Visual.PlayingClip == "ActiveRespornStone", "save restoration resumes the active loop without replaying the village intro");
                        Check(SoulstoneWindowUI.Instance.TryOpen(), "G window is usable normally after the tutorial " + OpenDiagnostic());
                        SoulstoneWindowUI.Instance.Select("TempleGate"); Check(SoulstoneWindowUI.Instance.Travel(), "normal return travel to TempleGate works"); phase = 9; return;
                    case 9:
                        if (Loading || SceneManager.GetActiveScene().name != "TempleGate" || Visual == null) return;
                        Check(!SoulstoneTutorialUI.IsRunning && !Visual.IsActivating && Visual.PlayingClip == "TempleRespornStone", "returning to TempleGate keeps it active and does not restart the tutorial");
                        File.AppendAllText(Report, "PASS: ALL SOULSTONE TUTORIAL CHECKS COMPLETED. User saves untouched.\n");
                        EditorApplication.update -= Tick; EditorApplication.isPlaying = false; return;
                }
            }
            catch (Exception exception)
            {
                File.AppendAllText(Report, "FAILED phase " + phase + ": " + exception + "\n");
                Debug.LogException(exception); EditorApplication.update -= Tick; EditorApplication.isPlaying = false;
            }
        }
    }
}
