using System.Collections;
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
        private bool subscribed;
        private PlayerSkillSystem subscribedSkillSystem;
        private PlayerCurrency subscribedCurrency;
        private PlayerInventory subscribedInventory;
        private CanvasGroup failurePopup;
        private TMP_Text failureText;
        private Coroutine failureRoutine;

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
                    if (keyboard.digit1Key.wasPressedThisFrame) inventory.TryUseConsumableSlot(0);
                    if (keyboard.digit2Key.wasPressedThisFrame) inventory.TryUseConsumableSlot(1);
                    if (keyboard.digit3Key.wasPressedThisFrame) inventory.TryUseConsumableSlot(2);
                    if (keyboard.digit4Key.wasPressedThisFrame) inventory.TryUseConsumableSlot(3);
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

        private void ShowFailure(string message)
        {
            if (failurePopup == null)
                CreateFailurePopup();
            if (failurePopup == null)
                return;

            if (failureRoutine != null)
                StopCoroutine(failureRoutine);
            failureText.text = message;
            failurePopup.alpha = 1f;
            failurePopup.gameObject.SetActive(true);
            failureRoutine = StartCoroutine(FadeFailure());
        }

        private void CreateFailurePopup()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;
            GameObject popup = new GameObject("Skill Failure Popup", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(CanvasGroup));
            popup.transform.SetParent(canvas.rootCanvas.transform, false);
            popup.transform.SetAsLastSibling();
            RectTransform rect = popup.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 30f);
            rect.sizeDelta = new Vector2(520f, 72f);
            Image background = popup.GetComponent<Image>();
            background.color = new Color(0.055f, 0.065f, 0.075f, 0.92f);
            background.raycastTarget = false;
            Outline border = popup.GetComponent<Outline>();
            border.effectColor = new Color(0.93f, 0.65f, 0.31f, 0.85f);
            border.effectDistance = new Vector2(2f, -2f);
            failurePopup = popup.GetComponent<CanvasGroup>();
            failurePopup.interactable = false;
            failurePopup.blocksRaycasts = false;

            GameObject label = new GameObject("Reason", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(popup.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 6f);
            labelRect.offsetMax = new Vector2(-16f, -6f);
            failureText = label.GetComponent<TextMeshProUGUI>();
            failureText.font = currencyText != null ? currencyText.font
                : TMP_Settings.defaultFontAsset;
            failureText.fontSize = 28f;
            failureText.color = new Color(1f, 0.88f, 0.68f);
            failureText.alignment = TextAlignmentOptions.Center;
            failureText.raycastTarget = false;
            popup.SetActive(false);
        }

        private IEnumerator FadeFailure()
        {
            yield return new WaitForSecondsRealtime(0.85f);
            float elapsed = 0f;
            while (elapsed < 0.35f && failurePopup != null)
            {
                elapsed += Time.unscaledDeltaTime;
                failurePopup.alpha = 1f - Mathf.Clamp01(elapsed / 0.35f);
                yield return null;
            }
            if (failurePopup != null)
                failurePopup.gameObject.SetActive(false);
            failureRoutine = null;
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
            if (failurePopup != null)
                Destroy(failurePopup.gameObject);
            if (Instance == this)
                Instance = null;
        }
    }
}
