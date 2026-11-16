using System.Collections.Generic;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed partial class MapTutorialUI : MonoBehaviour
    {
        private static readonly HashSet<CharacterClassId> completedClasses = new HashSet<CharacterClassId>();
        private static MapTutorialUI active;
        public static bool IsRunning => active != null && active.running;
        public int CurrentStep => guide != null && running ? guide.CurrentStep : -1;
        private MapWindowUI map;
        private MapPlayerLocationUI location;
        private PlayerInputReader input;
        private PetCompanionTutorialUI guide;
        private GameObject canvasObject;
        private RectTransform canvasRect, panel;
        private RectTransform[] targets;
        private Vector3 originalPosition, originalScale;
        private CharacterClassId tutorialClass;
        private bool running;
        private int openedFrame;

        private static readonly string[] Titles = {
            "1. 맵 직접 열기", "2. 현재 지역 클릭", "3. 상세 맵 확인", "4. 맵 안내 완료" };
        private static readonly string[] Messages = {
            "M키를 직접 눌러 맵을 열어 보세요.",
            "캐릭터 아이콘이 있는 섬이 현재 지역이에요.\n강조된 섬을 직접 클릭하세요.",
            "금색 띠와 캐릭터 아이콘이 현재 위치예요.\n전체 맵으로 돌아가는 버튼을 클릭하세요.",
            "???는 아직 방문하지 않은 지역이에요.\n안내 후 M키로 언제든 맵을 열 수 있어요." };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { completedClasses.Clear(); active = null; }

        private void OnEnable()
        {
            map = GetComponent<MapWindowUI>();
            location = GetComponent<MapPlayerLocationUI>();
            SceneManager.activeSceneChanged += SceneChanged;
        }

        private void Update()
        {
            if (SoulstoneTutorialUI.IsRunning) return;
            if (EasternFantasy.Saving.SaveGameService.IsRestoring) return;
            if (!running)
            {
                if (completedClasses.Contains(CharacterSelectionState.SelectedClass)
                    || map == null || location == null || !location.HasCurrentLocation
                    || PortalSceneTransition.IsTransitioning || StatsSkillTutorialUI.IsRunning
                    || InventoryEquipmentTutorialUI.IsRunning || DungeonWindowUI.IsTutorialRunning
                    || ShopWindowUI.IsTutorialRunning || EquipmentUpgradeWindowUI.IsTutorialRunning
                    || (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)) return;
                var player = FindFirstObjectByType<PlayerInputReader>();
                if (player != null) Begin(player);
                return;
            }
            if (Time.frameCount == openedFrame) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (guide.CurrentStep == 0 && keyboard.mKey.wasPressedThisFrame) HandleOpenMap();
                if (keyboard.spaceKey.wasPressedThisFrame) guide.Advance();
            }
            EvaluateProgress();
        }

        private void HandleOpenMap()
        {
            if (!running || CurrentStep != 0) return;
            map.Open();
            EvaluateProgress();
        }

        private void EvaluateProgress()
        {
            if (CurrentStep == 0 && map.IsOpen
                || CurrentStep == 1 && map.IsShowingCurrentRegionForTutorial
                || CurrentStep == 2 && map.IsOpen && !map.IsShowingDetail)
                guide.CompleteAction();
        }

        private void Begin(PlayerInputReader player)
        {
            panel = map.TutorialPanel;
            if (panel == null) return;
            input = player;
            originalPosition = panel.localPosition; originalScale = panel.localScale;
            tutorialClass = CharacterSelectionState.SelectedClass;
            running = true; active = this; openedFrame = Time.frameCount;
            input.SetInputBlocked(this, true);
            GameTimeController.SetPaused(this, true);
            canvasObject = new GameObject("MapTutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.overrideSorting = true; canvas.sortingOrder = 7000;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.anchorMin = Vector2.zero; canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = canvasRect.offsetMax = Vector2.zero;
            var overlay = new GameObject("MapGuide", typeof(RectTransform), typeof(PetCompanionTutorialUI));
            overlay.transform.SetParent(canvasObject.transform, false); guide = overlay.GetComponent<PetCompanionTutorialUI>();
            targets = new RectTransform[Titles.Length];
            var label = map.GetComponentInChildren<TMP_Text>(true);
            Canvas.ForceUpdateCanvases();
            guide.ShowCustom(label != null ? label.font : TMP_Settings.defaultFontAsset, "\uB9F5 \uD29C\uD1A0\uB9AC\uC5BC", Titles, Messages,
                targets, PrepareStep, Complete, true, false);
            // MapWindow renders at 6000; the guide and its pointer mask must be above it.
            guide.GetComponent<Canvas>().sortingOrder = 7100;
            guide.SetCompactBottomLayout();
        }

        private void PrepareStep(int step)
        {
            guide.SetFocusInteraction(step == 1 || step == 2);
            if (step == 0)
            {
                map.CloseInstant();
                targets[step] = null;
                guide.SetKeyHint("keyboard_m", "키를 눌러 맵을 여세요.");
                guide.RequireAction("M키를 직접 눌러 주세요.");
                return;
            }
            guide.ClearKeyHint();
            PlaceMap();
            switch (step)
            {
                case 1:
                    targets[step] = map.TutorialIsland;
                    guide.RequireAction("강조된 섬을 클릭하세요.");
                    break;
                case 2:
                    targets[step] = map.TutorialBackButton;
                    guide.RequireAction("전체 맵으로 돌아가는 버튼을 클릭하세요.");
                    break;
                case 3:
                    targets[step] = map.TutorialOverview;
                    guide.SetControlsHint("Space 또는 안내 완료");
                    break;
            }
        }

        private void LateUpdate()
        {
            if (running && map.IsOpen) PlaceMap();
        }
        private void PlaceMap()
        {
            if (canvasRect == null || panel == null) return;
            // Reserve a short bottom strip for instructions and fit the actual map artwork above it.
            panel.localPosition = originalPosition;
            panel.localScale = originalScale;
            var artwork = map.IsShowingDetail ? map.TutorialDetail : map.TutorialOverview;
            if (artwork == null) return;
            var corners = new Vector3[4]; artwork.GetWorldCorners(corners);
            Vector2 min = canvasRect.InverseTransformPoint(corners[0]);
            Vector2 max = canvasRect.InverseTransformPoint(corners[2]);
            float reserved = Mathf.Min(280f, canvasRect.rect.height * .3f);
            float fit = Mathf.Min(1f, (canvasRect.rect.width - 48f) / Mathf.Max(1f, max.x - min.x),
                (canvasRect.rect.height - reserved - 24f) / Mathf.Max(1f, max.y - min.y));
            panel.localScale *= fit;
            Vector2 center = canvasRect.rect.min + new Vector2(canvasRect.rect.width * .5f,
                reserved + (canvasRect.rect.height - reserved) * .5f);
            panel.position += canvasRect.TransformPoint(center) - artwork.TransformPoint(artwork.rect.center);
        }
        private void Complete()
        {
            completedClasses.Add(tutorialClass);
            End();
        }
        private void End()
        {
            if (!running) return;
            running = false;
            if (active == this) active = null;
            if (guide != null) guide.Hide();
            if (map != null) map.CloseInstant();
            if (panel != null) { panel.localPosition = originalPosition; panel.localScale = originalScale; }
            if (input != null) input.SetInputBlocked(this, false);
            GameTimeController.SetPaused(this, false);
            if (canvasObject != null) Destroy(canvasObject);
            canvasObject = null; guide = null;
        }
        private void SceneChanged(Scene previous, Scene next) => End();
        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneChanged;
            End();
        }
    }
}
