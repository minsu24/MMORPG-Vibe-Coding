using System.Collections.Generic;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dungeon;
using EasternFantasy.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed partial class DungeonWindowUI : MonoBehaviour
    {
        public DungeonDefinition[] dungeons;
        public GameObject windowRoot;
        public Image preview, dungeonEmblem;
        public TMP_Text dungeonName, description, levelText, timeText, monsterText, rewardText, message;
        public Image[] rewardIcons;
        public TMP_Text[] rewardAmounts;
        public Button enterButton, closeButton;
        public ScrollRect list;
        public DungeonListEntryUI entryTemplate;
        private readonly List<DungeonListEntryUI> entries = new List<DungeonListEntryUI>();
        private readonly List<DungeonDefinition> boundDungeons = new List<DungeonDefinition>();
        private PlayerMovement2D player;
        private bool loading;
        private static readonly HashSet<CharacterClassId> discoveredClasses = new HashSet<CharacterClassId>();
        public static bool IsUnlocked => discoveredClasses.Contains(CharacterSelectionState.SelectedClass);
        private static readonly HashSet<CharacterClassId> tutorialCompletedClasses = new HashSet<CharacterClassId>();
        private PetCompanionTutorialUI tutorial;
        private GameObject tutorialCanvas;
        private RectTransform tutorialPanel;
        private Vector3 tutorialPosition, tutorialScale;
        private PlayerInputReader tutorialInput;
        private bool tutorialInputWasEnabled;
        public static bool IsTutorialRunning => Instance != null && Instance.tutorial != null && Instance.tutorial.IsShowing;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTutorials() { tutorialCompletedClasses.Clear(); discoveredClasses.Clear(); Instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= EnsureWindow;
            SceneManager.sceneLoaded += EnsureWindow;
            EnsureWindow(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        private static void EnsureWindow(Scene scene, LoadSceneMode mode)
        {
            if (Instance != null || GameplayHudVisibility.IsMenuScene) return;
            var prefab = Resources.Load<GameObject>("DungeonWindow");
            if (prefab != null) Instantiate(prefab);
        }
        public static DungeonWindowUI Instance { get; private set; }
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;
        public DungeonDefinition Selected { get; private set; }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Initialize();
            DontDestroyOnLoad(gameObject);
            SceneManager.activeSceneChanged += SceneChanged;
            windowRoot.SetActive(false);
            entryTemplate.gameObject.SetActive(false);
        }
        private void Initialize()
        {
            Instance = this;

        }
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (!IsTutorialRunning && keyboard != null && keyboard.oKey.wasPressedThisFrame && !IsTyping())
            {
                if (IsOpen) Close();
                else if (IsUnlocked) OpenWindow(FindFirstObjectByType<PlayerMovement2D>());
            }
            if (!IsOpen) return;
            if (IsTutorialRunning)
            {
                if (player == null || (player.GetComponent<PlayerEntity>() is PlayerEntity actor && actor.IsDead)) { Close(); return; }
                if (Keyboard.current?.spaceKey.wasPressedThisFrame == true) tutorial.Advance();
                return;
            }
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true || player == null
                || (player.GetComponent<PlayerEntity>() is PlayerEntity entity && entity.IsDead)) Close();
        }
        public void Open(DungeonPortal source, PlayerMovement2D actor)
        {
            if (source == null) return;
            if (OpenWindow(actor)) discoveredClasses.Add(CharacterSelectionState.SelectedClass);
        }
        private static bool IsTyping()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null && (selected.GetComponent<TMP_InputField>() != null || selected.GetComponent<InputField>() != null);
        }
        private bool OpenWindow(PlayerMovement2D actor)
        {
            if (GameplayHudVisibility.IsMenuScene || PortalSceneTransition.IsTransitioning || EasternFantasy.Saving.SaveGameService.IsRestoring
                || DungeonRunController.Instance != null) return false;
            if (EasternFantasy.Dialogue.DialogueManager.Instance != null && EasternFantasy.Dialogue.DialogueManager.Instance.IsDialogueActive) return false;
            if (EquipmentUpgradeWindowUI.Instance != null && EquipmentUpgradeWindowUI.Instance.IsOpen) return false;
            if (IsTutorialRunning || MapTutorialUI.IsRunning || StatsSkillTutorialUI.IsRunning || InventoryEquipmentTutorialUI.IsRunning || ShopWindowUI.IsTutorialRunning || EquipmentUpgradeWindowUI.IsTutorialRunning) return false;
            if (loading || actor == null) return false;
            if (actor.GetComponent<PlayerEntity>() is PlayerEntity entity && entity.IsDead) return false;
            var input = actor.GetComponent<PlayerInputReader>();
            if (input != null && (!input.isActiveAndEnabled || input.IsInputBlocked)) return false;
            player = actor;
            InventoryWindowUI.Instance?.SetOpen(false);
            EquipmentWindowUI.Instance?.SetOpen(false);
            SkillWindowUI.Instance?.SetOpen(false);
            PlayerStatsWindowUI.Instance?.SetOpen(false);
            ShopWindowUI.Instance?.Close();
            PetCompanionWindowUI.Instance?.Close();
            MapWindowUI.Instance?.Close();
            ScrollCodexWindowUI.Instance?.Close();
            player.ClearInput();
            windowRoot.SetActive(true);
            GameTimeController.SetPaused(this, true);
            RebuildList();
            Select(boundDungeons.Contains(Selected) ? Selected : boundDungeons.Count > 0 ? boundDungeons[0] : null);
            list.verticalNormalizedPosition = 1f;
            BeginTutorial();
            return true;
        }
        private void RebuildList()
        {
            foreach (var entry in entries) { entry.gameObject.SetActive(false); Destroy(entry.gameObject); }
            entries.Clear();
            boundDungeons.Clear();
            if (dungeons == null) return;
            foreach (var dungeon in dungeons)
            {
                if (dungeon == null) continue;
                var entry = Instantiate(entryTemplate, list.content);
                entry.name = "Dungeon - " + dungeon.displayName;
                entry.gameObject.SetActive(true);
                entry.Bind(this, dungeon);
                entries.Add(entry);
                boundDungeons.Add(dungeon);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(list.content);
        }
        public bool CanEnter(DungeonDefinition dungeon)
        {
            if (dungeon == null || !dungeon.available || string.IsNullOrWhiteSpace(dungeon.sceneName)
                || !Application.CanStreamedLevelBeLoaded(dungeon.sceneName)) return false;
            var progression = player != null ? player.GetComponent<PlayerProgression>() : null;
            return (progression != null ? progression.CurrentLevel : 1) >= dungeon.minimumLevel;
        }
        public void Select(DungeonDefinition dungeon)
        {
            if (IsTutorialRunning) return;
            if (dungeon != null && !boundDungeons.Contains(dungeon)) return;
            Selected = dungeon;
            preview.sprite = dungeon != null ? dungeon.preview : null;
            preview.enabled = preview.sprite != null;
            if (dungeonEmblem != null)
            {
                dungeonEmblem.sprite = dungeon != null ? dungeon.emblem : null;
                dungeonEmblem.preserveAspect = true;
                dungeonEmblem.gameObject.SetActive(true);
                dungeonEmblem.enabled = dungeonEmblem.sprite != null;
            }
            var fitter = preview.GetComponent<AspectRatioFitter>();
            if (fitter != null && preview.sprite != null) fitter.aspectRatio = preview.sprite.rect.width / preview.sprite.rect.height;
            dungeonName.text = dungeon != null ? dungeon.displayName : "등록된 던전 없음";
            description.text = dungeon != null ? dungeon.description : "던전 데이터를 등록해 주세요.";
            levelText.text = dungeon != null ? $"입장 레벨\n<size=23>{dungeon.minimumLevel}</size>" : "입장 레벨\n<size=23>—</size>";
            timeText.text = dungeon != null && dungeon.timeLimitSeconds > 0
                ? $"제한시간\n<size=23>{dungeon.timeLimitSeconds / 60:00}:{dungeon.timeLimitSeconds % 60:00}</size>" : "제한시간\n<size=23>미설정</size>";
            monsterText.text = dungeon != null && dungeon.monsterCount > 0 ? $"몬스터\n<size=23>{dungeon.monsterCount}마리</size>" : "몬스터\n<size=23>미설정</size>";
            var names = new List<string>();
            int iconIndex = 0;
            bool hasPreviewRewards = dungeon != null && dungeon.obtainableRewards != null && dungeon.obtainableRewards.Length > 0;
            if (hasPreviewRewards)
                foreach (var reward in dungeon.obtainableRewards)
                {
                    if (reward == null || string.IsNullOrWhiteSpace(reward.DisplayName)) continue;
                    names.Add(reward.DisplayName);
                    if (iconIndex >= rewardIcons.Length) continue;
                    rewardIcons[iconIndex].sprite = reward.Icon;
                    rewardIcons[iconIndex].enabled = reward.Icon != null;
                    rewardIcons[iconIndex].preserveAspect = true;
                    ItemGradeVisual.Apply(rewardIcons[iconIndex], null);
                    rewardAmounts[iconIndex].text = string.Empty;
                    iconIndex++;
                }
            if (!hasPreviewRewards && dungeon != null && dungeon.clearRewards != null)
                foreach (var reward in dungeon.clearRewards)
                {
                    if (reward == null || reward.item == null) continue;
                    names.Add($"{reward.item.DisplayName} ×{reward.quantity}");
                    if (iconIndex >= rewardIcons.Length) continue;
                    rewardIcons[iconIndex].sprite = reward.item.Icon;
                    rewardIcons[iconIndex].enabled = reward.item.Icon != null;
                    rewardIcons[iconIndex].preserveAspect = true;
                    ItemGradeVisual.Apply(rewardIcons[iconIndex], null);
                    rewardAmounts[iconIndex].text = string.Empty;
                    iconIndex++;
                }
            for (int i = iconIndex; i < rewardIcons.Length; i++)
            { rewardIcons[i].enabled = false; ItemGradeVisual.Apply(rewardIcons[i], null); rewardAmounts[i].text = string.Empty; }
            rewardText.text = names.Count > 0 ? "클리어 보상" : "클리어 보상  미설정";
            for (int i = 0; i < entries.Count; i++) entries[i].SetSelected(boundDungeons[i] == dungeon);
            enterButton.interactable = !loading && CanEnter(dungeon);
            message.text = dungeon != null && !CanEnter(dungeon) ? "입장 조건 또는 연결된 씬을 확인해 주세요." : string.Empty;
        }
        public void EnterSelected()
        {
            if (IsTutorialRunning) return;
            if (!IsOpen || loading || player == null || DungeonRunController.Instance != null || PortalSceneTransition.IsTransitioning || !CanEnter(Selected)) return;
            if (player.GetComponent<PlayerEntity>() is PlayerEntity entity && entity.IsDead) return;
            string destination = Selected.sceneName;
            if (!player.TryGetComponent<PlayerLocationSetter>(out _)) player.gameObject.AddComponent<PlayerLocationSetter>();
            if (!PortalSceneTransition.TryLoad(destination, Selected.spawnPointName,
                success => { if (this != null) loading = false; })) return;
            DungeonRunController.Begin(Selected);
            loading = true;
            enterButton.interactable = false;
            Close();
        }
        public void Close()
        {
            EndTutorial();
            if (windowRoot != null) windowRoot.SetActive(false);
            if (player != null) player.ClearInput();
            GameTimeController.SetPaused(this, false);
        }
        private void BeginTutorial()
        {
            if (tutorialCompletedClasses.Contains(CharacterSelectionState.SelectedClass) || IsTutorialRunning) return;
            tutorialInput = player.GetComponent<PlayerInputReader>();
            if (tutorialInput != null) { tutorialInputWasEnabled = tutorialInput.enabled; tutorialInput.enabled = false; }
            tutorialCanvas = new GameObject("DungeonTutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = tutorialCanvas.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5000;
            var scaler = tutorialCanvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            Canvas.ForceUpdateCanvases();
            var canvasRect = (RectTransform)tutorialCanvas.transform;
            tutorialPanel = list.transform.parent as RectTransform;
            tutorialPosition = tutorialPanel.position; tutorialScale = tutorialPanel.localScale;
            var corners = new Vector3[4]; tutorialPanel.GetWorldCorners(corners);
            Vector2 min = canvasRect.InverseTransformPoint(corners[0]), max = canvasRect.InverseTransformPoint(corners[2]);
            float fit = Mathf.Min(1f, canvasRect.rect.width*.46f / Mathf.Max(1f,max.x-min.x), canvasRect.rect.height*.82f / Mathf.Max(1f,max.y-min.y));
            tutorialPanel.localScale *= fit;
            var center = canvasRect.TransformPoint(canvasRect.rect.min + new Vector2(canvasRect.rect.width*.25f,canvasRect.rect.height*.53f));
            tutorialPanel.position += center - tutorialPanel.TransformPoint(tutorialPanel.rect.center);
            var overlay = new GameObject("DungeonGuide", typeof(RectTransform), typeof(PetCompanionTutorialUI));
            overlay.transform.SetParent(tutorialCanvas.transform,false); tutorial = overlay.GetComponent<PetCompanionTutorialUI>();
            tutorial.ShowCustom(dungeonName.font, "던전 튜토리얼",
                new[] { "1. 던전 목록에서 선택", "2. 선택한 던전 정보", "3. 입장 조건과 제한시간", "4. 특별한 클리어 보상", "5. 준비가 되면 던전 입장" },
                new[] {
                    "던전 목록에서 도전하고 싶은 던전을 고를 수 있습니다.\n목록이 길면 스크롤해서 다른 던전도 살펴보세요.\n이 안내에서는 현재 선택된 던전을 예시로 보여 드립니다.",
                    "던전을 선택하면 해당 던전의 정보가 표시됩니다.\n이름과 설명, 미리보기 이미지, 입장 레벨, 제한시간,\n몬스터와 보상 정보를 확인하고 도전을 준비하세요.",
                    "던전마다 입장에 필요한 레벨과 조건이 있습니다.\n조건을 충족해야 입장 버튼을 사용할 수 있어요.\n제한시간이 있는 던전은 시간 안에 목표를 달성해야 합니다.\n던전 안에서는 메뉴를 열어도 제한시간이 흐르니 주의하세요.",
                    "던전을 클리어하면 특별한 보상을 얻을 수 있습니다.\n보상은 던전마다 다르므로 도전할 던전의 보상 정보를 살펴보세요.\n던전의 목표를 달성하고 클리어에 도전해 보세요!",
                    "도전할 던전을 고르고 준비가 끝나면 던전 입장 버튼을 누르세요.\n최초 발견 후에는 어디서든 O키로 던전 창을 열 수 있어요.\nO키, 닫기 버튼이나 ESC키로 창을 닫을 수 있습니다.\n던전 진행 중에는 다시 입장할 수 없습니다." },
                new[] { list.viewport != null ? list.viewport : (RectTransform)list.transform,
                    preview.rectTransform.parent as RectTransform, levelText.rectTransform, rewardText.rectTransform, (RectTransform)enterButton.transform },
                null, CompleteTutorial, true, false);
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
            if (tutorialPanel != null) { tutorialPanel.position = tutorialPosition; tutorialPanel.localScale = tutorialScale; }
            if (tutorialInput != null) tutorialInput.enabled = tutorialInputWasEnabled;
            if (player != null) player.ClearInput();
            Destroy(tutorialCanvas); tutorialCanvas = null; tutorial = null;
        }
        private void OnDisable() => Close();
        private void SceneChanged(Scene previous, Scene next) => Close();
        private void OnDestroy() { SceneManager.activeSceneChanged -= SceneChanged; EndTutorial(); GameTimeController.SetPaused(this, false); if (Instance == this) Instance = null; }
    }
}
