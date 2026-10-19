using System.Collections.Generic;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using EasternFantasy.Skill;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class StatsSkillTutorialUI : MonoBehaviour
    {
        [SerializeField] private MON_Goblin tutorialGoblin;
        [SerializeField] private DialogueSequence defeatedDialogue;
        private static readonly HashSet<CharacterClassId> shownClasses = new HashSet<CharacterClassId>();
        private static StatsSkillTutorialUI active;
        private static readonly HashSet<CharacterClassId> finishedClasses = new HashSet<CharacterClassId>();
        public static bool HasFinishedForCurrentClass => finishedClasses.Contains(CharacterSelectionState.SelectedClass);
        public static bool IsRunning => active != null && active.running;
        public bool IsShowing => running;
        public int CurrentStep => guide != null ? guide.CurrentStep : -1;
        public int StepCount => Titles.Length;
        private bool defeated, running;
        private int openedFrame;
        private DialogueManager boundDialogue;
        private PlayerStatsWindowUI stats;
        private SkillWindowUI skills;
        private PlayerInputReader playerInput;
        private bool inputWasEnabled;
        private GameObject canvasObject;
        private RectTransform canvasRect;
        private PetCompanionTutorialUI guide;
        private RectTransform[] targets;
        private Canvas quickCanvas;
        private bool quickCanvasWasEnabled;
        private readonly Dictionary<RectTransform, LayoutState> layouts = new Dictionary<RectTransform, LayoutState>();
        private struct LayoutState { public Vector3 Position, Scale; }
        private static readonly string[] Titles = {
            "1. 내 능력치 확인하기", "2. 스킬창 열고 닫기", "3. 레벨업과 스킬 포인트", "4. 마우스로 스킬 설명 보기",
            "5. 선행 스킬 확인하기", "6. 패시브 스킬", "7. 액티브 스킬", "8. 퀵슬롯에 액티브 스킬 등록" };
        private static readonly string[] Messages = {
            "C키로 스탯창을 열고 닫을 수 있습니다.\nESC키로도 닫을 수 있어요.\n체력·자원·공격력·방어력·치명타 등 현재 능력치를 확인하세요.\n다음으로 넘어가면 스탯창을 닫고 스킬창을 살펴봅니다.",
            "K키로 스킬창을 열고 닫을 수 있습니다.\nESC키로도 닫을 수 있어요.\n직업별 스킬과 습득 상태를 이곳에서 확인할 수 있습니다.",
            "레벨이 1 오를 때마다 스킬 포인트를 1 획득합니다.\n지금 2레벨이 되어 첫 스킬 포인트를 얻었어요.\n스킬의 + 버튼으로 습득하거나 강화할 수 있습니다.\n이 안내 중에는 포인트를 사용하지 않습니다.",
            "스킬 아이콘에 마우스를 올리면 설명이 나타납니다.\n스킬 효과와 현재 레벨, 소모 자원, 재사용 대기시간을 살펴보세요.\n습득 조건과 필요한 선행 스킬도 설명에서 확인할 수 있어요.",
            "0차 스킬을 제외한 스킬에는 선행 스킬이 있습니다.\n설명에 적힌 선행 스킬을 요구 레벨까지 먼저 배워야 해요.\n전직 등 다른 습득 조건도 함께 확인하세요.",
            "패시브 스킬은 배우면 효과가 자동으로 적용됩니다.\n체력이나 자원, 이동속도 같은 능력을 높여 줍니다.\n직접 사용하거나 퀵슬롯에 등록할 필요가 없어요.",
            "액티브 스킬은 직접 사용해 공격하거나 특별한 행동을 합니다.\n먼저 스킬을 배운 뒤 퀵슬롯에 등록해 사용하세요.\n스킬마다 소모 자원과 재사용 대기시간이 다르니 설명을 확인하세요.",
            "배운 액티브 스킬 아이콘을 드래그해서 아래 퀵슬롯에 놓으세요.\n등록한 슬롯에 표시된 Q·W·E·R·A·S·D·F 키로 사용할 수 있습니다.\n패시브 스킬은 등록할 수 없어요.\n안내를 마친 뒤 포인트를 사용해 원하는 스킬을 배워 보세요." };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { shownClasses.Clear(); finishedClasses.Clear(); active = null; }
        private void OnEnable()
        {
            if (tutorialGoblin != null) tutorialGoblin.Defeated += OnGoblinDefeated;
            BindDialogue();
        }
        private void OnGoblinDefeated(EnemyController enemy) { if (enemy == tutorialGoblin) defeated = true; }
        private void BindDialogue()
        {
            if (boundDialogue == DialogueManager.Instance) return;
            if (boundDialogue != null) boundDialogue.DialogueCompleted -= OnDialogueCompleted;
            boundDialogue = DialogueManager.Instance;
            if (boundDialogue != null) boundDialogue.DialogueCompleted += OnDialogueCompleted;
        }
        private void OnDialogueCompleted(DialogueSequence completed)
        {
            if (completed != defeatedDialogue || !defeated || running || shownClasses.Contains(CharacterSelectionState.SelectedClass)) return;
            var progression = FindFirstObjectByType<PlayerProgression>();
            if (progression == null || progression.CurrentLevel < 2) return;
            Begin();
        }
        private void Begin()
        {
            stats = PlayerStatsWindowUI.Instance;
            skills = SkillWindowUI.Instance;
            if (stats == null || skills == null) return;
            shownClasses.Add(CharacterSelectionState.SelectedClass);
            active = this; running = true; openedFrame = Time.frameCount;
            InventoryWindowUI.Instance?.SetOpen(false);
            EquipmentWindowUI.Instance?.CloseFromInventory();
            playerInput = FindFirstObjectByType<PlayerInputReader>();
            if (playerInput != null) { inputWasEnabled = playerInput.enabled; playerInput.enabled = false; }
            FindFirstObjectByType<PlayerMovement2D>()?.ClearInput();
            GameTimeController.SetPaused(this, true);
            canvasObject = new GameObject("StatsSkillTutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5000;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            canvasRect = (RectTransform)canvasObject.transform;
            var overlay = new GameObject("StatsSkillGuide", typeof(RectTransform), typeof(PetCompanionTutorialUI));
            overlay.transform.SetParent(canvasObject.transform, false); guide = overlay.GetComponent<PetCompanionTutorialUI>();
            targets = new RectTransform[Titles.Length];
            Capture(stats.TutorialTarget); Capture(skills.TutorialTarget);
            if (QuickSlotBarUI.Instance != null) {
                quickCanvas = QuickSlotBarUI.Instance.GetComponentInParent<Canvas>().rootCanvas;
                quickCanvasWasEnabled = quickCanvas.enabled;
            }
            Canvas.ForceUpdateCanvases();
            guide.ShowCustom(stats.TutorialFont, "스탯·스킬 튜토리얼", Titles, Messages, targets, PrepareStep, Complete, true, false);
        }
        private void Capture(RectTransform panel)
        {
            if (panel != null && !layouts.ContainsKey(panel)) layouts.Add(panel, new LayoutState { Position = panel.position, Scale = panel.localScale });
        }
        private void PlacePanel(RectTransform panel)
        {
            if (panel == null || !layouts.TryGetValue(panel, out var saved)) return;
            panel.position = saved.Position; panel.localScale = saved.Scale;
            var corners = new Vector3[4]; panel.GetWorldCorners(corners);
            Vector2 min = canvasRect.InverseTransformPoint(corners[0]), max = canvasRect.InverseTransformPoint(corners[2]);
            float fit = Mathf.Min(1f, canvasRect.rect.width * .46f / Mathf.Max(1, max.x-min.x), canvasRect.rect.height * .82f / Mathf.Max(1, max.y-min.y));
            panel.localScale *= fit;
            Vector3 center = canvasRect.TransformPoint(canvasRect.rect.min + new Vector2(canvasRect.rect.width * .25f, canvasRect.rect.height * .53f));
            panel.position += center - panel.TransformPoint(panel.rect.center);
        }
        private void PlaceWithPanel(RectTransform decoration, RectTransform panel)
        {
            if (decoration == null || panel == null || decoration.IsChildOf(panel)
                || !layouts.TryGetValue(decoration, out var saved) || !layouts.TryGetValue(panel, out var originalPanel)) return;
            float ratio = panel.localScale.x / Mathf.Max(.0001f, originalPanel.Scale.x);
            decoration.position = panel.position + (saved.Position - originalPanel.Position) * ratio;
            decoration.localScale = saved.Scale * ratio;
        }
        private void PrepareStep(int step)
        {
            if (step == 0) {
                skills.SetOpen(false); stats.SetOpenForTutorial(true);
                Capture(stats.TutorialPortrait); Capture(stats.TutorialPortraitBackground);
                PlacePanel(stats.TutorialTarget);
                PlaceWithPanel(stats.TutorialPortrait, stats.TutorialTarget);
                PlaceWithPanel(stats.TutorialPortraitBackground, stats.TutorialTarget);
                targets[step] = stats.TutorialTarget;
            }
            else {
                stats.SetOpen(false); skills.SetOpenForTutorial(true); PlacePanel(skills.TutorialTarget);
                targets[step] = step == 2 ? skills.TutorialPointsTarget : step == 7 && QuickSlotBarUI.Instance != null ? QuickSlotBarUI.Instance.TutorialTarget : SkillTarget(step);
            }
            if (quickCanvas != null) quickCanvas.enabled = step == 7 || quickCanvasWasEnabled;
            Canvas.ForceUpdateCanvases();
        }
        private RectTransform SkillTarget(int step)
        {
            foreach (var slot in skills.TutorialSlots) {
                if (slot == null || slot.Definition == null) continue;
                var definition = slot.Definition;
                if ((step == 3 && definition.Tier == SkillTier.Base)
                    || (step == 4 && definition.Tier != SkillTier.Base)
                    || (step == 5 && definition.ActivationType == SkillActivationType.Passive)
                    || (step == 6 && definition.ActivationType == SkillActivationType.Active)) return slot.transform as RectTransform;
            }
            return skills.TutorialTarget;
        }
        private void Update()
        {
            BindDialogue();
            if (!running || Time.frameCount == openedFrame || Keyboard.current == null) return;
            if (Keyboard.current.spaceKey.wasPressedThisFrame) guide.Advance();
        }
        public void Advance() { if (running) guide.Advance(); }
        private void Complete()
        {
            finishedClasses.Add(CharacterSelectionState.SelectedClass);
            Finish();
        }
        public void Finish()
        {
            if (!running) return;
            running = false;
            if (guide != null) guide.Hide();
            if (stats != null) stats.SetOpen(false);
            if (skills != null) skills.SetOpen(false);
            foreach (var pair in layouts) if (pair.Key != null) { pair.Key.position = pair.Value.Position; pair.Key.localScale = pair.Value.Scale; }
            layouts.Clear();
            if (quickCanvas != null) quickCanvas.enabled = quickCanvasWasEnabled;
            if (playerInput != null) playerInput.enabled = inputWasEnabled;
            FindFirstObjectByType<PlayerMovement2D>()?.ClearInput();
            GameTimeController.SetPaused(this, false);
            if (canvasObject != null) Destroy(canvasObject);
            if (active == this) active = null;
        }
        private void OnDisable()
        {
            if (tutorialGoblin != null) tutorialGoblin.Defeated -= OnGoblinDefeated;
            if (boundDialogue != null) boundDialogue.DialogueCompleted -= OnDialogueCompleted;
            boundDialogue = null;
            Finish();
        }
    }
}
