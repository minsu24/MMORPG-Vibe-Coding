using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class InventoryWindowUI : MonoBehaviour
    {
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private Button equipmentTab;
        [SerializeField] private Button consumableTab;
        [SerializeField] private Button miscellaneousTab;
        [SerializeField] private Button accessoryTab;
        [SerializeField] private InventorySlotUI[] slots;
        [SerializeField] private ItemTooltipUI tooltip;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private TMPro.TMP_Text currencyText;

        private PlayerCurrency currency;
        private Vector2 defaultWindowPosition;
        private bool slotsInitialized;
        private ScrollRect slotScroll;
        private GridLayoutGroup slotGrid;

        private ItemCategory selectedCategory = ItemCategory.Equipment;

        public static InventoryWindowUI Instance { get; private set; }
        public RectTransform TutorialTarget => windowRoot != null ? windowRoot.transform as RectTransform : null;
        public RectTransform TutorialCategoryTarget(ItemCategory category) =>
            (category == ItemCategory.Equipment || category == ItemCategory.Accessory ? equipmentTab
                : category == ItemCategory.Consumable ? consumableTab : miscellaneousTab).transform as RectTransform;
        public RectTransform TutorialItemTarget { get { foreach (var slot in slots) if (slot != null && slot.Definition != null) return slot.transform as RectTransform; return TutorialTarget; } }
        public bool IsUnlocked => InventoryEquipmentTutorialUI.HasFinishedForCurrentClass;
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;
        public ItemCategory SelectedCategory => selectedCategory;
        public ItemTooltipUI Tooltip => tooltip;
        public PlayerInventory Inventory => inventory;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            PrepareEquipmentSlots();
            if (accessoryTab != null) accessoryTab.gameObject.SetActive(false);
            if (windowRoot != null)
                defaultWindowPosition = ((RectTransform)windowRoot.transform).anchoredPosition;
            if (windowRoot != null)
                windowRoot.SetActive(false);
        }

        private void Start()
        {
            BindCurrentInventory();
            currency = FindFirstObjectByType<PlayerCurrency>();
            if (inventory == null)
            {
                Debug.LogError("InventoryWindowUI needs a PlayerInventory.", this);
                enabled = false;
                return;
            }

            equipmentTab.onClick.AddListener(() => SelectCategory(ItemCategory.Equipment));
            consumableTab.onClick.AddListener(() => SelectCategory(ItemCategory.Consumable));
            miscellaneousTab.onClick.AddListener(() => SelectCategory(ItemCategory.Miscellaneous));
            if (accessoryTab != null) accessoryTab.onClick.AddListener(() => SelectCategory(ItemCategory.Accessory));
            if (currency != null)
                currency.CurrencyChanged += OnCurrencyChanged;
            Refresh();
        }

        private void Update()
        {
            if (SoulstoneWindowUI.IsOpen || SoulstoneTutorialUI.IsRunning) return;
            if (MapTutorialUI.IsRunning || StatsSkillTutorialUI.IsRunning || InventoryEquipmentTutorialUI.IsRunning || DungeonWindowUI.IsTutorialRunning || EquipmentUpgradeWindowUI.IsTutorialRunning || ShopWindowUI.IsTutorialRunning) return;
            if (IsOpen && (inventory == null || inventory.gameObject == null))
                BindCurrentInventory();
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (IsOpen && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetOpen(false);
                return;
            }

            if (!keyboard.iKey.wasPressedThisFrame)
                return;

            bool dialogueBlocksOpening = !IsOpen
                && DialogueManager.Instance != null
                && DialogueManager.Instance.IsDialogueActive;
            if (!dialogueBlocksOpening)
                SetOpen(!IsOpen);
        }

        public void SetOpen(bool open) { SetOpenInternal(open, false); }
        internal void SetOpenForTutorial(bool open) { SetOpenInternal(open, true); }
        private void SetOpenInternal(bool open, bool forTutorial)
        {
            if (open && !forTutorial && !IsUnlocked) return;
            if (open && EquipmentUpgradeWindowUI.Instance != null && EquipmentUpgradeWindowUI.Instance.IsOpen) return;
            if (windowRoot == null)
                return;

            if (open)
            {
                BindCurrentInventory();
                PlayerStatsWindowUI.Instance?.SetOpen(false);
                SkillWindowUI.Instance?.SetOpen(false);
                Refresh();
            }
            else
            {
                tooltip?.Hide();
                EquipmentWindowUI.Instance?.CloseFromInventory();
            }

            windowRoot.SetActive(open);
        }

        public void SetEquipmentLayout(bool equipmentOpen)
        {
            if (windowRoot != null)
                ((RectTransform)windowRoot.transform).anchoredPosition = defaultWindowPosition
                    + (equipmentOpen ? new Vector2(-470f, 0f) : Vector2.zero);
        }

        private void BindCurrentInventory()
        {
            PlayerInventory current = FindFirstObjectByType<PlayerInventory>();
            if (current == inventory && current != null && slotsInitialized)
                return;
            if (inventory != null)
                inventory.InventoryChanged -= Refresh;
            inventory = current;
            if (inventory == null)
                return;
            foreach (InventorySlotUI slot in slots)
                slot.Initialize(inventory, tooltip);
            slotsInitialized = true;
            inventory.InventoryChanged += Refresh;
            EquipmentWindowUI.Instance?.SetInventory(inventory);
        }

        public void SelectCategory(ItemCategory category)
        {
            selectedCategory = category == ItemCategory.Accessory ? ItemCategory.Equipment : category;
            tooltip?.Hide();
            Refresh();
            if (slotScroll != null) { slotScroll.StopMovement(); slotScroll.verticalNormalizedPosition = 1f; }
        }

        private void PrepareEquipmentSlots()
        {
            if (slots == null || slots.Length == 0) return;
            var content = slots[0].transform.parent as RectTransform;
            slotGrid = content != null ? content.GetComponent<GridLayoutGroup>() : null;
            if (slotGrid == null) return;
            var viewport = new GameObject("Inventory Slot Scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            var rect = (RectTransform)viewport.transform;
            rect.SetParent(content.parent, false);
            rect.SetSiblingIndex(content.GetSiblingIndex());
            rect.anchorMin = content.anchorMin; rect.anchorMax = content.anchorMax;
            rect.pivot = content.pivot; rect.sizeDelta = content.sizeDelta;
            rect.anchoredPosition = content.anchoredPosition;
            viewport.GetComponent<Image>().color = Color.clear;
            content.SetParent(rect, false);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1f); content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            slotScroll = viewport.GetComponent<ScrollRect>();
            slotScroll.viewport = rect; slotScroll.content = content;
            slotScroll.horizontal = false; slotScroll.vertical = true;
            slotScroll.movementType = ScrollRect.MovementType.Clamped;
            slotScroll.scrollSensitivity = 35f;
            int originalCount = slots.Length;
            int capacity = PlayerInventory.GetSlotCapacity(ItemCategory.Equipment);
            System.Array.Resize(ref slots, Mathf.Max(originalCount, capacity));
            for (int i = originalCount; i < slots.Length; i++)
                slots[i] = Instantiate(slots[0], content);
        }

        public void Refresh()
        {
            if (inventory == null)
                return;

            if (currencyText != null)
                currencyText.text = $"{(currency != null ? currency.Yeopjeon : 0)}";

            equipmentTab.interactable = selectedCategory != ItemCategory.Equipment;
            consumableTab.interactable = selectedCategory != ItemCategory.Consumable;
            miscellaneousTab.interactable = selectedCategory != ItemCategory.Miscellaneous;
            if (accessoryTab != null) accessoryTab.interactable = selectedCategory != ItemCategory.Accessory;

            int capacity = PlayerInventory.GetSlotCapacity(selectedCategory);
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].gameObject.SetActive(i < capacity);
                slots[i].Clear(i);
            }
            if (slotGrid != null)
            {
                int rows = Mathf.CeilToInt((float)capacity / Mathf.Max(1, slotGrid.constraintCount));
                ((RectTransform)slotGrid.transform).sizeDelta = new Vector2(0f,
                    slotGrid.padding.vertical + rows * slotGrid.cellSize.y + Mathf.Max(0, rows - 1) * slotGrid.spacing.y);
            }

            foreach (InventoryStack stack in inventory.Items)
            {
                if (stack.Item == null || stack.Item.InventoryCategory != selectedCategory)
                    continue;
                if (stack.SlotIndex >= 0 && stack.SlotIndex < slots.Length)
                    slots[stack.SlotIndex].Bind(stack.Item, stack.Quantity, stack.SlotIndex);
            }
        }

        private void OnCurrencyChanged(int amount)
        {
            Refresh();
        }

        private void OnDestroy()
        {
            if (inventory != null)
                inventory.InventoryChanged -= Refresh;
            if (currency != null)
                currency.CurrencyChanged -= OnCurrencyChanged;
            if (Instance == this)
                Instance = null;
        }
    }
}
