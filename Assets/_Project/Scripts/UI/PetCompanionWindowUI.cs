using System.Collections.Generic;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed partial class PetCompanionWindowUI : MonoBehaviour
    {
        private static readonly HashSet<CharacterClassId> guideCompletedThisSession = new HashSet<CharacterClassId>();
        private PetCompanionTutorialUI guide;
        private static readonly HashSet<CharacterClassId> tutorialCompletedThisSession = new HashSet<CharacterClassId>();
        [SerializeField] private DialogueSequence tutorialDialogue;
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Image goblinPreview;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private float idleFrameRate = 5f;
        [SerializeField] private Button closeButton, confirmButton, equipButton;
        [SerializeField] private TMP_Text soulCountText, hintText;
        [SerializeField] private TMP_Text soulLabelText, levelText, statValuesText, companionStatusText, skillsStatusText, barLabelText;
        [SerializeField] private Image soulIcon;
        [SerializeField] private ItemDefinition soulFragment;
        [SerializeField] private PetCompanionCatalog companionCatalog;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private GameObject companionRowTemplate, placeholderRow;
        [SerializeField] private Sprite selectedListFrame, normalListFrame;
        [SerializeField] private TMP_Text petNameText, verticalNameText, descriptionText;
        [SerializeField] private int selectedCompanionIndex;
        [SerializeField] private List<CompanionRowBinding> companionRows = new List<CompanionRowBinding>();
        [System.Serializable]
        private sealed class CompanionRowBinding
        {
            public PetCompanionActor prefab;
            public Image frame;
            public Image portrait;
            public TMP_Text status;
        }
        private PetCompanionActor SelectedPrefab => selectedCompanionIndex >= 0 && companionRows.Count > selectedCompanionIndex
            ? companionRows[selectedCompanionIndex].prefab : null;
        [SerializeField] private List<CompanionClassProgress> progressByClass = new List<CompanionClassProgress>();

        [System.Serializable]
        private sealed class CompanionClassProgress
        {
            public CharacterClassId characterClass;
            public string companionId = "goblin";
            public PetCompanionProgress progress = new PetCompanionProgress();
        }

        private PetCompanionProgress CurrentProgress => GetProgress(SelectedPrefab);

        private PetCompanionActor EquippedPrefab
        {
            get
            {
                foreach (var row in companionRows)
                    if (GetProgress(row.prefab).IsEquipped) return row.prefab;
                return null;
            }
        }

        private PetCompanionProgress GetProgress(PetCompanionActor prefab)
        {
            string identifier = prefab != null ? prefab.CompanionId : "goblin";
            var characterClass = CharacterSelectionState.SelectedClass;
            var entry = progressByClass.Find(p => p.characterClass == characterClass && p.companionId == identifier);
            if (entry == null)
            {
                entry = new CompanionClassProgress { characterClass = characterClass, companionId = identifier };
                progressByClass.Add(entry);
            }
            return entry.progress;
        }

        [SerializeField] private PetCompanionActor actor;
        private CharacterClassId actorClass;
        [SerializeField] private PetCompanionSkillSlotUI skillSlot;
        private Canvas dragQuickCanvas;
        private bool quickCanvasEnabled;
        private int quickCanvasSorting;
        private CanvasGroup dragWindowGroup;

        private void RefreshCompanionSkills(PlayerEntity player)
        {
            var system = player != null ? player.GetComponent<PlayerSkillSystem>() : null;
            if (system != null)
                foreach (var row in companionRows)
                    system.SetCompanionSkillUnlocked(row.prefab.ActiveSkill, GetProgress(row.prefab).IsSummoned);
            if (skillSlot == null)
            {
                foreach (var image in GetComponentsInChildren<Image>(true))
                    if (image.name == "LockedSkill0")
                    {
                        skillSlot = image.GetComponent<PetCompanionSkillSlotUI>();
                        if (skillSlot == null) skillSlot = image.gameObject.AddComponent<PetCompanionSkillSlotUI>();
                        break;
                    }
            }
            skillSlot?.Configure(this, system, SelectedPrefab != null ? SelectedPrefab.ActiveSkill : null, CurrentProgress.IsSummoned);
        }
        public void ShowSkillDescription(SkillDefinition skill, bool acquired)
        {
            if (hintText != null && skill != null) hintText.text = skill.DisplayName + " · " + skill.ShortDescription
                + (acquired ? " · 드래그하여 퀵슬롯에 등록" : " · 소환 시 해금");
        }
        public void RestoreWindowHint() { if (hintText != null) hintText.text = "P 키로 열기 · ESC 키로 닫기"; }
        public void SetSkillDragActive(bool active)
        {
            if (active && dragQuickCanvas == null && QuickSlotBarUI.Instance != null)
            {
                dragQuickCanvas = QuickSlotBarUI.Instance.GetComponentInParent<Canvas>().rootCanvas;
                quickCanvasEnabled = dragQuickCanvas.enabled; quickCanvasSorting = dragQuickCanvas.sortingOrder;
                dragQuickCanvas.enabled = true; dragQuickCanvas.sortingOrder = 6000;
                dragWindowGroup = windowRoot.GetComponent<CanvasGroup>();
                if (dragWindowGroup == null) dragWindowGroup = windowRoot.AddComponent<CanvasGroup>();
                dragWindowGroup.alpha = .45f; dragWindowGroup.blocksRaycasts = false;
                if (hintText != null) hintText.text = "스킬 아이콘을 원하는 퀵슬롯에 놓아 주세요.";
            }
            else if (!active && dragQuickCanvas != null)
            {
                dragQuickCanvas.enabled = quickCanvasEnabled; dragQuickCanvas.sortingOrder = quickCanvasSorting;
                dragQuickCanvas = null;
                if (dragWindowGroup != null) { dragWindowGroup.alpha = 1f; dragWindowGroup.blocksRaycasts = true; }
                RestoreWindowHint();
            }
        }
        private float nextPlayerSearch;

        private DialogueManager boundDialogue;
        private PlayerInventory inventory;
        private bool dialogueStartedInBrokenSeal;
        private float previewTime;
        private readonly List<Canvas> hiddenCanvases = new List<Canvas>();

        private static PetCompanionWindowUI instance;
        public static PetCompanionWindowUI Instance
        {
            get
            {
                // Editor recompilation resets statics while persistent UI can survive.
                if (instance == null) instance = FindFirstObjectByType<PetCompanionWindowUI>();
                return instance;
            }
        }
        public bool IsUnlocked => tutorialCompletedThisSession.Contains(CharacterSelectionState.SelectedClass);
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            // Also runs when Enter Play Mode has domain reload disabled.
            tutorialCompletedThisSession.Clear();
            guideCompletedThisSession.Clear();
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var prefab = Resources.Load<GameObject>("PetCompanionWindow");
            if (prefab != null) Instantiate(prefab);
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            windowRoot.SetActive(false);
            if (companionCatalog == null) companionCatalog = Resources.Load<PetCompanionCatalog>("PetCompanionCatalog");
            BuildCompanionList();
            closeButton.onClick.AddListener(Close);
            confirmButton.onClick.AddListener(SummonOrGrow);
            if (equipButton != null) equipButton.onClick.AddListener(ToggleEquipment);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void BuildCompanionList()
        {
            if (companionCatalog == null || listContent == null || companionRowTemplate == null) return;
            var seen = new HashSet<string>();
            foreach (var prefab in companionCatalog.CompanionPrefabs)
            {
                if (prefab == null || string.IsNullOrWhiteSpace(prefab.CompanionId) || !seen.Add(prefab.CompanionId)) continue;
                int index = companionRows.Count;
                var row = index == 0 ? companionRowTemplate : Instantiate(companionRowTemplate, listContent);
                row.name = "CompanionRow" + index;
                row.SetActive(true);
                var rect = (RectTransform)row.transform;
                rect.anchoredPosition = new Vector2(0f, -index * 92f);
                row.transform.Find("Name").GetComponent<TMP_Text>().text = prefab.DisplayName;
                var portrait = row.transform.Find("Goblin");
                if (portrait != null && prefab.PreviewFrames != null && prefab.PreviewFrames.Length > 0)
                    portrait.GetComponent<Image>().sprite = prefab.PreviewFrames[0];
                var button = row.GetComponent<Button>();
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() => SelectCompanion(index));
                companionRows.Add(new CompanionRowBinding { prefab = prefab, frame = row.GetComponent<Image>(),
                    status = row.transform.Find("Status").GetComponent<TMP_Text>(),
                    portrait = portrait != null ? portrait.GetComponent<Image>() : null });
            }
            if (companionRows.Count == 0) companionRowTemplate.SetActive(false);
            int count = companionRows.Count;
            if (placeholderRow != null)
            {
                placeholderRow.SetActive(companionCatalog.ShowPlaceholder);
                ((RectTransform)placeholderRow.transform).anchoredPosition = new Vector2(0f, -count * 92f);
                placeholderRow.transform.Find("Name").GetComponent<TMP_Text>().text = "???";
                placeholderRow.transform.Find("Status").GetComponent<TMP_Text>().text = "확인용";
                if (companionCatalog.ShowPlaceholder) count++;
            }
            float viewportHeight = listContent.parent is RectTransform viewport ? viewport.rect.height : 602f;
            listContent.sizeDelta = new Vector2(listContent.sizeDelta.x, Mathf.Max(viewportHeight, count * 92f));
            if (companionRows.Count > 0) SelectCompanion(0);
        }

        private void SelectCompanion(int index)
        {
            if (index < 0 || index >= companionRows.Count) return;
            selectedCompanionIndex = index;
            var prefab = SelectedPrefab;
            idleFrames = prefab.PreviewFrames;
            idleFrameRate = prefab.PreviewFrameRate;
            if (petNameText != null) petNameText.text = prefab.DisplayName;
            if (verticalNameText != null) verticalNameText.text = string.Join("\n", prefab.DisplayName.ToCharArray());
            if (descriptionText != null) descriptionText.text = prefab.Description;
            ReplayIdle();
            RefreshSoulCount();
            nextPlayerSearch = 0f;
        }

        private void Start() => BindDialogue();

        private void BindDialogue()
        {
            if (boundDialogue == DialogueManager.Instance) return;
            UnbindDialogue();
            boundDialogue = DialogueManager.Instance;
            if (boundDialogue == null) return;
            boundDialogue.DialogueStarted += OnDialogueStarted;
            boundDialogue.DialogueCompleted += OnDialogueCompleted;
        }

        private void OnDialogueStarted()
        {
            dialogueStartedInBrokenSeal = SceneManager.GetActiveScene().name == "BrokenSeal";
            Close();
        }

        private void OnDialogueCompleted(DialogueSequence completed)
        {
            if (!dialogueStartedInBrokenSeal || completed != tutorialDialogue
                || completed == null || SceneManager.GetActiveScene().name != "BrokenSeal") return;
            dialogueStartedInBrokenSeal = false;
            tutorialCompletedThisSession.Add(CharacterSelectionState.SelectedClass);
            // DialogueCompleted is raised after the dialogue has released input and pause.
            Open();
        }

        private void Update()
        {
            if (EasternFantasy.Saving.SaveGameService.IsRestoring) return;
            BindDialogue();
            EnsureCompanionActor();
            // Every entry point, including an accidentally enabled root, respects the gate.
            if (IsOpen && !IsUnlocked) Close();
            if (IsOpen)
            {
                Fit();
                if (idleFrames != null && idleFrames.Length > 0)
                {
                    previewTime += Time.unscaledDeltaTime;
                    goblinPreview.sprite = idleFrames[(int)(previewTime * idleFrameRate) % idleFrames.Length];
                }
            }
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (IsOpen && keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (IsOpen && guide != null && guide.IsShowing)
            {
                if (keyboard.spaceKey.wasPressedThisFrame) guide.Advance();
                return;
            }
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && (selected.GetComponent<TMP_InputField>() != null
                || selected.GetComponent<InputField>() != null)) return;
            if (keyboard.pKey.wasPressedThisFrame) Toggle();
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public bool Open()
        {
            if (!IsUnlocked || windowRoot == null || MapTutorialUI.IsRunning || StatsSkillTutorialUI.IsRunning || InventoryEquipmentTutorialUI.IsRunning || DungeonWindowUI.IsTutorialRunning || EquipmentUpgradeWindowUI.IsTutorialRunning || ShopWindowUI.IsTutorialRunning) return false;
            if (IsOpen) return true;
            string scene = SceneManager.GetActiveScene().name;
            if (scene == "MainScreen" || scene == "ChooseCharactor") return false;
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return false;
            if (EquipmentUpgradeWindowUI.Instance != null && EquipmentUpgradeWindowUI.Instance.IsOpen) return false;
            var player = FindFirstObjectByType<PlayerEntity>();
            if (player == null || player.IsDead) return false;
            InventoryWindowUI.Instance?.SetOpen(false);
            EquipmentWindowUI.Instance?.CloseFromInventory();
            PlayerStatsWindowUI.Instance?.SetOpen(false);
            SkillWindowUI.Instance?.SetOpen(false);
            ShopWindowUI.Instance?.Close();
            ScrollCodexWindowUI.Instance?.Close();
            HideGameplayCanvases();
            BindInventory(player.GetComponent<PlayerInventory>());
            windowRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
            Fit();
            ReplayIdle();
            GameTimeController.SetPaused(this, true);
            RefreshSoulCount();
            BeginFirstOpenGuide();
            return true;
        }

        private void BeginFirstOpenGuide()
        {
            var characterClass = CharacterSelectionState.SelectedClass;
            if (guideCompletedThisSession.Contains(characterClass)) return;
            if (guide == null)
            {
                var overlay = new GameObject("CompanionTutorial", typeof(RectTransform), typeof(PetCompanionTutorialUI));
                overlay.transform.SetParent(windowRoot.transform, false);
                guide = overlay.GetComponent<PetCompanionTutorialUI>();
            }
            guide.Show(hintText != null ? hintText.font : TMP_Settings.defaultFontAsset,
                new[] { (RectTransform)null, companionRowTemplate != null ? (RectTransform)companionRowTemplate.transform : listContent,
                    confirmButton != null ? (RectTransform)confirmButton.transform : null,
                    equipButton != null ? (RectTransform)equipButton.transform : null,
                    confirmButton != null ? (RectTransform)confirmButton.transform : null,
                    skillSlot != null ? (RectTransform)skillSlot.transform : null },
                () => guideCompletedThisSession.Add(characterClass));
        }

        public void Close()
        {
            if (guide != null) guide.Hide();
            SetSkillDragActive(false);
            if (windowRoot != null) windowRoot.SetActive(false);
            foreach (Canvas canvas in hiddenCanvases)
                if (canvas != null) canvas.enabled = true;
            hiddenCanvases.Clear();
            GameTimeController.SetPaused(this, false);
            BindInventory(null);
        }

        private void HideGameplayCanvases()
        {
            // Keep HUD and other canvases from covering this modal window.
            // Restore exactly the canvases that were enabled when it opened.
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.enabled || canvas.transform.IsChildOf(transform)) continue;
                hiddenCanvases.Add(canvas);
                canvas.enabled = false;
            }
        }

        public void ShowLockedCompanion()
        {
            if (hintText != null) hintText.text = "아직 등록되지 않은 동행요괴입니다.";
        }

        public void ReplayIdle()
        {
            previewTime = 0f;
            if (hintText != null) hintText.text = "P 키로 열기 · ESC 키로 닫기";
            if (idleFrames != null && idleFrames.Length > 0) goblinPreview.sprite = idleFrames[0];
        }

        private void SummonOrGrow()
        {
            if (!IsOpen || !IsUnlocked || inventory == null) return;
            var progress = CurrentProgress;
            bool wasSummoned = progress.IsSummoned;
            bool success = wasSummoned ? progress.TryGrow(inventory, soulFragment) : progress.TrySummon(inventory, SelectedPrefab != null ? SelectedPrefab.SummonItem : null);
            if (success && !wasSummoned) EquipSelected();
            RefreshSoulCount();
            if (hintText != null) hintText.text = success
                ? (wasSummoned ? SelectedPrefab.DisplayName + " 성장 완료. 공격력 +1" : SelectedPrefab.DisplayName + " 소환 완료. 창을 닫으면 동행을 시작합니다.")
                : (wasSummoned ? "성장에 필요한 영혼 조각이 부족합니다." : "도깨비의 영혼 1개가 필요합니다.");
            if (success) { nextPlayerSearch = 0f; EnsureCompanionActor(); }
        }

        private bool EquipSelected()
        {
            if (SelectedPrefab == null || !CurrentProgress.TryEquip()) return false;
            var progress = CurrentProgress;
            foreach (var entry in progressByClass)
                if (entry.characterClass == CharacterSelectionState.SelectedClass && entry.progress != progress)
                    entry.progress.Unequip();
            return true;
        }

        private void ToggleEquipment()
        {
            if (!IsOpen || !IsUnlocked || SelectedPrefab == null || !CurrentProgress.IsSummoned) return;
            bool unequip = CurrentProgress.IsEquipped;
            if (unequip) CurrentProgress.Unequip();
            else if (!EquipSelected()) return;
            nextPlayerSearch = 0f;
            EnsureCompanionActor();
            RefreshSoulCount();
            if (hintText != null) hintText.text = SelectedPrefab.DisplayName + (unequip ? " 장착 해제. 동행을 중지합니다." : " 장착 완료. 창을 닫으면 동행합니다.");
        }

        private void DestroyActor()
        {
            if (actor == null) return;
            // Stop rendering and attack subscriptions immediately, including while the window pauses time.
            actor.gameObject.SetActive(false);
            Destroy(actor.gameObject);
            actor = null;
        }

        private void EnsureCompanionActor()
        {
            // Browsing the list never changes the equipped companion.
            var prefab = EquippedPrefab;
            var characterClass = CharacterSelectionState.SelectedClass;
            string scene = SceneManager.GetActiveScene().name;
            if (actor != null && (prefab == null || actorClass != characterClass || actor.CompanionId != prefab.CompanionId
                || actor.gameObject.scene != SceneManager.GetActiveScene()
                || scene == "MainScreen" || scene == "ChooseCharactor")) DestroyActor();
            if (prefab == null || scene == "MainScreen" || scene == "ChooseCharactor") return;
            var currentPlayer = FindFirstObjectByType<PlayerEntity>();
            RefreshCompanionSkills(currentPlayer);
            var progress = GetProgress(prefab);
            if (actor != null) { actor.BindProgress(progress); return; }
            if (Time.unscaledTime < nextPlayerSearch) return;
            nextPlayerSearch = Time.unscaledTime + .5f;
            var player = FindFirstObjectByType<PlayerEntity>();
            if (player == null || player.IsDead) return;
            actor = Instantiate(prefab);
            // Keep the companion in the map, rather than the player's DontDestroyOnLoad scene.
            SceneManager.MoveGameObjectToScene(actor.gameObject, SceneManager.GetActiveScene());
            actor.Initialize(player, progress);
            actorClass = characterClass;
        }

        private void BindInventory(PlayerInventory current)
        {
            if (inventory == current) return;
            if (inventory != null) inventory.InventoryChanged -= RefreshSoulCount;
            inventory = current;
            if (inventory != null) inventory.InventoryChanged += RefreshSoulCount;
        }

        private void RefreshSoulCount()
        {
            var progress = CurrentProgress;
            var material = progress.IsSummoned ? soulFragment : (SelectedPrefab != null ? SelectedPrefab.SummonItem : null);
            int cost = progress.IsSummoned ? progress.GrowthCost : 1;
            int quantity = inventory != null && material != null ? inventory.GetMaterialQuantity(material) : 0;
            if (soulCountText != null) soulCountText.text = quantity + " / " + cost + " 개";
            if (soulLabelText != null) soulLabelText.text = progress.IsSummoned ? "영혼 조각 · 성장 재료" : (material != null ? material.DisplayName + " · 소환 재료" : "소환 재료 없음");
            if (soulIcon != null) soulIcon.sprite = material != null ? material.Icon : null;
            if (levelText != null) levelText.text = progress.IsSummoned ? "Lv. " + progress.Level : "미소환";
            if (statValuesText != null) statValuesText.text = progress.Attack + "\n\n일반";
            for (int i = 0; i < companionRows.Count; i++)
            {
                var row = companionRows[i];
                var state = GetProgress(row.prefab);
                state.TryUnlock(inventory, row.prefab.SummonItem);
                if (row.portrait != null) row.portrait.color = state.IsUnlocked ? Color.white : new Color(.3f, .3f, .3f, .65f);
                row.frame.color = state.IsUnlocked ? Color.white : new Color(.6f, .6f, .6f, 1f);
                row.status.text = state.IsSummoned ? "Lv. " + state.Level + (state.IsEquipped ? "   동행 중" : "   장착 해제") : state.IsUnlocked ? "잠금 해제 · 소환 가능" : "잠김 · 전용 영혼 필요";
                if (selectedListFrame != null && normalListFrame != null)
                    row.frame.sprite = i == selectedCompanionIndex ? selectedListFrame : normalListFrame;
            }
            RefreshCompanionSkills(inventory != null ? inventory.GetComponent<PlayerEntity>() : FindFirstObjectByType<PlayerEntity>());
            if (skillsStatusText != null) skillsStatusText.text = progress.IsSummoned ? "퀵슬롯에 드래그" : "소환 시 해금";
            if (barLabelText != null) barLabelText.text = progress.IsSummoned ? "영혼 조각으로 성장" : (material != null ? material.DisplayName + " 1개로 소환" : "동행요괴 프리팹을 등록해 주세요.");
            if (equipButton != null)
            {
                equipButton.interactable = SelectedPrefab != null && progress.IsSummoned;
                var label = equipButton.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = progress.IsEquipped ? "장착 해제" : "장착";
            }
            if (confirmButton != null)
            {
                confirmButton.interactable = material != null && inventory != null && quantity >= cost;
                var label = confirmButton.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = progress.IsSummoned ? "성장" : "소환";
            }
        }

        private void Fit()
        {
            Rect available = ((RectTransform)windowRoot.transform).rect;
            float scale = Mathf.Min((available.width - 48f) / 1448f, (available.height - 36f) / 1000f);
            panel.localScale = Vector3.one * Mathf.Max(.05f, scale);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Close();
            DestroyActor();
            nextPlayerSearch = 0f;
            dialogueStartedInBrokenSeal = false;
            BindDialogue();
        }

        private void UnbindDialogue()
        {
            if (boundDialogue != null)
            {
                boundDialogue.DialogueStarted -= OnDialogueStarted;
                boundDialogue.DialogueCompleted -= OnDialogueCompleted;
            }
            boundDialogue = null;
        }

        private void OnDisable() { Close(); UnbindDialogue(); }
        private void OnDestroy()
        {
            DestroyActor();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnbindDialogue();
            BindInventory(null);
            GameTimeController.SetPaused(this, false);
            if (instance == this) instance = null;
        }
    }
}
