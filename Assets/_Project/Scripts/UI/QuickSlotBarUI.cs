using EasternFantasy.Inventory;
using EasternFantasy.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class QuickSlotBarUI : MonoBehaviour
    {
        [SerializeField] private QuickSkillSlotUI[] slots;
        [SerializeField] private TMP_Text currencyText;
        [SerializeField] private PlayerSkillSystem skillSystem;
        [SerializeField] private PlayerActiveSkillCaster caster;
        [SerializeField] private PlayerCurrency currency;

        public static QuickSlotBarUI Instance { get; private set; }
        private bool subscribed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (skillSystem == null || caster == null || currency == null)
                ResolveReferences();

            Keyboard keyboard = Keyboard.current;
            if (skillSystem != null && keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame)
                    skillSystem.TryUseQuickSlot(0);
                if (keyboard.digit2Key.wasPressedThisFrame)
                    skillSystem.TryUseQuickSlot(1);
                if (keyboard.digit3Key.wasPressedThisFrame)
                    skillSystem.TryUseQuickSlot(2);
            }

            if (slots != null)
                foreach (QuickSkillSlotUI slot in slots)
                    slot?.RefreshCooldown();
        }

        private void ResolveReferences()
        {
            if (skillSystem == null)
                skillSystem = FindFirstObjectByType<PlayerSkillSystem>();
            if (caster == null)
                caster = FindFirstObjectByType<PlayerActiveSkillCaster>();
            if (currency == null)
                currency = FindFirstObjectByType<PlayerCurrency>();

            if (skillSystem == null || caster == null || currency == null)
                return;

            if (!subscribed)
            {
                skillSystem.QuickSlotsChanged += RefreshSlots;
                currency.CurrencyChanged += RefreshCurrency;
                subscribed = true;
            }

            for (int i = 0; slots != null && i < slots.Length; i++)
                slots[i]?.Initialize(skillSystem, caster, i);
            RefreshSlots();
            RefreshCurrency(currency.Yeopjeon);
        }

        private void RefreshSlots()
        {
            if (slots == null)
                return;
            foreach (QuickSkillSlotUI slot in slots)
                slot?.Refresh();
        }

        private void RefreshCurrency(int amount)
        {
            if (currencyText != null)
                currencyText.text = $"엽전  {amount:N0}";
        }

        private void OnDestroy()
        {
            if (subscribed)
            {
                if (skillSystem != null)
                    skillSystem.QuickSlotsChanged -= RefreshSlots;
                if (currency != null)
                    currency.CurrencyChanged -= RefreshCurrency;
            }
            if (Instance == this)
                Instance = null;
        }
    }
}
