using EasternFantasy.Inventory;
using EasternFantasy.Skill;
using EasternFantasy.Player;
using EasternFantasy.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class QuickSlotBarUI : MonoBehaviour
    {
        public static readonly char[] SkillHotkeys = { 'Q', 'W', 'E', 'R', 'A', 'S', 'D', 'F' };
        [SerializeField] private QuickSkillSlotUI[] slots;
        [SerializeField] private QuickConsumableSlotUI[] consumableSlots;
        [SerializeField] private TMP_Text currencyText;
        [SerializeField] private PlayerSkillSystem skillSystem;
        [SerializeField] private PlayerCurrency currency;
        [SerializeField] private PlayerInventory inventory;

        private IPlayerSkillCaster caster;

        public static QuickSlotBarUI Instance { get; private set; }
        public RectTransform TutorialTarget => slots != null && slots.Length > 0 && slots[0] != null ? slots[0].transform.parent as RectTransform : null;
        private bool subscribed;
        private PlayerSkillSystem subscribedSkillSystem;
        private PlayerCurrency subscribedCurrency;
        private PlayerInventory subscribedInventory;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameplayHudVisibility.Attach(gameObject);
        }

        private void Start()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (GameplayHudVisibility.IsMenuScene) return;
            if (skillSystem == null || currency == null || inventory == null)
                ResolveReferences();

            Keyboard keyboard = Keyboard.current;
            PlayerEntity player = skillSystem != null
                ? skillSystem.GetComponent<PlayerEntity>()
                : null;
            if (skillSystem != null && keyboard != null && Time.timeScale > 0f
                && (EasternFantasy.Dungeon.DungeonRunController.Instance == null
                    || !EasternFantasy.Dungeon.DungeonRunController.Instance.IsExitPromptOpen)
                && (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive)
                && (InventoryWindowUI.Instance == null || !InventoryWindowUI.Instance.IsOpen)
                && (SkillWindowUI.Instance == null || !SkillWindowUI.Instance.IsOpen)
                && (PlayerStatsWindowUI.Instance == null || !PlayerStatsWindowUI.Instance.IsOpen)
                && (player == null || !player.IsDead))
            {
                if (keyboard.qKey.wasPressedThisFrame) TryUseSlot(0);
                if (keyboard.wKey.wasPressedThisFrame) TryUseSlot(1);
                if (keyboard.eKey.wasPressedThisFrame) TryUseSlot(2);
                if (keyboard.rKey.wasPressedThisFrame) TryUseSlot(3);
                if (keyboard.aKey.wasPressedThisFrame) TryUseSlot(4);
                if (keyboard.sKey.wasPressedThisFrame) TryUseSlot(5);
                if (keyboard.dKey.wasPressedThisFrame) TryUseSlot(6);
                if (keyboard.fKey.wasPressedThisFrame) TryUseSlot(7);
                if (inventory != null)
                {
                    if (keyboard.digit1Key.wasPressedThisFrame) TryUseConsumableSlot(0);
                    if (keyboard.digit2Key.wasPressedThisFrame) TryUseConsumableSlot(1);
                    if (keyboard.digit3Key.wasPressedThisFrame) TryUseConsumableSlot(2);
                    if (keyboard.digit4Key.wasPressedThisFrame) TryUseConsumableSlot(3);
                }
            }

            if (slots != null)
                foreach (QuickSkillSlotUI slot in slots)
                    slot?.RefreshCooldown();
        }

        private void TryUseSlot(int index)
        {
            if (skillSystem.TryUseQuickSlot(index))
                return;
            string reason = slots != null && index < slots.Length && slots[index] != null
                ? slots[index].GetUnavailableReason() : string.Empty;
            ShowFailure(string.IsNullOrEmpty(reason) ? "지금 사용할 수 없습니다" : reason);
        }

        private void ShowFailure(string message) => SystemNoticeUI.Show(message, this);

        public void TryUseConsumableSlot(int index)
        {
            if (inventory == null) return;
            if (inventory.TryUseConsumableSlot(index)) return;
            ItemDefinition item = index >= 0 && index < inventory.ConsumableSlots.Count
                ? inventory.ConsumableSlots[index] : null;
            string reason = item == null ? "퀵슬롯에 소모품을 등록해 주세요."
                : inventory.GetQuantity(item) <= 0 ? "등록된 소모품이 없습니다."
                : inventory.CurrentClass == EasternFantasy.CharacterSelection.CharacterClassId.Monk
                    && item.ConsumableEffect == ConsumableEffect.RestoreMana
                    ? "해당 직업이 사용할 수 없는 아이템입니다."
                : "지금은 이 소모품을 사용할 수 없습니다.";
            ShowFailure(reason);
        }

        private void ResolveReferences()
        {
            if (skillSystem == null)
                skillSystem = FindFirstObjectByType<PlayerSkillSystem>();
            if (currency == null)
                currency = FindFirstObjectByType<PlayerCurrency>();
            if (inventory == null)
                inventory = FindFirstObjectByType<PlayerInventory>();

            if (skillSystem != null)
                caster = skillSystem.ResolveSkillCaster();

            if (skillSystem == null || currency == null || inventory == null)
                return;

            if (subscribed
                && (subscribedSkillSystem != skillSystem || subscribedCurrency != currency
                    || subscribedInventory != inventory))
            {
                Unsubscribe();
            }

            if (!subscribed)
            {
                skillSystem.QuickSlotsChanged += RefreshSlots;
                currency.CurrencyChanged += RefreshCurrency;
                inventory.InventoryChanged += RefreshConsumables;
                subscribedSkillSystem = skillSystem;
                subscribedCurrency = currency;
                subscribedInventory = inventory;
                subscribed = true;
            }

            for (int i = 0; slots != null && i < slots.Length; i++)
                slots[i]?.Initialize(skillSystem, caster, i);
            for (int i = 0; consumableSlots != null && i < consumableSlots.Length; i++)
                consumableSlots[i]?.Initialize(inventory, i);
            RefreshSlots();
            RefreshConsumables();
            RefreshCurrency(currency.Yeopjeon);
        }

        private void RefreshSlots()
        {
            if (slots == null)
                return;
            foreach (QuickSkillSlotUI slot in slots)
                slot?.Refresh();
        }

        private void RefreshConsumables()
        {
            if (consumableSlots == null)
                return;
            foreach (QuickConsumableSlotUI slot in consumableSlots)
                slot?.Refresh();
        }

        private void RefreshCurrency(int amount)
        {
            if (currencyText != null)
                currencyText.text = $"{amount:N0}";
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            if (subscribedSkillSystem != null)
                subscribedSkillSystem.QuickSlotsChanged -= RefreshSlots;
            if (subscribedCurrency != null)
                subscribedCurrency.CurrencyChanged -= RefreshCurrency;
            if (subscribedInventory != null)
                subscribedInventory.InventoryChanged -= RefreshConsumables;

            subscribedSkillSystem = null;
            subscribedCurrency = null;
            subscribedInventory = null;
            subscribed = false;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            SystemNoticeUI.Hide(this);
            if (Instance == this)
                Instance = null;
        }
    }
}
