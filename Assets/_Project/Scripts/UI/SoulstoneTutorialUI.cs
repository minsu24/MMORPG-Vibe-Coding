using System.Collections.Generic;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    public sealed partial class SoulstoneTutorialUI : MonoBehaviour
    {
        public static SoulstoneTutorialUI Instance { get; private set; }
        public static bool IsRunning => Instance != null && Instance.running;
        public int CurrentStep => running && guide != null ? guide.CurrentStep : -1;
        private SoulstoneWindowUI window;
        private PlayerInputReader input;
        private PetCompanionTutorialUI guide;
        private GameObject canvasObject;
        private RectTransform canvasRect;
        private RectTransform[] targets;
        private SoulstoneActivationVisual visual;
        private SoulstoneWindowView view;
        private bool running, waitingForAnimation;
        public bool IsPlayingActivationSequence => waitingForAnimation;
        private int startedFrame;

        private static readonly string[] Titles = {
            "1. 귀혼석으로 편리하게 이동하기", "2. G키로 귀혼석 활성화", "3. 이동하고 싶은 맵 선택", "4. 이동 버튼 클릭" };
        private static readonly string[] Messages = {
            "귀혼석은 지역 사이를 편리하게 이동할 수 있게 해 줍니다.\n절 문의 귀혼석을 활성화하면\n서막 마을의 귀혼석도 함께 깨어납니다.",
            "귀혼석 가까이에서 G키를 눌러 활성화하세요.\n활성화 연출이 끝나면 이동할 지역을 고를 수 있습니다.\n상단의 G키 안내를 따라 직접 눌러 보세요.",
            "오른쪽 목록에서 이동하고 싶은 맵을 선택하세요.\n선택한 맵의 사진과 이름, NPC를 확인할 수 있습니다.\n이번에는 ‘서막 마을’을 직접 선택해 보세요.",
            "선택한 맵을 확인한 뒤 ‘이동’ 버튼을 누르세요.\n해당 지역의 귀혼석 앞으로 이동합니다.\n앞으로도 귀혼석 근처에서 G키로 이 창을 열 수 있습니다." };

        private void Awake() { Instance = this; window = GetComponent<SoulstoneWindowUI>(); }
        private void OnEnable() => SceneManager.activeSceneChanged += SceneChanged;

        internal bool TryBegin(PlayerInputReader player, SoulstoneActivationVisual stoneVisual)
        {
            if (running || SoulstoneProgress.TutorialCompleted || SceneManager.GetActiveScene().name != "TempleGate"
                || player == null || !player.CanControlGameplay || SaveGameService.IsRestoring
                || MapTutorialUI.IsRunning || StatsSkillTutorialUI.IsRunning || InventoryEquipmentTutorialUI.IsRunning
                || DungeonWindowUI.IsTutorialRunning || ShopWindowUI.IsTutorialRunning || EquipmentUpgradeWindowUI.IsTutorialRunning)
                return false;
            input = player; visual = stoneVisual; running = true; startedFrame = Time.frameCount;
            input.SetInputBlocked(this, true); GameTimeController.SetPaused(this, true);
            canvasObject = new GameObject("Soulstone Tutorial Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 7200;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            canvasRect = (RectTransform)canvasObject.transform;
            var overlay = new GameObject("Soulstone Guide", typeof(RectTransform), typeof(PetCompanionTutorialUI));
            overlay.transform.SetParent(canvasObject.transform, false); guide = overlay.GetComponent<PetCompanionTutorialUI>();
            targets = new RectTransform[Titles.Length];
            var data = Resources.Load<SoulstoneCatalog>("SoulstoneCatalog");
            guide.ShowCustom(data.font, "귀혼석 튜토리얼", Titles, Messages, targets, PrepareStep, null, true, false);
            guide.GetComponent<Canvas>().sortingOrder = 7300;
            return true;
        }
        private void PrepareStep(int step)
        {
            guide.SetFocusInteraction(step >= 2);
            if (step == 0) return;
            if (step == 1)
            {
                guide.SetKeyHint("keyboard_g", SoulstoneProgress.IsActivated ? "키를 눌러 귀혼석 창을 여세요." : "키를 눌러 활성화 하세요.");
                guide.RequireAction("G키를 직접 눌러 진행하세요."); return;
            }
            guide.ClearKeyHint();
            targets[step] = step == 2 ? (RectTransform)view.destinationRows[0].transform : (RectTransform)view.travel.transform;
            guide.RequireAction(step == 2 ? "서막 마을을 직접 클릭해 선택하세요." : "강조된 이동 버튼을 직접 클릭하세요.");
        }
        private void Update()
        {
            if (!running) return;
            if (input == null || input.GetComponent<PlayerEntity>()?.IsDead == true || SaveGameService.IsRestoring
                || (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)) { Cancel(); return; }
            if (Time.frameCount == startedFrame) return;
            if (waitingForAnimation) return;
            if (CurrentStep == 1 && Keyboard.current?.gKey.wasPressedThisFrame == true)
            {
                if (!window.IsNearCurrentStone(input.GetComponent<PlayerMovement2D>())) return;
                guide.ClearKeyHint();
                activationSequence = StartCoroutine(ActivateAndContinue()); return;
            }
            if (CurrentStep == 0 && Keyboard.current?.spaceKey.wasPressedThisFrame == true) guide.Advance();
        }
        private void ContinueInWindow()
        {
            waitingForAnimation = false;
            if (!window.OpenForTutorial(input)) { Cancel(); return; }
            view = window.TutorialView;
            guide.gameObject.SetActive(true);
            guide.SetPlacement(false);
            guide.CompleteAction();
        }
        internal void DestinationSelected(string scene)
        {
            if (running && CurrentStep == 2 && scene != SceneManager.GetActiveScene().name
                && Application.CanStreamedLevelBeLoaded(scene)) guide.CompleteAction();
        }
        internal void CompleteForTravel()
        {
            if (!running || CurrentStep != 3) return;
            SoulstoneProgress.CompleteTutorial(); Cancel();
        }
        public void Cancel()
        {
            running = false; waitingForAnimation = false;
            if (guide != null) guide.Hide();
            StopActivationSequence();
            view = null;
            window?.Close();
            if (input != null) input.SetInputBlocked(this, false);
            input = null; GameTimeController.SetPaused(this, false);
            if (canvasObject != null) Destroy(canvasObject);
            canvasObject = null; guide = null;
        }
        private void SceneChanged(Scene before, Scene after) => Cancel();
        private void OnDisable() { SceneManager.activeSceneChanged -= SceneChanged; Cancel(); }
        private void OnDestroy() { Cancel(); if (Instance == this) Instance = null; }
    }
}
