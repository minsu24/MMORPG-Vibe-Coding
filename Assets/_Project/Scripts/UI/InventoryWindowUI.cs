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
        [SerializeField] private InventorySlotUI[] slots;
        [SerializeField] private ItemTooltipUI tooltip;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private TMPro.TMP_Text currencyText;

        private PlayerCurrency currency;
        private Vector2 defaultWindowPosition;
        private bool slotsInitialized;

        private ItemCategory selectedCategory = ItemCategory.Equipment;

        public static InventoryWindowUI Instance { get; private set; }
        public RectTransform TutorialTarget => windowRoot != null ? windowRoot.transform as RectTransform : null;
        public RectTransform TutorialCategoryTarget(ItemCategory category) => (category == ItemCategory.Equipment ? equipmentTab : category == ItemCategory.Consumable ? consumableTab : miscellaneousTab).transform as RectTransform;
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
            if (currency != null)
                currency.CurrencyChanged += OnCurrencyChanged;
            Refresh();
        }

        private void Update()
        {
            if (StatsSkillTutorialUI.IsRunning || InventoryEquipmentTutorialUI.IsRunning || DungeonWindowUI.IsTutorialRunning || EquipmentUpgradeWindowUI.IsTutorialRunning || ShopWindowUI.IsTutorialRunning) return;
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
            selectedCategory = category;
            tooltip?.Hide();
            Refresh();
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

            for (int i = 0; i < slots.Length; i++)
                slots[i].Clear(i);

            foreach (InventoryStack stack in inventory.Items)
            {
                if (stack.Item == null || stack.Item.Category != selectedCategory)
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
