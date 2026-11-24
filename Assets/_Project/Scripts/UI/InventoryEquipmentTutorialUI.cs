using System.Collections.Generic;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed partial class InventoryEquipmentTutorialUI : MonoBehaviour
    {
        private static readonly HashSet<CharacterClassId> shownClasses = new HashSet<CharacterClassId>();
        private static readonly HashSet<CharacterClassId> completedClasses = new HashSet<CharacterClassId>();
        private static InventoryEquipmentTutorialUI active;
        public static bool HasFinishedForCurrentClass => completedClasses.Contains(CharacterSelectionState.SelectedClass);
        public static bool IsRunning => active != null && active.running;
        public bool IsShowing => running;
        public int CurrentStep => running && guide != null ? guide.CurrentStep : -1;
        public int StepCount => Titles.Length;
        private bool pending, running, inputWasEnabled;
        private bool titleTutorial;
        private int openedFrame;
        private float nextRecoveryCheck;
        private CharacterClassId tutorialClass;
        private ItemCategory originalCategory;
        private ItemDefinition practiceItem;
        private PlayerInventory pendingInventory;
        private InventoryWindowUI inventory;
        private EquipmentWindowUI equipment;
        private PlayerInputReader input;
        private GameObject canvasObject;
        private RectTransform canvasRect;
        private PetCompanionTutorialUI guide;
        private RectTransform[] targets;
        private readonly Dictionary<RectTransform, LayoutState> layouts = new Dictionary<RectTransform, LayoutState>();
        private struct LayoutState { public Vector3 Position, Scale; }
        private static readonly string[] Titles = {
            "1. 인벤토리 직접 열기", "2. 직접 장착하기", "3. 장비창 직접 열기", "4. 장비창 확인" };
        private static readonly string[] TitleTutorialTitles = {
            "1. 새 칭호 획득", "2. 칭호 직접 장착", "3. 장비창 직접 열기", "4. 장착한 칭호 확인" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { shownClasses.Clear(); completedClasses.Clear(); active = null; }
        private void OnEnable() => PlayerInventory.ItemAcquired += OnItemCollected;

        private bool CanPractice(PlayerInventory owner, ItemDefinition item) =>
            owner != null && owner.CurrentClass == CharacterSelectionState.SelectedClass
            && item != null && owner.CanEquip(item) && owner.GetQuantity(item) > 0;

        private void OnItemCollected(PlayerInventory owner, ItemDefinition item)
        {
            if (EasternFantasy.Saving.SaveGameService.IsRestoring || running || pending
                || !CanPractice(owner, item)) return;
            if (item.EquipmentSlot == EquipmentSlot.Title)
            {
                if (owner.CaveBellTitleTutorialItem == item) Queue(owner, item);
                return;
            }
            if (shownClasses.Contains(CharacterSelectionState.SelectedClass)) return;
            Queue(owner, item);
        }

        private void Queue(PlayerInventory owner, ItemDefinition item)
        {
            pendingInventory = owner;
            practiceItem = item;
            pending = true;
        }

        private void RecoverUnfinishedTutorial()
        {
            // Save restoration does not raise acquisition events. Resume with real owned equipment.
            if (Time.unscaledTime < nextRecoveryCheck) return;
            nextRecoveryCheck = Time.unscaledTime + .5f;
            var owner = FindFirstObjectByType<PlayerInventory>();
            if (owner == null) return;
            if (CanPractice(owner, owner.CaveBellTitleTutorialItem))
            {
                Queue(owner, owner.CaveBellTitleTutorialItem);
                return;
            }
            if (shownClasses.Contains(CharacterSelectionState.SelectedClass)) return;
            foreach (var stack in owner.Items)
                if (stack.Item.EquipmentSlot != EquipmentSlot.Title && CanPractice(owner, stack.Item))
                { Queue(owner, stack.Item); return; }
        }

        private void Update()
        {
            if (GameplayHudVisibility.IsMenuScene || SceneLoadingScreen.IsVisible)
            {
                Finish();
                pending = false;
                return;
            }
            if (EasternFantasy.Saving.SaveGameService.IsRestoring || SoulstoneTutorialUI.IsRunning) return;
            if (!pending && !running) RecoverUnfinishedTutorial();
            if (pending && !running && !MapTutorialUI.IsRunning && !StatsSkillTutorialUI.IsRunning
                && !CaveBellInteraction.IsViewing
                && (DungeonWindowUI.Instance == null || !DungeonWindowUI.Instance.IsOpen)
                && (ShopWindowUI.Instance == null || !ShopWindowUI.Instance.IsOpen)
                && (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive)
                && (EquipmentUpgradeWindowUI.Instance == null || !EquipmentUpgradeWindowUI.Instance.IsOpen)) Begin();
            if (!running || Time.frameCount == openedFrame) return;
            if (pendingInventory == null || pendingInventory.CurrentClass != tutorialClass) { Finish(); return; }
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                // Normal window shortcuts are blocked during the guide; accept only the requested opening action.
                if (keyboard.iKey.wasPressedThisFrame) HandleShortcut(false);
                else if (keyboard.jKey.wasPressedThisFrame) HandleShortcut(true);
                if (keyboard.spaceKey.wasPressedThisFrame) Advance();
            }
            EvaluateProgress();
        }

        private void HandleShortcut(bool equipmentKey)
        {
            if (!running) return;
            if (!equipmentKey && CurrentStep == 0) inventory.SetOpenForTutorial(true);
            else if (equipmentKey && CurrentStep == 2) equipment.SetOpenForTutorial(true);
            EvaluateProgress();
        }

        private void EvaluateProgress()
        {
            if (CurrentStep == 0 && inventory.IsOpen
                || CurrentStep == 1 && pendingInventory.IsEquipped(practiceItem)
                || CurrentStep == 2 && equipment.IsOpen)
                guide.CompleteAction();
        }

        private void Begin()
        {
            inventory = InventoryWindowUI.Instance; equipment = EquipmentWindowUI.Instance;
            if (inventory == null || equipment == null) return;
            if (!CanPractice(pendingInventory, practiceItem)) { pending = false; return; }
            pending = false; running = true; active = this; openedFrame = Time.frameCount;
            tutorialClass = pendingInventory.CurrentClass;
            titleTutorial = practiceItem.EquipmentSlot == EquipmentSlot.Title;
            if (!titleTutorial) shownClasses.Add(tutorialClass);
            originalCategory = inventory.SelectedCategory;
            Capture(inventory.TutorialTarget); Capture(equipment.TutorialTarget);
            equipment.CloseFromInventory();
            input = pendingInventory.GetComponent<PlayerInputReader>();
            if (input != null) { inputWasEnabled = input.enabled; input.enabled = false; }
            pendingInventory.GetComponent<PlayerMovement2D>()?.ClearInput();
            GameTimeController.SetPaused(this, true);
            canvasObject = new GameObject("InventoryEquipmentTutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5000;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            canvasRect = (RectTransform)canvasObject.transform;
            var overlay = new GameObject("InventoryEquipmentGuide", typeof(RectTransform), typeof(PetCompanionTutorialUI));
            overlay.transform.SetParent(canvasObject.transform, false); guide = overlay.GetComponent<PetCompanionTutorialUI>();
            targets = new RectTransform[Titles.Length];
            string itemName = practiceItem.DisplayName;
            string[] messages = {
                "장비를 획득했어요.\nI키로 인벤토리를 직접 열어 보세요.",
                $"‘{itemName}’을 더블클릭해 장착하세요.",
                "장비를 장착했습니다!\nJ키로 장비창을 직접 열어 보세요.",
                "I: 인벤토리 · J: 장비창\n두 창을 열고 장비를 알맞은 칸에 드래그 앤 드롭해도 착용돼요." };
            if (titleTutorial)
                messages = new[]
                {
                    $"동자승에게 ‘{itemName}’ 칭호를 받았어요.\n장착하면 공격력이 {practiceItem.TitleAttackBonusRate:P0} 증가하고 머리 위에 칭호가 표시돼요.\nI키로 인벤토리를 열어 보세요.",
                    $"강조된 종 아이콘 ‘{itemName}’을 더블클릭해 장착하세요.",
                    "칭호를 장착했어요.\nJ키로 장비창을 열어 보세요.",
                    "신발 아래 칭호 칸에 장착한 칭호가 표시돼요.\n칭호 칸을 더블클릭하면 해제할 수 있어요."
                };
            guide.ShowCustom(PlayerStatsWindowUI.Instance != null ? PlayerStatsWindowUI.Instance.TutorialFont : TMPro.TMP_Settings.defaultFontAsset,
                titleTutorial ? "칭호 튜토리얼" : "인벤토리·장비 튜토리얼",
                titleTutorial ? TitleTutorialTitles : Titles, messages, targets, PrepareStep, Complete, true, false);
        }

        private void Capture(RectTransform panel)
        {
            if (panel != null) layouts[panel] = new LayoutState { Position = panel.position, Scale = panel.localScale };
        }

        private void PlacePanel(RectTransform panel, float centerX, float widthFraction)
        {
            if (panel == null || !layouts.TryGetValue(panel, out var saved)) return;
            panel.position = saved.Position; panel.localScale = saved.Scale;
            var corners = new Vector3[4]; panel.GetWorldCorners(corners);
            Vector2 min = canvasRect.InverseTransformPoint(corners[0]), max = canvasRect.InverseTransformPoint(corners[2]);
            float fit = Mathf.Min(1f, canvasRect.rect.width * widthFraction / Mathf.Max(1f, max.x - min.x), canvasRect.rect.height * .80f / Mathf.Max(1f, max.y - min.y));
            panel.localScale *= fit;
            Vector3 center = canvasRect.TransformPoint(canvasRect.rect.min + new Vector2(canvasRect.rect.width * centerX, canvasRect.rect.height * .53f));
            panel.position += center - panel.TransformPoint(panel.rect.center);
        }

        private void PrepareStep(int step)
        {
            inventory.Tooltip?.Hide();
            inventory.SelectCategory(ItemCategory.Equipment);
            guide.SetFocusInteraction(step == 1);
            guide.ClearKeyHint();
            if (step == 0)
            {
                inventory.SetOpen(false);
                guide.SetKeyHint("keyboard_i", "키로 인벤토리를 여세요.");
                guide.RequireAction("I키를 직접 눌러 주세요.");
            }
            else if (step < 3)
            {
                equipment.CloseFromInventory();
                inventory.SetOpenForTutorial(true);
                guide.RequireAction(step == 1
                    ? titleTutorial ? "강조된 칭호를 더블클릭하세요." : "강조된 장비를 더블클릭하세요."
                    : "J키를 직접 눌러 주세요.");
            }
            else
            {
                equipment.SetOpenForTutorial(true);
                // Show only the demonstrated window, so both panels can use their normal readable size.
                inventory.TutorialTarget.gameObject.SetActive(false);
                guide.SetControlsHint("Space 또는 안내 완료");
            }
            UpdateFocus();
        }

        private void UpdateFocus()
        {
            if (guide == null || targets == null) return;
            if (equipment.IsOpen) PlacePanel(equipment.TutorialTarget, .25f, .46f);
            else if (inventory.IsOpen) PlacePanel(inventory.TutorialTarget, .25f, .46f);
            Canvas.ForceUpdateCanvases();
            int step = CurrentStep;
            if (step < 0) return;
            if (step == 1) inventory.RevealTutorialItem(practiceItem);
            targets[step] = step == 0 ? null : step == 3
                ? titleTutorial ? equipment.TutorialSlotTarget(EquipmentSlot.Title) : equipment.TutorialTarget
                : step == 1 ? inventory.TutorialItemTargetFor(practiceItem) : inventory.TutorialTarget;
        }

        public void Advance() { if (running) guide.Advance(); }
        private void Complete()
        {
            if (titleTutorial) pendingInventory?.CompleteCaveBellTitleTutorial(practiceItem);
            else completedClasses.Add(tutorialClass);
            Finish();
        }

        public void Finish()
        {
            if (!running) return;
            running = false;
            if (!titleTutorial && !completedClasses.Contains(tutorialClass)) shownClasses.Remove(tutorialClass);
            if (guide != null) guide.Hide();
            inventory?.SetOpen(false);
            inventory?.SelectCategory(originalCategory);
            foreach (var pair in layouts) if (pair.Key != null) { pair.Key.position = pair.Value.Position; pair.Key.localScale = pair.Value.Scale; }
            layouts.Clear();
            if (input != null) input.enabled = inputWasEnabled;
            if (pendingInventory != null) pendingInventory.GetComponent<PlayerMovement2D>()?.ClearInput();
            GameTimeController.SetPaused(this, false);
            if (canvasObject != null) Destroy(canvasObject);
            pending = false;
            practiceItem = null;
            pendingInventory = null;
            nextRecoveryCheck = Time.unscaledTime + .5f;
            if (active == this) active = null;
        }

        private void OnDisable() { PlayerInventory.ItemAcquired -= OnItemCollected; pending = false; Finish(); }
    }
}
