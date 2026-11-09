using System.Collections.Generic;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    public sealed class SoulstoneWindowUI : MonoBehaviour
    {
        public static SoulstoneWindowUI Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.modal != null && Instance.modal.activeSelf;
        public string SelectedScene => selected != null ? selected.sceneName : string.Empty;
        internal SoulstoneWindowView TutorialView => view;
        private SoulstoneTutorialUI tutorial;
        private SoulstoneActivationVisual stoneVisual;
        private SoulstoneCatalog catalog;
        private SoulstoneCatalog.Destination selected;
        private Transform stone;
        private PlayerInputReader input;
        private GameObject modal;
        private SoulstoneWindowView view;
        private RectTransform panel;
        private Image preview;
        private AspectRatioFitter previewAspect;
        private MenuButtonHoverEffect travelHover;
        private TMP_Text title, status;
        private GameObject prompt;
        private Transform promptTarget;
        private float promptHeight;
        private Button travel;
        private readonly List<Button> entries = new List<Button>();
        private readonly List<TMP_Text> entryLabels = new List<TMP_Text>();
        private const float InteractionRange = 3.5f;
        private static readonly Color Gold = new Color(1f, .91f, .72f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Instance != null) return;
            var go = new GameObject("Soulstone Travel", typeof(RectTransform));
            go.AddComponent<SoulstoneWindowUI>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            catalog = Resources.Load<SoulstoneCatalog>("SoulstoneCatalog");
            SceneManager.activeSceneChanged += SceneChanged;
            LocateStone();
            if (Application.isPlaying) tutorial = gameObject.AddComponent<SoulstoneTutorialUI>();
        }

        private void LocateStone()
        {
            var arrival = FindArrivalStone(SceneManager.GetActiveScene());
            stone = arrival != null ? arrival.transform : null;
            stoneVisual = null;
            if (Application.isPlaying && arrival != null)
            {
                stoneVisual = arrival.GetComponent<SoulstoneActivationVisual>();
                if (stoneVisual == null) stoneVisual = arrival.AddComponent<SoulstoneActivationVisual>();
                stoneVisual.Initialize(isTemple: SceneManager.GetActiveScene().name == "TempleGate", catalog: catalog);
            }
        }

        public const string ArrivalTarget = "SoulstoneArrival";
        public static GameObject FindArrivalStone(Scene scene)
        {
            var data = Resources.Load<SoulstoneCatalog>("SoulstoneCatalog");
            if (data == null) return null;
            foreach (var destination in data.destinations)
            {
                if (destination == null || destination.sceneName != scene.name) continue;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
                        if (candidate.name.Trim() == destination.stoneName
                            || (scene.name == "TempleGate" && (candidate.name.Trim() == "TempleRespornStone" || candidate.name.Trim() == "절 문 귀혼석")))
                            return candidate.gameObject;
            }
            return null;
        }

        internal bool IsNearCurrentStone(PlayerMovement2D player) => IsNearStone(player);

        private bool IsNearStone(PlayerMovement2D player)
        {
            if (stone == null || !stone.gameObject.activeInHierarchy || player == null) return false;
            var renderer = stone.GetComponentInChildren<SpriteRenderer>();
            Vector3 anchor = renderer != null ? new Vector3(renderer.bounds.center.x, renderer.bounds.min.y, player.transform.position.z) : stone.position;
            return Vector2.Distance(player.transform.position, anchor) <= InteractionRange;
        }

        private void Update()
        {
            if (catalog == null || GameplayHudVisibility.IsMenuScene) return;
            if (stone == null) LocateStone();
            if (SoulstoneTutorialUI.IsRunning)
            {
                UpdateInteractionPrompt(tutorial != null && tutorial.CurrentStep == 1 && !tutorial.IsPlayingActivationSequence);
                return;
            }
            if (!SoulstoneProgress.IsActivated && SceneManager.GetActiveScene().name != "TempleGate")
            {
                UpdateInteractionPrompt(false); return;
            }
            var keyboard = Keyboard.current;
            if (IsOpen)
            {
                if (input == null || input.GetComponent<PlayerEntity>()?.IsDead == true
                    || PortalSceneTransition.IsTransitioning || !IsNearStone(input.GetComponent<PlayerMovement2D>())) Close();
                else if (keyboard != null && (keyboard.gKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)) Close();
                return;
            }
            var actor = FindFirstObjectByType<PlayerMovement2D>();
            var reader = actor != null ? actor.GetComponent<PlayerInputReader>() : null;
            bool nearby = IsNearStone(actor) && reader != null && reader.CanControlGameplay && Time.timeScale > 0f
                && !SaveGameService.IsRestoring && !SceneLoadingScreen.IsVisible;
            if (nearby && tutorial != null && !SoulstoneProgress.TutorialCompleted && tutorial.TryBegin(reader, stoneVisual)) return;
            UpdateInteractionPrompt(nearby);
            if (nearby && keyboard != null && keyboard.gKey.wasPressedThisFrame && !IsTyping()) TryOpen();
        }

        private void UpdateInteractionPrompt(bool visible)
        {
            if (!visible || stone == null)
            {
                if (prompt != null) prompt.SetActive(false);
                return;
            }
            if (prompt == null)
            {
                if (catalog.interactionPromptPrefab == null) return;
                prompt = Instantiate(catalog.interactionPromptPrefab, transform, false);
                prompt.name = "Interact G Prompt";
            }
            if (promptTarget != stone)
            {
                promptTarget = stone;
                var renderer = stone.GetComponentInChildren<SpriteRenderer>();
                var collider = stone.GetComponentInChildren<Collider2D>();
                float localTop = SceneManager.GetActiveScene().name == "TempleGate" ? catalog.templePromptTop : catalog.villagePromptTop;
                float top = renderer != null && localTop > 0f
                    ? renderer.transform.TransformPoint(new Vector3(0, localTop, 0)).y
                    : renderer != null ? renderer.bounds.max.y
                    : collider != null ? collider.bounds.max.y : stone.position.y + 1.3f;
                promptHeight = top - stone.position.y + .35f;
            }
            prompt.transform.position = stone.position + Vector3.up * promptHeight;
            prompt.SetActive(true);
        }
        private void LateUpdate()
        {
            if (prompt != null && prompt.activeSelf && promptTarget != null)
                prompt.transform.position = promptTarget.position + Vector3.up * promptHeight;
        }

        private static bool IsTyping()
        {
            var target = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return target != null && (target.GetComponent<TMP_InputField>() != null || target.GetComponent<InputField>() != null);
        }

        public bool TryOpen()
        {
            var player = FindFirstObjectByType<PlayerMovement2D>();
            var reader = player != null ? player.GetComponent<PlayerInputReader>() : null;
            if (!SoulstoneProgress.IsActivated || SoulstoneTutorialUI.IsRunning || IsOpen || catalog == null || !IsNearStone(player) || reader == null || !reader.CanControlGameplay
                || Time.timeScale <= 0f || SaveGameService.IsRestoring || SceneLoadingScreen.IsVisible || IsTyping()
                || MapTutorialUI.IsRunning || StatsSkillTutorialUI.IsRunning || InventoryEquipmentTutorialUI.IsRunning
                || DungeonWindowUI.IsTutorialRunning || ShopWindowUI.IsTutorialRunning || EquipmentUpgradeWindowUI.IsTutorialRunning) return false;
            Open(reader);
            return true;
        }

        internal bool OpenForTutorial(PlayerInputReader reader)
        {
            if (!SoulstoneTutorialUI.IsRunning || !SoulstoneProgress.IsActivated || IsOpen || reader == null
                || !IsNearStone(reader.GetComponent<PlayerMovement2D>()) || SaveGameService.IsRestoring
                || PortalSceneTransition.IsTransitioning || SceneLoadingScreen.IsVisible) return false;
            Open(reader); return true;
        }

        private void Open(PlayerInputReader reader)
        {
            if (modal == null) Build();
            input = reader;
            reader.GetComponent<PlayerMovement2D>().ClearInput();
            input.SetInputBlocked(this, true);
            GameTimeController.SetPaused(this, true);
            modal.SetActive(true);
            if (prompt != null) prompt.SetActive(false);
            string current = SceneManager.GetActiveScene().name;
            var destination = System.Array.Find(catalog.destinations, d => d.sceneName != current && Application.CanStreamedLevelBeLoaded(d.sceneName));
            Select(destination != null ? destination.sceneName : current);
        }

        public void Select(string sceneName)
        {
            selected = System.Array.Find(catalog.destinations, d => d.sceneName == sceneName);
            if (selected == null) return;
            preview.sprite = selected.preview;
            preview.enabled = selected.preview != null;
            if (selected.preview != null) previewAspect.aspectRatio = selected.preview.rect.width / selected.preview.rect.height;
            title.text = selected.displayName;
            bool current = selected.sceneName == SceneManager.GetActiveScene().name;
            travel.interactable = !current && Application.CanStreamedLevelBeLoaded(selected.sceneName);
            travelHover.enabled = travel.interactable;
            travel.GetComponent<Image>().color = travel.interactable ? Color.white : new Color(.48f, .48f, .48f, 1f);
            status.text = current ? "현재 위치입니다." : "선택한 지역의 귀혼석으로 이동합니다.";
            for (int i = 0; i < entries.Count; i++)
            {
                bool chosen = catalog.destinations[i] == selected;
                entries[i].GetComponent<Image>().color = chosen ? new Color(1f, .86f, .58f) : Color.white;
                entryLabels[i].color = chosen ? new Color(1f, .95f, .8f) : Gold;
            }
            view.SetNpcs(selected.npcs);
            tutorial?.DestinationSelected(sceneName);
        }

        public bool Travel()
        {
            if (!IsOpen || selected == null || !travel.interactable || input == null
                || !IsNearStone(input.GetComponent<PlayerMovement2D>()) || PortalSceneTransition.IsTransitioning) return false;
            if (SoulstoneTutorialUI.IsRunning && tutorial.CurrentStep != 3) return false;
            var destination = selected;
            var actor = input;
            tutorial?.CompleteForTravel();
            if (!actor.TryGetComponent<PlayerLocationSetter>(out _)) actor.gameObject.AddComponent<PlayerLocationSetter>();
            Close();
            return PortalSceneTransition.TryLoad(destination.sceneName, ArrivalTarget,
                success => { if (!success) SystemNoticeUI.ShowFailure("귀혼석 이동에 실패했습니다. 다시 시도해 주세요."); }, playPortalAudio: true);
        }

        public void Close()
        {
            if (modal != null) modal.SetActive(false);
            if (input != null) { input.SetInputBlocked(this, false); input.GetComponent<PlayerMovement2D>()?.ClearInput(); }
            input = null;
            GameTimeController.SetPaused(this, false);
        }

        private void SceneChanged(Scene oldScene, Scene newScene) { Close(); LocateStone(); if (prompt != null) prompt.SetActive(false); promptTarget = null; }
        private void OnDisable() => Close();
        private void OnDestroy() { SceneManager.activeSceneChanged -= SceneChanged; Close(); if (Instance == this) Instance = null; }

        private void Build()
        {
            var prefab = catalog.windowPrefab;
            if (prefab == null) throw new System.InvalidOperationException("SoulstoneWindow prefab is missing.");
            view = Instantiate(prefab, transform, false);
            view.name = "SoulstoneWindow";
            modal = view.modal; panel = view.panel;
            preview = view.preview; previewAspect = view.previewAspect;
            title = view.mapName; status = view.travelStatus;
            travel = view.travel; travelHover = view.travelHover;
            travel.onClick.AddListener(() => Travel());
            view.close.onClick.AddListener(Close);
            // Reuse the authored rows; only extra catalog entries need a template clone.
            for (int i = 0; i < catalog.destinations.Length; i++)
            {
                var button = i < view.destinationRows.Count ? view.destinationRows[i]
                    : Instantiate(view.destinationRows[0], view.destinationContent);
                string scene = catalog.destinations[i].sceneName;
                button.gameObject.SetActive(true);
                button.onClick.AddListener(() => Select(scene));
                var label = button.GetComponentInChildren<TMP_Text>(true);
                label.text = catalog.destinations[i].displayName;
                entries.Add(button); entryLabels.Add(label);
            }
            for (int i = catalog.destinations.Length; i < view.destinationRows.Count; i++)
                view.destinationRows[i].gameObject.SetActive(false);
            modal.SetActive(false);
        }

#if UNITY_EDITOR
        public void ShowEditorPreview(SoulstoneCatalog data, string sceneName)
        {
            catalog = data;
            if (modal == null) Build();
            modal.SetActive(true); Select(sceneName);
            view.FitToCanvas();
        }
#endif
    }
}
