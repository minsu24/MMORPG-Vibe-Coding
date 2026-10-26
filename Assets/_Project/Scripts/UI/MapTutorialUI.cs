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
    public sealed class MapTutorialUI : MonoBehaviour
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
            "1. M키로 맵 열기", "2. 게임 전체 맵 살펴보기", "3. 아직 도달하지 못한 지역",
            "4. 아이콘으로 현재 위치 확인", "5. 섬을 눌러 상세 맵 보기", "6. 상세 맵의 현재 위치" };
        private static readonly string[] Messages = {
            "M키를 누르면 맵을 열 수 있습니다.\n여행을 시작하기 전에 맵을 함께 살펴볼까요?\nM키를 누르거나 다음 버튼을 눌러 맵을 펼쳐 보세요.",
            "이 화면에서 게임 전체 맵을 볼 수 있습니다.\n여러 섬과 지역의 위치를 살펴보고\n앞으로 여행할 곳을 확인해 보세요.",
            "아직 도달하지 못한 지역은 이름이 ???로 표시되고\n흐릿하게 보입니다.\n직접 그 지역에 도달하면 이름과 모습이 밝혀집니다.",
            "맵의 캐릭터 아이콘이 현재 위치를 표시합니다.\n어느 섬에 있는지 궁금할 때는 이 아이콘을 찾아보세요.",
            "방문한 섬을 누르면 그 섬에 속한 각 맵의\n상세 맵을 볼 수 있습니다.\n강조된 섬을 직접 누르거나 다음 버튼을 눌러 보세요.",
            "상세 맵에서도 미방문 지역은 먹안개와 ???로 표시됩니다.\n현재 지역은 금색 띠와 캐릭터 아이콘으로 강조합니다.\n퀘스트로 알게 된 목적지는 방문 전에 이름만 먼저 공개됩니다.\n안내를 마친 뒤에는 M키로 언제든 맵을 확인하세요!" };

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
            if (guide.CurrentStep == 4 && map.IsShowingDetail) { guide.Advance(); return; }
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.spaceKey.wasPressedThisFrame
                || (guide.CurrentStep == 0 && keyboard.mKey.wasPressedThisFrame)) guide.Advance();
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
        }

        private void PrepareStep(int step)
        {
            guide.SetFocusInteraction(step == 4);
            if (step == 0)
            {
                map.CloseInstant();
                targets[step] = null;
                return;
            }
            if (!map.IsOpen) map.Open();
            if (step == 5) map.ShowCurrentRegionForTutorial(); else map.ShowOverview();
            PlaceMap();
            switch (step)
            {
                case 1: targets[step] = map.TutorialOverview; break;
                case 2:
                    targets[step] = map.TutorialOverview;
                    foreach (var island in map.GetComponentsInChildren<MapIslandDiscoveryUI>(true))
                        if (!island.IsDiscovered) { targets[step] = island.transform as RectTransform; break; }
                    break;
                case 3: targets[step] = location.OverviewTutorialTarget; break;
                case 4: targets[step] = map.TutorialIsland; break;
                case 5: targets[step] = map.TutorialDetail; break;
            }
        }

        private void LateUpdate()
        {
            if (running && map.IsOpen) PlaceMap();
        }
        private void PlaceMap()
        {
            if (canvasRect == null || panel == null) return;
            // The overview artwork is enlarged inside the panel; fit that artwork too.
            float artworkScale = !map.IsShowingDetail && map.TutorialOverview != null
                ? Mathf.Max(1f, map.TutorialOverview.localScale.x) : 1f;
            panel.localScale = originalScale * Mathf.Min(.47f, .46f / artworkScale);
            Vector2 center = canvasRect.rect.center + Vector2.left * canvasRect.rect.width * .25f;
            panel.position = canvasRect.TransformPoint(center);
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
