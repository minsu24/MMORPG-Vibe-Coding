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

        private ItemCategory selectedCategory = ItemCategory.Equipment;

        public static InventoryWindowUI Instance { get; private set; }
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;
        public ItemCategory SelectedCategory => selectedCategory;

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
                windowRoot.SetActive(false);
        }

        private void Start()
        {
            if (inventory == null)
                inventory = FindFirstObjectByType<PlayerInventory>();
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
            foreach (InventorySlotUI slot in slots)
                slot.Initialize(inventory, tooltip);

            inventory.InventoryChanged += Refresh;
            if (currency != null)
                currency.CurrencyChanged += OnCurrencyChanged;
            Refresh();
        }

        private void Update()
        {
            if (Keyboard.current?.iKey.wasPressedThisFrame != true)
                return;

            bool dialogueBlocksOpening = !IsOpen
                && DialogueManager.Instance != null
                && DialogueManager.Instance.IsDialogueActive;
            if (!dialogueBlocksOpening)
                SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            if (windowRoot == null)
                return;

            if (open)
            {
                PlayerStatsWindowUI.Instance?.SetOpen(false);
                SkillWindowUI.Instance?.SetOpen(false);
                Refresh();
            }
            else
            {
                tooltip?.Hide();
            }

            windowRoot.SetActive(open);
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
                currencyText.text = $"엽전  {(currency != null ? currency.Yeopjeon : 0)}";

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
