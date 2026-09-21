using EasternFantasy.Inventory;
using EasternFantasy.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class ShopWindowUI : MonoBehaviour
    {
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private Button buyTab;
        [SerializeField] private Button sellTab;
        [SerializeField] private TMP_Text currencyText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private ShopSlotUI[] slots;

        private PlayerInventory inventory;
        private PlayerCurrency currency;
        private Shopkeeper shopkeeper;
        private bool selling;
        private bool inventorySubscribed;
        private bool currencySubscribed;

        public static ShopWindowUI Instance { get; private set; }
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;

        private void Awake()
        {
            Instance = this;
            if (windowRoot != null)
                windowRoot.SetActive(false);
        }

        private void Start()
        {
            inventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);
            currency = FindFirstObjectByType<PlayerCurrency>(FindObjectsInactive.Include);
            buyTab.onClick.AddListener(() => SetSelling(false));
            sellTab.onClick.AddListener(() => SetSelling(true));
            foreach (ShopSlotUI slot in slots)
                slot.Initialize(this);

            SubscribeToPlayerEvents();
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Close();
        }

        public void Open(Shopkeeper target)
        {
            if (target == null || !ResolvePlayerReferences())
                return;

            shopkeeper = target;
            selling = false;
            messageText.text = string.Empty;
            InventoryWindowUI.Instance?.SetOpen(false);
            PlayerStatsWindowUI.Instance?.SetOpen(false);
            SkillWindowUI.Instance?.SetOpen(false);
            windowRoot.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (windowRoot != null)
                windowRoot.SetActive(false);
        }

        public void ProcessItem(ItemDefinition item)
        {
            if (item == null || !ResolvePlayerReferences())
                return;

            if (selling)
            {
                if (!inventory.RemoveItem(item))
                {
                    messageText.text = "판매할 수 없습니다.";
                    return;
                }
                currency.Add(item.SellPrice);
                messageText.text = $"{item.DisplayName} 판매 완료";
            }
            else
            {
                if (!inventory.CanAdd(item))
                {
                    messageText.text = "인벤토리 공간이나 최대 수량을 확인하세요.";
                    return;
                }
                if (!currency.TrySpend(item.BuyPrice))
                {
                    messageText.text = "엽전이 부족합니다.";
                    return;
                }
                inventory.AddItem(item);
                messageText.text = $"{item.DisplayName} 구매 완료";
            }

            Refresh();
        }

        private void SetSelling(bool value)
        {
            selling = value;
            messageText.text = string.Empty;
            Refresh();
        }

        private void Refresh()
        {
            if (!ResolvePlayerReferences())
                return;

            currencyText.text = $"보유 엽전  {currency.Yeopjeon}";
            buyTab.interactable = selling;
            sellTab.interactable = !selling;
            int index = 0;

            if (!selling && shopkeeper != null)
            {
                foreach (ItemDefinition item in shopkeeper.Stock)
                {
                    if (item != null && index < slots.Length)
                        slots[index++].Bind(item, 0, false);
                }
            }
            else if (selling)
            {
                foreach (InventoryStack stack in inventory.Items)
                {
                    if (stack.Item != null && index < slots.Length)
                        slots[index++].Bind(stack.Item, stack.Quantity, true);
                }
            }

            while (index < slots.Length)
                slots[index++].Clear();
        }

        private void OnCurrencyChanged(int value)
        {
            Refresh();
        }

        private bool ResolvePlayerReferences()
        {
            if (inventory == null)
                inventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);
            if (currency == null)
                currency = FindFirstObjectByType<PlayerCurrency>(FindObjectsInactive.Include);
            SubscribeToPlayerEvents();
            return inventory != null && currency != null;
        }

        private void SubscribeToPlayerEvents()
        {
            if (inventory != null && !inventorySubscribed)
            {
                inventory.InventoryChanged += Refresh;
                inventorySubscribed = true;
            }
            if (currency != null && !currencySubscribed)
            {
                currency.CurrencyChanged += OnCurrencyChanged;
                currencySubscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (inventory != null && inventorySubscribed)
                inventory.InventoryChanged -= Refresh;
            if (currency != null && currencySubscribed)
                currency.CurrencyChanged -= OnCurrencyChanged;
            if (Instance == this)
                Instance = null;
        }
    }
}
