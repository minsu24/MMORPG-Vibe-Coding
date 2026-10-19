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
    public sealed class InventoryEquipmentTutorialUI : MonoBehaviour
    {
        private static readonly HashSet<CharacterClassId> shownClasses = new HashSet<CharacterClassId>();
        private static InventoryEquipmentTutorialUI active;
        private static readonly HashSet<CharacterClassId> completedClasses = new HashSet<CharacterClassId>();
        public static bool HasFinishedForCurrentClass => completedClasses.Contains(CharacterSelectionState.SelectedClass);
        public static bool IsRunning => active != null && active.running;
        public bool IsShowing => running;
        public int CurrentStep => guide != null ? guide.CurrentStep : -1;
        public int StepCount => Titles.Length;
        private bool pending, running, inputWasEnabled;
        private int openedFrame;
        private ItemCategory originalCategory, pickedCategory;
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
            "1. 인벤토리 열고 닫기", "2. 장비 아이템", "3. 소비 아이템", "4. 기타 아이템", "5. 아이템 정보 확인",
            "6. 장비창 열고 닫기", "7. 장비 장착하기", "8. 장비 해제하기", "9. 장착한 아이템 정보 확인" };
        private static readonly string[] Messages = {
            "아이템을 습득하면 인벤토리에 보관됩니다.\nI키로 인벤토리를 열고 닫을 수 있어요.\n장비·소비·기타 탭으로 아이템을 나누어 확인할 수 있습니다.",
            "장비 탭에는 캐릭터가 장착하는 아이템이 들어갑니다.\n직업별 무기, 모자·상의·하의·장갑·신발 같은 방어구,\n반지·목걸이·팔찌 같은 장신구를 이곳에서 확인하세요.",
            "소비 탭에는 사용하면 수량이 줄어드는 아이템이 들어갑니다.\n체력이나 자원을 회복하는 물약이 대표적인 소비 아이템이에요.",
            "기타 탭에는 성장이나 제작에 사용하는 재료 등이 들어갑니다.\n도깨비 방망이, 영혼 조각, 도깨비의 영혼 같은 아이템을\n이곳에서 확인할 수 있어요.",
            "인벤토리의 아이템에 마우스를 올리면 정보가 표시됩니다.\n이름·등급·설명·효과와 장착 가능한 직업 등을 확인하세요.\n판매 가능 여부도 아이템 정보에서 확인할 수 있습니다.",
            "J키로 장비창을 열고 닫을 수 있습니다.\n장비창을 열면 인벤토리도 함께 표시되어 장비를 비교할 수 있어요.\n무기·방어구·장신구의 장착 상태를 확인하는 창입니다.",
            "인벤토리의 장비를 장비창의 알맞은 칸으로 드래그 앤 드롭하세요.\n인벤토리에서 장비를 더블클릭해도 장착할 수 있습니다.\n자신의 직업이 사용할 수 있는 장비인지 먼저 확인하세요.\n이 안내 중에는 장비를 변경하지 않습니다.",
            "장비창의 장착된 아이템을 인벤토리의 장비 칸으로\n드래그 앤 드롭하면 해제할 수 있습니다.\n장비창의 아이템을 더블클릭해도 해제됩니다.\n장착과 해제를 활용해 원하는 장비로 바꿔 보세요.",
            "장비창에서도 아이템에 마우스를 올리면 정보를 볼 수 있습니다.\n현재 장착한 아이템의 등급과 능력치를 확인하세요.\n안내를 마치면 I키와 J키로 창을 열어 직접 살펴보세요!" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { shownClasses.Clear(); completedClasses.Clear(); active = null; }
        private void OnEnable() { PlayerInventory.ItemAcquired += OnItemCollected; }
        private void OnItemCollected(PlayerInventory owner, ItemDefinition item)
        {
            if (item == null || owner == null || running || pending || shownClasses.Contains(CharacterSelectionState.SelectedClass)) return;
            pendingInventory = owner; pickedCategory = item.Category; pending = true;
        }
        private void Update()
        {
            if (pending && !running && !StatsSkillTutorialUI.IsRunning
                && (DungeonWindowUI.Instance == null || !DungeonWindowUI.Instance.IsOpen)
                && (ShopWindowUI.Instance == null || !ShopWindowUI.Instance.IsOpen)
                && (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive)
                && (EquipmentUpgradeWindowUI.Instance == null || !EquipmentUpgradeWindowUI.Instance.IsOpen)) Begin();
            if (!running || Time.frameCount == openedFrame || Keyboard.current == null) return;
            if (Keyboard.current.spaceKey.wasPressedThisFrame) Advance();
        }
        private void Begin()
        {
            inventory = InventoryWindowUI.Instance; equipment = EquipmentWindowUI.Instance;
            if (inventory == null || equipment == null || pendingInventory == null) return;
            pending = false; running = true; active = this; openedFrame = Time.frameCount;
            shownClasses.Add(CharacterSelectionState.SelectedClass);
            originalCategory = inventory.SelectedCategory;
            Capture(inventory.TutorialTarget); Capture(equipment.TutorialTarget);
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
            guide.ShowCustom(PlayerStatsWindowUI.Instance != null ? PlayerStatsWindowUI.Instance.TutorialFont : TMPro.TMP_Settings.defaultFontAsset,
                "인벤토리·장비 튜토리얼", Titles, Messages, targets, PrepareStep, Complete, true, false);
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
            float fit = Mathf.Min(1f, canvasRect.rect.width * widthFraction / Mathf.Max(1f, max.x-min.x), canvasRect.rect.height * .80f / Mathf.Max(1f, max.y-min.y));
            panel.localScale *= fit;
            Vector3 center = canvasRect.TransformPoint(canvasRect.rect.min + new Vector2(canvasRect.rect.width * centerX, canvasRect.rect.height * .53f));
            panel.position += center - panel.TransformPoint(panel.rect.center);
        }
        private void PrepareStep(int step)
        {
            inventory.Tooltip?.Hide();
            if (step < 5) {
                equipment.CloseFromInventory(); inventory.SetOpenForTutorial(true);
                var category = step == 2 ? ItemCategory.Consumable : step == 3 ? ItemCategory.Miscellaneous : step == 4 ? pickedCategory : ItemCategory.Equipment;
                inventory.SelectCategory(category); PlacePanel(inventory.TutorialTarget, .25f, .44f);
                targets[step] = step >= 1 && step <= 3 ? inventory.TutorialCategoryTarget(category) : step == 4 ? inventory.TutorialItemTarget : inventory.TutorialTarget;
            }
            else {
                equipment.SetOpenForTutorial(true); inventory.SelectCategory(ItemCategory.Equipment);
                PlacePanel(inventory.TutorialTarget, .135f, .235f); PlacePanel(equipment.TutorialTarget, .375f, .225f);
                targets[step] = step == 6 ? inventory.TutorialItemTarget : step == 7 ? equipment.TutorialWeaponTarget : equipment.TutorialTarget;
            }
            Canvas.ForceUpdateCanvases();
        }
        public void Advance() { if (running) guide.Advance(); }
        private void Complete()
        {
            completedClasses.Add(CharacterSelectionState.SelectedClass);
            Finish();
        }
        public void Finish()
        {
            if (!running) return;
            running = false;
            if (guide != null) guide.Hide();
            inventory?.SetOpen(false);
            inventory?.SelectCategory(originalCategory);
            foreach (var pair in layouts) if (pair.Key != null) { pair.Key.position = pair.Value.Position; pair.Key.localScale = pair.Value.Scale; }
            layouts.Clear();
            if (input != null) input.enabled = inputWasEnabled;
            if (pendingInventory != null) pendingInventory.GetComponent<PlayerMovement2D>()?.ClearInput();
            GameTimeController.SetPaused(this, false);
            if (canvasObject != null) Destroy(canvasObject);
            if (active == this) active = null;
        }
        private void OnDisable() { PlayerInventory.ItemAcquired -= OnItemCollected; pending = false; Finish(); }
    }
}
