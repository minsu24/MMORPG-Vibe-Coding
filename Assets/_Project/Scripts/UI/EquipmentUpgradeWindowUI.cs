using System;
using DG.Tweening;
using System.Collections.Generic;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Player;
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
        private static readonly HashSet<CharacterClassId> tutorialCompletedClasses = new HashSet<CharacterClassId>();
        private PetCompanionTutorialUI tutorial;
        private GameObject tutorialCanvas;
        private PlayerInputReader tutorialInput;
        private bool tutorialInputWasEnabled;
        private Vector3[] tutorialPositions, tutorialScales;
        public static bool IsTutorialRunning => Instance != null && Instance.tutorial != null && Instance.tutorial.IsShowing;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTutorials() { tutorialCompletedClasses.Clear(); }
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
            if (StatsSkillTutorialUI.IsRunning || InventoryEquipmentTutorialUI.IsRunning || DungeonWindowUI.IsTutorialRunning || ShopWindowUI.IsTutorialRunning || IsTutorialRunning) return;
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
            popupSequence.OnComplete(() => { popupSequence = null; windowGroup.interactable = true; BeginTutorial(); });
        }
        public void Close()
        {
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
            for (int i = 0; i < popupPanels.Length; i++)
                popupSequence.Join(popupPanels[i].DOScale(panelScales[i] * popupStartScale,
                    Mathf.Max(0.01f, closeDuration)).SetEase(Ease.InQuad));
            popupSequence.OnComplete(() => { popupSequence = null; CloseInstant(); });
        }
        private void CloseInstant()
        {
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
            if (popupPanels != null)
                for (int i = 0; i < popupPanels.Length; i++)
                    if (popupPanels[i] != null) popupPanels[i].localScale = panelScales[i];
            if (windowRoot != null) windowRoot.SetActive(false);
            GameTimeController.SetPaused(this, false);
        }
        private void Update()
        {
            if (!IsOpen) return;
            if (IsTutorialRunning) { if (Keyboard.current?.spaceKey.wasPressedThisFrame == true) tutorial.Advance(); return; }
            if (inventory == null || currency == null) { Close(); return; }
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) Close();
        }
        private void BeginTutorial()
        {
            if (tutorialCompletedClasses.Contains(CharacterSelectionState.SelectedClass) || IsTutorialRunning) return;
            tutorialInput = inventory != null ? inventory.GetComponent<PlayerInputReader>() : null;
            if (tutorialInput != null) { tutorialInputWasEnabled = tutorialInput.enabled; tutorialInput.enabled = false; }
            inventory?.GetComponent<PlayerMovement2D>()?.ClearInput();
            tutorialCanvas = new GameObject("EquipmentUpgradeTutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = tutorialCanvas.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5000;
            var scaler = tutorialCanvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            Canvas.ForceUpdateCanvases();
            var canvasRect = (RectTransform)tutorialCanvas.transform;
            tutorialPositions = new Vector3[popupPanels.Length]; tutorialScales = new Vector3[popupPanels.Length];
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < popupPanels.Length; i++) {
                tutorialPositions[i] = popupPanels[i].position; tutorialScales[i] = popupPanels[i].localScale;
                var corners = new Vector3[4]; popupPanels[i].GetWorldCorners(corners);
                foreach (var corner in corners) { Vector2 point = canvasRect.InverseTransformPoint(corner); min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
            }
            float fit = Mathf.Min(1f, canvasRect.rect.width * .46f / Mathf.Max(1f, max.x-min.x), canvasRect.rect.height * .82f / Mathf.Max(1f, max.y-min.y));
            var oldCenter = canvasRect.TransformPoint((min+max)*.5f);
            var center = canvasRect.TransformPoint(canvasRect.rect.min + new Vector2(canvasRect.rect.width*.25f, canvasRect.rect.height*.53f));
            for (int i = 0; i < popupPanels.Length; i++) { popupPanels[i].position = center + (tutorialPositions[i]-oldCenter)*fit; popupPanels[i].localScale = tutorialScales[i]*fit; }
            // Preview owned equipment without moving or consuming any inventory item.
            foreach (var stack in inventory.Items) {
                if (stack.Item == null || stack.Item.Category != ItemCategory.Equipment) continue;
                foreach (var recipe in recipes) if (recipe != null && recipe.Matches(stack.Item)) { target = stack.Item; break; }
                if (target != null) break;
            }
            Refresh();
            var overlay = new GameObject("EquipmentUpgradeGuide", typeof(RectTransform), typeof(PetCompanionTutorialUI));
            overlay.transform.SetParent(tutorialCanvas.transform, false); tutorial = overlay.GetComponent<PetCompanionTutorialUI>();
            tutorial.ShowCustom(targetText.font, "장비 승급 튜토리얼",
                new[] { "1. 승급할 장비 넣기", "2. 장비마다 다른 승급 조건", "3. 요구 재료 넣기", "4. 엽전을 지불하고 승급", "5. 승급 결과와 재료 소비" },
                new[] {
                    "옆 인벤토리의 장비 탭에서 승급하고 싶은 장비를 찾아보세요.\n장비를 드래그 앤 드롭해 큰 장비 칸에 놓으면 됩니다.\n이 안내에서는 보유 장비를 예시로 보여 주며 실제 승급은 하지 않습니다.",
                    "장비의 등급과 종류에 따라 필요한 승급 재료가 달라집니다.\n장비를 넣으면 다음 등급과 요구 재료·수량이 표시됩니다.\n새 장비를 넣을 때마다 조건을 다시 확인하세요.",
                    "옆 인벤토리의 재료 탭을 선택하세요.\n요구되는 재료를 드래그 앤 드롭해 알맞은 재료 칸에 놓습니다.\n각 재료의 수량을 확인하고 필요한 재료를 모두 넣어 주세요.",
                    "필요 엽전과 보유 엽전을 확인한 뒤 승급 확인 버튼을 누르세요.\n재료를 모두 넣고 엽전이 충분해야 승급할 수 있습니다.\n표시된 비용이 0엽전이면 엽전을 사용하지 않습니다.",
                    "승급하면 기존 장비가 다음 등급 장비로 바뀌고,\n필요한 수량의 재료와 표시된 엽전 비용이 소모됩니다.\n칸에 넣는 것만으로는 소모되지 않으며, 칸을 클릭해 뺄 수 있어요.\n안내를 마치면 직접 승급할 장비를 골라 보세요." },
                new[] { (RectTransform)targetIcon.transform.parent, nextGradeText.rectTransform,
                    (RectTransform)materialTab.transform, (RectTransform)confirmButton.transform, (RectTransform)targetIcon.transform.parent },
                step => SetPicker(step == 0 || step == 4), CompleteTutorial, true, false);
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
            for (int i = 0; i < popupPanels.Length; i++) if (popupPanels[i] != null) { popupPanels[i].position = tutorialPositions[i]; popupPanels[i].localScale = tutorialScales[i]; }
            target = null; insertedMaterials.Clear(); SetPicker(true);
            messageText.text = "장비를 선택한 후 필요한 재료를 넣어 주세요.";
            if (tutorialInput != null) tutorialInput.enabled = tutorialInputWasEnabled;
            inventory?.GetComponent<PlayerMovement2D>()?.ClearInput();
            Destroy(tutorialCanvas); tutorialCanvas = null; tutorial = null;
        }

        private void SetPicker(bool equipment) { pickingEquipment = equipment; Refresh(); }
        public void SelectFromInventory(ItemDefinition item)
        {
            if (IsTutorialRunning) return;
            if (item == null) return;
            if (item.Category == ItemCategory.Equipment && item.EquipmentSlot != EquipmentSlot.None) DropItem(item, -1);
            else
            {
                int index = requirements.FindIndex(pair => (pair.Key == item || pair.Key.MatchesMaterial(item)));
                if (index < 0) messageText.text = "이 승급에 사용하는 재료가 아닙니다.";
                else DropItem(item, index);
            }
        }
        public void DropItem(ItemDefinition item, int materialIndex)
        {
            if (IsTutorialRunning) return;
            if (inventory == null || item == null || inventory.GetQuantity(item) <= 0) return;
            if (materialIndex < 0)
            {
                if (item.Category != ItemCategory.Equipment || item.EquipmentSlot == EquipmentSlot.None) { messageText.text = "장비만 넣을 수 있습니다."; return; }
                target = item; insertedMaterials.Clear();
                messageText.text = "";
                Refresh();
                return;
            }
            if (materialIndex >= requirements.Count || !(requirements[materialIndex].Key == item || requirements[materialIndex].Key.MatchesMaterial(item)))
            {
                messageText.text = "해당 칸에 맞는 재료를 넣어 주세요."; return;
            }
            insertedMaterials.Add(requirements[materialIndex].Key);
            messageText.text = "";
            Refresh();
        }
        public void ClearSlot(int index)
        {
            if (IsTutorialRunning) return;
            if (index < 0) { target = null; insertedMaterials.Clear(); }
            else if (index < requirements.Count) insertedMaterials.Remove(requirements[index].Key);
            messageText.text = ""; Refresh();
        }
        public void Confirm()
        {
            if (IsTutorialRunning) return;
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
                if (!insertedMaterials.Contains(pair.Key) || inventory.GetMaterialQuantity(pair.Key) < pair.Value) return false;
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
                bool filled = used && insertedMaterials.Contains(requirements[i].Key) && inventory.GetMaterialQuantity(requirements[i].Key) > 0;
                bool enough = filled && inventory.GetMaterialQuantity(requirements[i].Key) >= requirements[i].Value;
                materialFrames[i].sprite = enough ? readyMaterialFrame : filled ? filledMaterialFrame : emptyMaterialFrame;
                ItemGradeVisual.CenterInUpgradeFrame(materialIcons[i], materialFrames[i]);
                materialIcons[i].enabled = used;
                materialIcons[i].sprite = used ? requirements[i].Key.Icon : null;
                materialIcons[i].color = new Color(1, 1, 1, filled ? 1f : .2f);
                materialTexts[i].text = used ? requirements[i].Key.DisplayName + "\n" +
                    (filled ? inventory.GetMaterialQuantity(requirements[i].Key) : 0) + " / " + requirements[i].Value : "재료 없음";
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
                if (!pickingEquipment && requirements.Count > 0 && !requirements.Exists(pair => (pair.Key == stack.Item || pair.Key.MatchesMaterial(stack.Item)))) continue;
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