using System.Collections.Generic;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Player;
using DG.Tweening;
using EasternFantasy.Inventory;
using EasternFantasy.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed partial class ShopWindowUI : MonoBehaviour
    {
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private Button buyTab;
        [SerializeField] private Button sellTab;
        [SerializeField] private TMP_Text currencyText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private ShopSlotUI[] slots;
        [SerializeField] private ItemTooltipUI tooltip;
        private ShopSlotUI tooltipSlot;

        [Header("DOTween popup")]
        [SerializeField, Min(0.01f)] private float openDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float closeDuration = 0.18f;
        [SerializeField, Range(0.5f, 1f)] private float popupStartScale = 0.9f;
        private CanvasGroup windowGroup;
        private RectTransform popupPanel;
        private Vector3 panelScale;
        private Sequence popupSequence;
        private bool closing;

        private PlayerInventory inventory;
        private PlayerCurrency currency;
        private Shopkeeper shopkeeper;
        private bool selling;
        private bool inventorySubscribed;
        private bool currencySubscribed;
        private static readonly HashSet<CharacterClassId> tutorialCompletedClasses = new HashSet<CharacterClassId>();
        private PetCompanionTutorialUI tutorial;
        private GameObject tutorialCanvas;
        private PlayerInputReader tutorialInput;
        private bool inputWasEnabled;
        private Vector3 tutorialPanelPosition, tutorialPanelScale;
        public static bool IsTutorialRunning => Instance != null && Instance.tutorial != null && Instance.tutorial.IsShowing;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTutorials() { tutorialCompletedClasses.Clear(); }

        public static ShopWindowUI Instance { get; private set; }
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;
        public bool IsSelling => selling;

        public void ShowItemTooltip(ShopSlotUI slot, ItemDefinition item)
        {
            if (slot == null || item == null || !IsOpen || closing || IsTutorialRunning || tooltip == null || !ResolvePlayerReferences()) return;
            tooltipSlot = slot;
            tooltip.ShowShop(item, inventory, slot.transform as RectTransform, selling);
        }

        public void HideItemTooltip(ShopSlotUI slot = null, bool delayed = false)
        {
            if (slot != null && tooltipSlot != slot) return;
            if (delayed) tooltip?.HideAfterPointerExit();
            else { tooltip?.Hide(); tooltipSlot = null; }
        }

        private void Awake()
        {
            Instance = this;
            PreparePopup();
            CloseInstant();
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
            if (IsTutorialRunning)
            {
                if (Keyboard.current?.spaceKey.wasPressedThisFrame == true) tutorial.Advance();
                return;
            }
            if (IsOpen && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Close();
        }

        public void Open(Shopkeeper target)
        {
            if (MapTutorialUI.IsRunning || StatsSkillTutorialUI.IsRunning || InventoryEquipmentTutorialUI.IsRunning || DungeonWindowUI.IsTutorialRunning || EquipmentUpgradeWindowUI.IsTutorialRunning || IsTutorialRunning) return;
            if (target == null || windowRoot == null || !isActiveAndEnabled || !ResolvePlayerReferences())
                return;

            shopkeeper = target;
            selling = false;
            messageText.text = string.Empty;
            InventoryWindowUI.Instance?.SetOpen(false);
            PlayerStatsWindowUI.Instance?.SetOpen(false);
            SkillWindowUI.Instance?.SetOpen(false);
            Refresh();
            AnimateOpen();
        }

        private void PreparePopup()
        {
            if (windowGroup != null || windowRoot == null) return;
            windowGroup = windowRoot.GetComponent<CanvasGroup>();
            if (windowGroup == null) windowGroup = windowRoot.AddComponent<CanvasGroup>();
            // The shop root is the complete visual panel, including its background.
            popupPanel = windowRoot.GetComponent<RectTransform>();
            panelScale = popupPanel.localScale;
        }

        private void AnimateOpen()
        {
            PreparePopup();
            popupSequence?.Kill();
            closing = false;
            if (!windowRoot.activeSelf)
            {
                windowGroup.alpha = 0f;
                popupPanel.localScale = panelScale * popupStartScale;
            }
            windowRoot.SetActive(true);
            windowGroup.interactable = false;
            windowGroup.blocksRaycasts = true;
            popupSequence = DOTween.Sequence().SetUpdate(true);
            popupSequence.Append(windowGroup.DOFade(1f, Mathf.Max(0.01f, openDuration)).SetEase(Ease.OutQuad));
            popupSequence.Join(popupPanel.DOScale(panelScale, Mathf.Max(0.01f, openDuration)).SetEase(Ease.OutBack));
            popupSequence.OnComplete(() => { popupSequence = null; windowGroup.interactable = true; BeginTutorial(); });
        }

        public void Close()
        {
            HideItemTooltip();
            EndTutorial();
            if (closing) return;
            if (!IsOpen || !isActiveAndEnabled) { CloseInstant(); return; }
            PreparePopup();
            popupSequence?.Kill();
            closing = true;
            windowGroup.interactable = false;
            windowGroup.blocksRaycasts = true;
            popupSequence = DOTween.Sequence().SetUpdate(true);
            popupSequence.Append(windowGroup.DOFade(0f, Mathf.Max(0.01f, closeDuration)).SetEase(Ease.InQuad));
            popupSequence.Join(popupPanel.DOScale(panelScale * popupStartScale,
                Mathf.Max(0.01f, closeDuration)).SetEase(Ease.InQuad));
            popupSequence.OnComplete(() => { popupSequence = null; CloseInstant(); });
        }

        private void CloseInstant()
        {
            HideItemTooltip();
            EndTutorial();
            popupSequence?.Kill();
            popupSequence = null;
            closing = false;
            if (windowGroup != null)
            {
                windowGroup.alpha = 0f;
                windowGroup.interactable = false;
                windowGroup.blocksRaycasts = false;
            }
            if (popupPanel != null) popupPanel.localScale = panelScale;
            if (windowRoot != null) windowRoot.SetActive(false);
        }
        public void ProcessItem(ItemDefinition item)
        {
            if (IsTutorialRunning) return;
            if (item == null || !IsOpen || closing || windowGroup == null || !windowGroup.interactable || !ResolvePlayerReferences())
                return;

            if (selling)
            {
                if (!item.CanSell || !inventory.RemoveItem(item))
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

        private void BeginTutorial()
        {
            if (tutorialCompletedClasses.Contains(CharacterSelectionState.SelectedClass) || IsTutorialRunning) return;
            tutorialPanelPosition = popupPanel.position; tutorialPanelScale = popupPanel.localScale;
            tutorialInput = inventory != null ? inventory.GetComponent<PlayerInputReader>() : null;
            if (tutorialInput != null) { inputWasEnabled = tutorialInput.enabled; tutorialInput.enabled = false; }
            inventory?.GetComponent<PlayerMovement2D>()?.ClearInput();
            GameTimeController.SetPaused(this, true);
            tutorialCanvas = new GameObject("ShopTutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = tutorialCanvas.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5000;
            var scaler = tutorialCanvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            Canvas.ForceUpdateCanvases();
            var canvasRect = (RectTransform)tutorialCanvas.transform;
            var corners = new Vector3[4]; popupPanel.GetWorldCorners(corners);
            Vector2 min = canvasRect.InverseTransformPoint(corners[0]), max = canvasRect.InverseTransformPoint(corners[2]);
            float fit = Mathf.Min(1f, canvasRect.rect.width * .45f / Mathf.Max(1f, max.x-min.x), canvasRect.rect.height * .8f / Mathf.Max(1f, max.y-min.y));
            popupPanel.localScale *= fit;
            var center = canvasRect.TransformPoint(canvasRect.rect.min + new Vector2(canvasRect.rect.width * .25f, canvasRect.rect.height * .53f));
            popupPanel.position += center - popupPanel.TransformPoint(popupPanel.rect.center);
            var overlay = new GameObject("ShopGuide", typeof(RectTransform), typeof(PetCompanionTutorialUI));
            overlay.transform.SetParent(tutorialCanvas.transform, false); tutorial = overlay.GetComponent<PetCompanionTutorialUI>();
            tutorial.ShowCustom(currencyText.font, "상점 튜토리얼", new[] { "1. 엽전으로 아이템 구매", "2. 아이템 판매" },
                new[] { "구매 탭에서 아이템에 표시된 가격만큼 엽전을 지불하면 구매할 수 있습니다.\n원하는 아이템을 더블클릭해 구매하세요.\n보유 엽전과 인벤토리 공간을 확인해 주세요.",
                    "판매 탭에서 보유 아이템을 클릭하면 판매하고 엽전을 받을 수 있습니다.\n판매불가로 표시된 아이템은 팔 수 없어요.\n안내를 마치면 구매와 판매를 이용해 보세요." },
                new[] { (RectTransform)buyTab.transform, (RectTransform)sellTab.transform },
                step => SetSelling(step == 1), CompleteTutorial, true, false);
        }
        private void CompleteTutorial()
        {
            tutorialCompletedClasses.Add(CharacterSelectionState.SelectedClass);
            EndTutorial();
        }
        private void EndTutorial()
        {
            if (tutorialCanvas == null) return;
            if (tutorial != null) tutorial.Hide();
            popupPanel.position = tutorialPanelPosition; popupPanel.localScale = tutorialPanelScale;
            SetSelling(false);
            if (tutorialInput != null) tutorialInput.enabled = inputWasEnabled;
            inventory?.GetComponent<PlayerMovement2D>()?.ClearInput();
            GameTimeController.SetPaused(this, false);
            Destroy(tutorialCanvas); tutorialCanvas = null; tutorial = null;
        }

        private void SetSelling(bool value)
        {
            selling = value;
            messageText.text = string.Empty;
            Refresh();
        }

        private void Refresh()
        {
            HideItemTooltip();
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

        private void OnDisable() => CloseInstant();

        private void OnDestroy()
        {
            popupSequence?.Kill();
            if (inventory != null && inventorySubscribed)
                inventory.InventoryChanged -= Refresh;
            if (currency != null && currencySubscribed)
                currency.CurrencyChanged -= OnCurrencyChanged;
            if (Instance == this)
                Instance = null;
        }
    }
}
