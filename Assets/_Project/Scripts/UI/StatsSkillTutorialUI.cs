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
    public sealed partial class StatsSkillTutorialUI : MonoBehaviour
    {
        [SerializeField] private MON_Goblin tutorialGoblin;
        [SerializeField] private DialogueSequence defeatedDialogue;
        private static readonly HashSet<CharacterClassId> shownClasses = new HashSet<CharacterClassId>();
        private static StatsSkillTutorialUI active;
        private static readonly HashSet<CharacterClassId> finishedClasses = new HashSet<CharacterClassId>();
        private static readonly HashSet<CharacterClassId> practicedClasses = new HashSet<CharacterClassId>();
        public static bool HasFinishedForCurrentClass => finishedClasses.Contains(CharacterSelectionState.SelectedClass);
        public static bool IsRunning => active != null && active.running;
        public bool IsShowing => running;
        public int CurrentStep => running && guide != null ? guide.CurrentStep : -1;
        public int StepCount => Titles.Length;
        private bool defeated, running;
        private int openedFrame;
        private DialogueManager boundDialogue;
        private PlayerStatsWindowUI stats;
        private SkillWindowUI skills;
        private PlayerInputReader playerInput;
        private PlayerSkillSystem skillSystem;
        private CharacterClassId tutorialClass;
        private int initialSkillPoints;
        private readonly Dictionary<SkillDefinition, int> initialSkillLevels = new Dictionary<SkillDefinition, int>();
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
            "1. 능력치 창 직접 열기", "2. 스킬창 직접 열기", "3. 패시브 스킬",
            "4. 액티브 스킬", "5. 스킬 직접 배우기", "6. 액티브 단축키 등록" };
        private static readonly string[] Messages = {
            "C키로 능력치 창을 직접 열어 보세요.",
            "현재 능력치를 확인했나요?\nK키로 스킬창을 직접 열어 보세요.",
            "배우면 능력이 자동으로 적용되는 스킬이에요.\n직접 사용하거나 단축키에 등록할 필요가 없어요.",
            "직접 사용해 공격하거나 특별한 효과를 내는 스킬이에요.\n자원 소모와 재사용 대기시간을 확인하세요.",
            "레벨업마다 스킬 포인트를 1 얻어요.\n원하는 스킬의 + 버튼을 눌러 직접 배워 보세요.",
            "배운 액티브는 드래그 앤 드롭으로 단축키에 등록할 수 있어요.\nQ·W·E·R·A·S·D·F로 사용해요." };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { shownClasses.Clear(); finishedClasses.Clear(); practicedClasses.Clear(); active = null; }
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
            if (running || HasFinishedForCurrentClass) return;
            stats = PlayerStatsWindowUI.Instance;
            skills = SkillWindowUI.Instance;
            if (stats == null || skills == null) return;
            playerInput = FindFirstObjectByType<PlayerInputReader>();
            skillSystem = playerInput != null ? playerInput.GetComponent<PlayerSkillSystem>() : null;
            if (skillSystem == null || (skillSystem.SkillPoints <= 0 && !HasLearnedSkill())) return;
            tutorialClass = CharacterSelectionState.SelectedClass;
            initialSkillPoints = skillSystem.SkillPoints;
            initialSkillLevels.Clear();
            bool canLearn = false;
            foreach (var skill in skillSystem.AvailableSkills)
            {
                if (skill == null) continue;
                initialSkillLevels[skill] = skillSystem.GetSkillLevel(skill);
                canLearn |= skillSystem.CanLearn(skill);
            }
            // Legacy/incomplete saves can already contain spent points. Never force a refund or grant extra points.
            if (!canLearn && HasLearnedSkill()) practicedClasses.Add(tutorialClass);
            skillSystem.SkillsChanged += ObserveLearning;
            shownClasses.Add(CharacterSelectionState.SelectedClass);
            active = this; running = true; openedFrame = Time.frameCount;
            InventoryWindowUI.Instance?.SetOpen(false);
            EquipmentWindowUI.Instance?.CloseFromInventory();
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
            guide.SetCompactBottomLayout();
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
            float reserved = Mathf.Min(280f, canvasRect.rect.height * .3f);
            float fit = Mathf.Min(1f, (canvasRect.rect.width - 48f) / Mathf.Max(1, max.x-min.x),
                (canvasRect.rect.height - reserved - 24f) / Mathf.Max(1, max.y-min.y));
            panel.localScale *= fit;
            Vector3 center = canvasRect.TransformPoint(canvasRect.rect.min + new Vector2(canvasRect.rect.width * .5f,
                reserved + (canvasRect.rect.height - reserved) * .5f));
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
            skills.HideTutorialTooltip();
            guide.ClearKeyHint();
            guide.SetFocusInteraction(step == 4);
            if (step == 0)
            {
                skills.SetOpenForTutorial(false); stats.SetOpen(false);
                guide.SetKeyHint("keyboard_c", "키로 능력치 창을 여세요.");
                guide.RequireAction("C키를 직접 눌러 주세요.");
                targets[step] = null;
            }
            else if (step == 1)
            {
                Capture(stats.TutorialPortrait); Capture(stats.TutorialPortraitBackground);
                PlacePanel(stats.TutorialTarget);
                PlaceWithPanel(stats.TutorialPortrait, stats.TutorialTarget);
                PlaceWithPanel(stats.TutorialPortraitBackground, stats.TutorialTarget);
                targets[step] = stats.TutorialTarget;
                guide.RequireAction("K키를 직접 눌러 주세요.");
            }
            else {
                stats.SetOpen(false); skills.SetOpenForTutorial(true); PlacePanel(skills.TutorialTarget);
                targets[step] = step == 4 ? skills.TutorialTarget : step == 5 && QuickSlotBarUI.Instance != null ? QuickSlotBarUI.Instance.TutorialTarget : SkillTarget(step);
                if (step == 4) guide.RequireAction("선행 스킬·전직 조건을 확인하고 + 버튼을 눌러 주세요.");
                else guide.SetControlsHint(step == 5 ? "Space 또는 안내 완료" : "Space 또는 다음");
            }
            if (quickCanvas != null) quickCanvas.enabled = step == 5 || quickCanvasWasEnabled;
            Canvas.ForceUpdateCanvases();
        }
        private RectTransform SkillTarget(int step)
        {
            foreach (var slot in skills.TutorialSlots) {
                if (slot == null || slot.Definition == null) continue;
                var definition = slot.Definition;
                if ((step == 2 && definition.ActivationType == SkillActivationType.Passive)
                    || (step == 3 && definition.ActivationType == SkillActivationType.Active)) return slot.transform as RectTransform;
            }
            return skills.TutorialTarget;
        }
        private void Update()
        {
            if (SoulstoneTutorialUI.IsRunning || EasternFantasy.Saving.SaveGameService.IsRestoring) return;
            BindDialogue();
            if (!running)
            {
                if (shownClasses.Contains(CharacterSelectionState.SelectedClass)) return;
                // Resume an interrupted lesson after load, including one whose point was already spent.
                var progress = FindFirstObjectByType<PlayerProgression>();
                var system = progress != null ? progress.GetComponent<PlayerSkillSystem>() : null;
                if (!shownClasses.Contains(CharacterSelectionState.SelectedClass) && progress != null && progress.CurrentLevel >= 2
                    && system != null && !MapTutorialUI.IsRunning && !InventoryEquipmentTutorialUI.IsRunning
                    && !DungeonWindowUI.IsTutorialRunning && !ShopWindowUI.IsTutorialRunning && !EquipmentUpgradeWindowUI.IsTutorialRunning
                    && (DungeonWindowUI.Instance == null || !DungeonWindowUI.Instance.IsOpen)
                    && (ShopWindowUI.Instance == null || !ShopWindowUI.Instance.IsOpen)
                    && (EquipmentUpgradeWindowUI.Instance == null || !EquipmentUpgradeWindowUI.Instance.IsOpen)
                    && (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive)) Begin();
                return;
            }
            if (Time.frameCount == openedFrame) return;
            if (skillSystem == null || CharacterSelectionState.SelectedClass != tutorialClass) { Finish(); return; }
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.cKey.wasPressedThisFrame) HandleShortcut(false);
                else if (keyboard.kKey.wasPressedThisFrame) HandleShortcut(true);
                if (keyboard.spaceKey.wasPressedThisFrame) guide.Advance();
            }
            EvaluateProgress();
        }
        private void HandleShortcut(bool skillKey)
        {
            if (!running) return;
            if (!skillKey && CurrentStep == 0) stats.SetOpenForTutorial(true);
            else if (skillKey && CurrentStep == 1) skills.SetOpenForTutorial(true);
            EvaluateProgress();
        }
        private bool HasLearnedSkill()
        {
            if (skillSystem == null) return false;
            foreach (var skill in skillSystem.AvailableSkills) if (skill != null && skillSystem.GetSkillLevel(skill) > 0) return true;
            return false;
        }
        private void ObserveLearning()
        {
            if (!running || skillSystem == null || skillSystem.SkillPoints >= initialSkillPoints) return;
            foreach (var pair in initialSkillLevels)
                if (skillSystem.GetSkillLevel(pair.Key) > pair.Value) { practicedClasses.Add(tutorialClass); break; }
        }
        private void EvaluateProgress()
        {
            if (CurrentStep == 0 && stats.IsOpen || CurrentStep == 1 && skills.IsOpen
                || CurrentStep == 4 && practicedClasses.Contains(tutorialClass)) guide.CompleteAction();
        }
        public void Advance() { if (running) guide.Advance(); }
        private void Complete()
        {
            finishedClasses.Add(tutorialClass);
            Finish();
        }
        public void Finish()
        {
            if (!running) return;
            running = false;
            if (!finishedClasses.Contains(tutorialClass)) shownClasses.Remove(tutorialClass);
            if (skillSystem != null) skillSystem.SkillsChanged -= ObserveLearning;
            skillSystem = null;
            initialSkillLevels.Clear();
            if (guide != null) guide.Hide();
            if (stats != null) stats.SetOpen(false);
            if (skills != null) skills.SetOpenForTutorial(false);
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
