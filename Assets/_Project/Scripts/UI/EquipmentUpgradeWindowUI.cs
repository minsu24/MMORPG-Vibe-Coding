using System;
using DG.Tweening;
using System.Collections.Generic;
using EasternFantasy.Dialogue;
using EasternFantasy.EquipmentUpgrade;
using EasternFantasy.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    public sealed class EquipmentUpgradeWindowUI : MonoBehaviour
    {
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private EquipmentUpgradeRecipe[] recipes;
        [SerializeField] private Image targetIcon;
        [SerializeField] private TMP_Text targetText;
        [SerializeField] private TMP_Text nextGradeText;
        [SerializeField] private Image[] materialFrames;
        [SerializeField] private Image[] materialIcons;
        [SerializeField] private TMP_Text[] materialTexts;
        [SerializeField] private Sprite emptyMaterialFrame;
        [SerializeField] private Sprite filledMaterialFrame;
        [SerializeField] private Sprite readyMaterialFrame;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text pickerTitle;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button equipmentTab;
        [SerializeField] private Button materialTab;
        [SerializeField] private Sprite selectedTabSprite;
        [SerializeField] private Sprite unselectedTabSprite;
        [SerializeField] private UpgradeInventorySlotUI[] inventorySlots;
        [SerializeField] private UnityEvent onConfirmClicked = new UnityEvent();
        [SerializeField] private UnityEvent<ItemDefinition> onUpgradeSucceeded = new UnityEvent<ItemDefinition>();
        [Header("DOTween popup")]
        [SerializeField, Min(0.01f)] private float openDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float closeDuration = 0.18f;
        [SerializeField, Range(0.5f, 1f)] private float popupStartScale = 0.9f;
        private CanvasGroup windowGroup;
        private RectTransform[] popupPanels;
        private Vector3[] panelScales;
        private Sequence popupSequence;
        private bool closing;
        private PlayerInventory inventory;
        private PlayerCurrency currency;
        private ItemDefinition target;
        private bool pickingEquipment = true;
        private bool initialized;
        private bool committing;
        private readonly HashSet<ItemDefinition> insertedMaterials = new HashSet<ItemDefinition>();
        private readonly List<KeyValuePair<ItemDefinition, int>> requirements = new List<KeyValuePair<ItemDefinition, int>>();
        private EquipmentUpgradeRecipe currentRecipe;
        public static EquipmentUpgradeWindowUI Instance { get; private set; }
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;
        public bool CanConfirm => confirmButton != null && confirmButton.interactable;
        public ItemDefinition Target => target;
        public event Action<ItemDefinition> Upgraded;

        private void Awake()
        {
            Instance = this;
            PreparePopup();
            CloseInstant();
        }
        private void Start() => Initialize();
        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            confirmButton.onClick.AddListener(Confirm);
            closeButton.onClick.AddListener(Close);
            equipmentTab.onClick.AddListener(() => SetPicker(true));
            materialTab.onClick.AddListener(() => SetPicker(false));
            foreach (var slot in inventorySlots) slot.Initialize(this);
            foreach (var slot in GetComponentsInChildren<UpgradeDropSlotUI>(true)) slot.Initialize(this);
            confirmButton.GetComponent<UpgradeConfirmFeedback>().Initialize(this);
        }
        public void BindPlayer(PlayerInventory owner, PlayerCurrency money)
        {
            if (inventory != null) inventory.InventoryChanged -= Refresh;
            if (currency != null) currency.CurrencyChanged -= OnCurrencyChanged;
            inventory = owner; currency = money;
            if (inventory != null) inventory.InventoryChanged += Refresh;
            if (currency != null) currency.CurrencyChanged += OnCurrencyChanged;
        }
        public void Open()
        {
            Initialize();
            BindPlayer(FindFirstObjectByType<PlayerInventory>(), FindFirstObjectByType<PlayerCurrency>());
            if (inventory == null || currency == null) return;
            InventoryWindowUI.Instance?.SetOpen(false);
            EquipmentWindowUI.Instance?.CloseFromInventory();
            ShopWindowUI.Instance?.Close();
            PlayerStatsWindowUI.Instance?.SetOpen(false);
            SkillWindowUI.Instance?.SetOpen(false);
            target = null; insertedMaterials.Clear(); pickingEquipment = true;
            messageText.text = "장비를 선택한 후 필요한 재료를 넣어 주세요.";
            AnimateOpen();
            GameTimeController.SetPaused(this, true);
            Refresh();
        }
        private void PreparePopup()
        {
            if (windowGroup != null || windowRoot == null) return;
            windowGroup = windowRoot.GetComponent<CanvasGroup>();
            if (windowGroup == null) windowGroup = windowRoot.AddComponent<CanvasGroup>();
            var panels = new List<RectTransform>();
            foreach (Transform child in windowRoot.transform)
                if (child.name != "Dim" && child is RectTransform rect) panels.Add(rect);
            popupPanels = panels.ToArray();
            panelScales = new Vector3[popupPanels.Length];
            for (int i = 0; i < popupPanels.Length; i++) panelScales[i] = popupPanels[i].localScale;
        }
        private void AnimateOpen()
        {
            PreparePopup();
            popupSequence?.Kill();
            closing = false;
            if (!windowRoot.activeSelf)
            {
                windowGroup.alpha = 0f;
                for (int i = 0; i < popupPanels.Length; i++)
                    popupPanels[i].localScale = panelScales[i] * popupStartScale;
            }
            windowRoot.SetActive(true);
            windowGroup.interactable = false;
            windowGroup.blocksRaycasts = true;
            popupSequence = DOTween.Sequence().SetUpdate(true);
            popupSequence.Append(windowGroup.DOFade(1f, Mathf.Max(0.01f, openDuration)).SetEase(Ease.OutQuad));
            for (int i = 0; i < popupPanels.Length; i++)
                popupSequence.Join(popupPanels[i].DOScale(panelScales[i], Mathf.Max(0.01f, openDuration)).SetEase(Ease.OutBack));
            popupSequence.OnComplete(() => { popupSequence = null; windowGroup.interactable = true; });
        }
        public void Close()
        {
            if (closing) return;
            if (!IsOpen || !isActiveAndEnabled) { CloseInstant(); return; }
            PreparePopup();
            popupSequence?.Kill();
            closing = true;
            windowGroup.interactable = false;
            windowGroup.blocksRaycasts = true;
            popupSequence = DOTween.Sequence().SetUpdate(true);
            popupSequence.Append(windowGroup.DOFade(0f, Mathf.Max(0.01f, closeDuration)).SetEase(Ease.InQuad));
            for (int i = 0; i < popupPanels.Length; i++)
                popupSequence.Join(popupPanels[i].DOScale(panelScales[i] * popupStartScale,
                    Mathf.Max(0.01f, closeDuration)).SetEase(Ease.InQuad));
            popupSequence.OnComplete(() => { popupSequence = null; CloseInstant(); });
        }
        private void CloseInstant()
        {
            popupSequence?.Kill();
            popupSequence = null;
            closing = false;
            if (windowGroup != null)
            {
                windowGroup.alpha = 0f;
                windowGroup.interactable = false;
                windowGroup.blocksRaycasts = false;
            }
            if (popupPanels != null)
                for (int i = 0; i < popupPanels.Length; i++)
                    if (popupPanels[i] != null) popupPanels[i].localScale = panelScales[i];
            if (windowRoot != null) windowRoot.SetActive(false);
            GameTimeController.SetPaused(this, false);
        }
        private void Update()
        {
            if (!IsOpen) return;
            if (inventory == null || currency == null) { Close(); return; }
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) Close();
        }
        private void SetPicker(bool equipment) { pickingEquipment = equipment; Refresh(); }
        public void SelectFromInventory(ItemDefinition item)
        {
            if (item == null) return;
            if (item.Category == ItemCategory.Equipment && item.EquipmentSlot != EquipmentSlot.None) DropItem(item, -1);
            else
            {
                int index = requirements.FindIndex(pair => pair.Key == item);
                if (index < 0) messageText.text = "이 승급에 사용하는 재료가 아닙니다.";
                else DropItem(item, index);
            }
        }
        public void DropItem(ItemDefinition item, int materialIndex)
        {
            if (inventory == null || item == null || inventory.GetQuantity(item) <= 0) return;
            if (materialIndex < 0)
            {
                if (item.Category != ItemCategory.Equipment || item.EquipmentSlot == EquipmentSlot.None) { messageText.text = "장비만 넣을 수 있습니다."; return; }
                target = item; insertedMaterials.Clear();
                messageText.text = "";
                Refresh();
                return;
            }
            if (materialIndex >= requirements.Count || requirements[materialIndex].Key != item)
            {
                messageText.text = "해당 칸에 맞는 재료를 넣어 주세요."; return;
            }
            insertedMaterials.Add(item);
            messageText.text = "";
            Refresh();
        }
        public void ClearSlot(int index)
        {
            if (index < 0) { target = null; insertedMaterials.Clear(); }
            else if (index < requirements.Count) insertedMaterials.Remove(requirements[index].Key);
            messageText.text = ""; Refresh();
        }
        public void Confirm()
        {
            if (committing || !IsOpen || closing || (windowGroup != null && !windowGroup.interactable)) return;
            confirmButton.GetComponent<UpgradeConfirmFeedback>()?.PlayFeedback(CanConfirm);
            committing = true;
            try
            {
                onConfirmClicked.Invoke();
                if (!IsOpen || closing || (windowGroup != null && !windowGroup.interactable) || inventory == null || currency == null) return;
                if (!inventory.CanUpgradeEquipment(target, currentRecipe, currency, out string error))
                { messageText.text = error; return; }
                if (!AllMaterialsInserted())
                { messageText.text = "필요한 재료를 모두 넣어 주세요."; return; }
                if (!inventory.TryUpgradeEquipment(target, currentRecipe, currency, out var result, out error))
                { messageText.text = error; return; }
                target = result;
                insertedMaterials.Clear();
                messageText.text = result.GradedDisplayName + " 승급 완료!";
                onUpgradeSucceeded.Invoke(result);
                Upgraded?.Invoke(result);
            }
            finally { committing = false; Refresh(); }
        }
        private bool AllMaterialsInserted()
        {
            if (requirements.Count == 0) return false;
            foreach (var pair in requirements)
                if (!insertedMaterials.Contains(pair.Key) || inventory.GetQuantity(pair.Key) < pair.Value) return false;
            return true;
        }
        public void Refresh()
        {
            if (!initialized || inventory == null || currency == null || committing) return;
            if (target != null && inventory.GetQuantity(target) == 0) { target = null; insertedMaterials.Clear(); }
            currentRecipe = null;
            if (recipes != null) foreach (var recipe in recipes)
                if (recipe != null && recipe.Matches(target)) { currentRecipe = recipe; break; }
            requirements.Clear();
            string configError = null;
            if (currentRecipe != null && currentRecipe.TryGetRequirements(out var needed, out configError))
                foreach (var pair in needed) requirements.Add(pair);
            targetIcon.sprite = target != null ? target.Icon : null;
            targetIcon.enabled = target != null;
            ItemGradeVisual.CenterInUpgradeFrame(targetIcon, targetIcon.transform.parent.GetComponent<Image>());
            targetText.text = target != null ? target.GradedDisplayName : "승급 대상 장비";
            targetText.color = target != null ? target.GradeColor : Color.white;
            nextGradeText.text = target == null ? "장비를 클릭하거나 상단 칸으로 끌어 놓으세요." :
                target.Grade == ItemGrade.Mythic ? "최고 등급" :
                configError ?? (currentRecipe == null ? "승급 조건 없음" :
                    target.GradeName + " → " + ItemGradeUtility.GetName(currentRecipe.TargetGrade));
            for (int i = 0; i < materialFrames.Length; i++)
            {
                bool used = i < requirements.Count;
                bool filled = used && insertedMaterials.Contains(requirements[i].Key) && inventory.GetQuantity(requirements[i].Key) > 0;
                bool enough = filled && inventory.GetQuantity(requirements[i].Key) >= requirements[i].Value;
                materialFrames[i].sprite = enough ? readyMaterialFrame : filled ? filledMaterialFrame : emptyMaterialFrame;
                ItemGradeVisual.CenterInUpgradeFrame(materialIcons[i], materialFrames[i]);
                materialIcons[i].enabled = used;
                materialIcons[i].sprite = used ? requirements[i].Key.Icon : null;
                materialIcons[i].color = new Color(1, 1, 1, filled ? 1f : .2f);
                materialTexts[i].text = used ? requirements[i].Key.DisplayName + "\n" +
                    (filled ? inventory.GetQuantity(requirements[i].Key) : 0) + " / " + requirements[i].Value : "재료 없음";
                materialTexts[i].color = enough ? new Color(.7f, .95f, .7f) : Color.white;
            }
            bool configured = currentRecipe != null && currentRecipe.TryGetRequirements(out _, out _);
            costText.text = configured ? "필요 엽전  " + currentRecipe.CostYeopjeon + "     보유  " + currency.Yeopjeon : "필요 엽전  미설정     보유  " + currency.Yeopjeon;
            costText.color = configured && !currency.CanAfford(currentRecipe.CostYeopjeon) ? new Color(1f,.45f,.4f) : new Color(.96f,.84f,.58f);
            confirmButton.interactable = AllMaterialsInserted() && inventory.CanUpgradeEquipment(target, currentRecipe, currency, out _);
            ApplyPickerTab(equipmentTab, pickingEquipment);
            ApplyPickerTab(materialTab, !pickingEquipment);
            pickerTitle.text = pickingEquipment ? "보유 장비" : requirements.Count > 0 ? "승급 재료" : "보유 재료";
            int index = 0;
            foreach (var stack in inventory.Items)
            {
                if ((stack.Item.Category == ItemCategory.Equipment && stack.Item.EquipmentSlot != EquipmentSlot.None) != pickingEquipment) continue;
                if (!pickingEquipment && requirements.Count > 0 && !requirements.Exists(pair => pair.Key == stack.Item)) continue;
                if (index < inventorySlots.Length) inventorySlots[index++].Bind(stack.Item, stack.Quantity);
            }
            while (index < inventorySlots.Length) inventorySlots[index++].Bind(null, 0);
        }
        private void ApplyPickerTab(Button button, bool selected)
        {
            button.transition = Selectable.Transition.None;
            button.interactable = !selected;
            var image = button.targetGraphic as Image;
            if (image == null) return;
            image.overrideSprite = null;
            Sprite sprite = selected ? selectedTabSprite : unselectedTabSprite;
            if (sprite != null) image.sprite = sprite;
            image.color = Color.white;
            image.CrossFadeColor(Color.white, 0f, true, true);
        }

        private void OnCurrencyChanged(int amount) => Refresh();
        private void OnDisable() => CloseInstant();
        private void OnDestroy()
        {
            popupSequence?.Kill();
            BindPlayer(null, null);
            GameTimeController.SetPaused(this, false);
            if (Instance == this) Instance = null;
        }
    }
}