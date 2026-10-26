using EasternFantasy.Dialogue;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class MapWindowUI : MonoBehaviour
    {
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private RectTransform mapBackground;
        [SerializeField, Min(0.01f)] private float openDuration = 1.05f;
        [SerializeField, Min(0.01f)] private float closeDuration = 0.22f;
        [Header("Region map detail")]
        [SerializeField] private GameObject tutorialDetailRoot;
        [SerializeField] private Button tutorialMapButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Image detailImage;
        [SerializeField] private Sprite tutorialFullMap;
        [SerializeField] private Button giantFarmButton;
        [SerializeField] private Sprite giantFarmFullMap;
        private MapInkRevealUI inkReveal;
        private MapPlayerLocationUI playerLocation;
        private CanvasGroup windowGroup;
        private Vector3 backgroundScale;
        private Sequence popupSequence;
        private bool closing;

        public static MapWindowUI Instance { get; private set; }
        public bool IsShowingDetail => tutorialDetailRoot != null && tutorialDetailRoot.activeSelf;
        public bool IsShowingGiantFarm => IsShowingDetail && detailImage != null && detailImage.sprite == giantFarmFullMap;
        public bool IsShowingTutorial => IsShowingDetail && detailImage != null && detailImage.sprite == tutorialFullMap;
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;
        internal RectTransform TutorialPanel => windowRoot != null ? windowRoot.transform as RectTransform : null;
        internal RectTransform TutorialOverview => mapBackground;
        internal RectTransform TutorialDetail => detailImage != null ? detailImage.rectTransform : null;
        internal RectTransform TutorialIsland => CurrentRegionIsGiantFarm
            ? giantFarmButton != null ? giantFarmButton.transform as RectTransform : null
            : tutorialMapButton != null ? tutorialMapButton.transform as RectTransform : null;
        private bool CurrentRegionIsGiantFarm => SceneManager.GetActiveScene().name.StartsWith("GiantFarm_");
        internal void ShowCurrentRegionForTutorial()
        {
            if (CurrentRegionIsGiantFarm) ShowGiantFarmMap(); else ShowTutorialMap();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            HideUnavailableNotice();
            BindMapButtons();
            PreparePopup();
            GetComponent<MapDetailDiscoveryUI>()?.Initialize();
            CloseInstant();
            SceneManager.activeSceneChanged += SceneChanged;
            if (GetComponent<MapTutorialUI>() == null) gameObject.AddComponent<MapTutorialUI>();
        }

        private void Update()
        {
            if (MapTutorialUI.IsRunning) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (IsOpen && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (IsShowingDetail && !closing) ShowOverview(); else Close();
                return;
            }
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && (selected.GetComponent<TMPro.TMP_InputField>() != null
                || selected.GetComponent<UnityEngine.UI.InputField>() != null)) return;
            if (keyboard.mKey.wasPressedThisFrame) Toggle();
        }

        public void Toggle() { if (IsOpen && !closing) Close(); else Open(); }

        public void Open()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (scene == "GapInSpace")
            {
                if (IsOpen) CloseInstant();
                ShowUnavailableNotice();
                return;
            }
            if (scene == "MainScreen" || scene == "ChooseCharactor") return;
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
            if (StatsSkillTutorialUI.IsRunning || InventoryEquipmentTutorialUI.IsRunning
                || DungeonWindowUI.IsTutorialRunning || ShopWindowUI.IsTutorialRunning
                || EquipmentUpgradeWindowUI.IsTutorialRunning) return;
            if (windowRoot == null || !isActiveAndEnabled || (IsOpen && !closing)) return;
            HideUnavailableNotice();
            if (Application.isPlaying) MapRegionDiscovery.RecordSceneEntry(scene);
            foreach (var island in GetComponentsInChildren<MapIslandDiscoveryUI>(true)) island.Refresh();
            PreparePopup();
            popupSequence?.Kill();
            closing = false;
            if (!IsOpen)
            {
                ShowOverview();
                windowGroup.alpha = 0f;
                if (mapBackground != null) mapBackground.localScale = backgroundScale;
            }
            windowRoot.SetActive(true);
            RefreshPlayerLocation();
            windowGroup.blocksRaycasts = true;
            windowGroup.interactable = false;
            GameTimeController.SetPaused(this, true);
            popupSequence = DOTween.Sequence().SetUpdate(true);
            popupSequence.Append(windowGroup.DOFade(1f, .12f).SetEase(Ease.OutCubic));
            if (inkReveal != null) inkReveal.AnimateOpen(popupSequence, Mathf.Max(.12f, openDuration));
            popupSequence.OnComplete(() =>
            {
                popupSequence = null;
                inkReveal?.Restore();
                windowGroup.interactable = true;
            });
        }

        private void BindMapButtons()
        {
            if (tutorialMapButton != null)
            {
                tutorialMapButton.onClick.RemoveListener(ShowTutorialMap);
                tutorialMapButton.onClick.AddListener(ShowTutorialMap);
            }
            if (giantFarmButton != null)
            {
                giantFarmButton.onClick.RemoveListener(ShowGiantFarmMap);
                giantFarmButton.onClick.AddListener(ShowGiantFarmMap);
            }
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ShowOverview);
                backButton.onClick.AddListener(ShowOverview);
            }
        }

        public void ShowTutorialMap()
        {
            if (Application.isPlaying && !MapRegionDiscovery.IsVisited(MapRegionDiscovery.Tutorial)) return;
            ShowDetail(tutorialFullMap);
        }
        public void ShowGiantFarmMap()
        {
            if (Application.isPlaying && !MapRegionDiscovery.IsVisited(MapRegionDiscovery.GiantFarm)) return;
            ShowDetail(giantFarmFullMap);
        }

        private void ShowDetail(Sprite fullMap)
        {
            if (!IsOpen || closing || tutorialDetailRoot == null || detailImage == null || fullMap == null) return;
            popupSequence?.Kill(); popupSequence = null;
            inkReveal?.Restore();
            if (windowGroup != null) { windowGroup.alpha = 1; windowGroup.interactable = true; }
            detailImage.sprite = fullMap;
            tutorialDetailRoot.SetActive(true);
            GetComponent<MapDetailDiscoveryUI>()?.Refresh();
            if (mapBackground != null) mapBackground.gameObject.SetActive(false);
            RefreshPlayerLocation();
        }

        public void ShowOverview()
        {
            GetComponent<MapDetailDiscoveryUI>()?.StopPresentation();
            if (tutorialDetailRoot != null) tutorialDetailRoot.SetActive(false);
            if (mapBackground != null) mapBackground.gameObject.SetActive(true);
            RefreshPlayerLocation();
        }

        private void RefreshPlayerLocation()
        {
            if (playerLocation == null) playerLocation = GetComponent<MapPlayerLocationUI>();
            if (playerLocation != null) playerLocation.RefreshLocation();
        }

        private void PreparePopup()
        {
            if (windowGroup != null || windowRoot == null) return;
            inkReveal = GetComponent<MapInkRevealUI>();
            windowGroup = windowRoot.GetComponent<CanvasGroup>();
            if (windowGroup == null) windowGroup = windowRoot.AddComponent<CanvasGroup>();
            if (mapBackground == null) mapBackground = windowRoot.transform.Find("MapBackground") as RectTransform;
            if (mapBackground != null) backgroundScale = mapBackground.localScale;
        }

        public void Close()
        {
            if (closing) return;
            if (!IsOpen || !isActiveAndEnabled) { CloseInstant(); return; }
            PreparePopup();
            popupSequence?.Kill();
            closing = true;
            GetComponent<MapDetailDiscoveryUI>()?.StopPresentation();
            windowGroup.interactable = false;
            popupSequence = DOTween.Sequence().SetUpdate(true);
            popupSequence.Append(windowGroup.DOFade(0f, Mathf.Max(.01f, closeDuration)).SetEase(Ease.InCubic));
            if (inkReveal != null) inkReveal.AnimateClose(popupSequence, Mathf.Max(.01f, closeDuration));
            // Gameplay stays paused until the closing animation has finished.
            popupSequence.OnComplete(() => { popupSequence = null; CloseInstant(); });
        }

        internal void CloseInstant()
        {
            popupSequence?.Kill();
            popupSequence = null;
            closing = false;
            inkReveal?.Restore();
            if (windowGroup != null)
            {
                windowGroup.alpha = 0f;
                windowGroup.interactable = false;
                windowGroup.blocksRaycasts = false;
            }
            if (mapBackground != null) mapBackground.localScale = backgroundScale;
            if (windowRoot != null) windowRoot.SetActive(false);
            ShowOverview();
            // Release only this window: other pause owners remain in control.
            GameTimeController.SetPaused(this, false);
        }

        private void ShowUnavailableNotice() => SystemNoticeUI.Show("지도를 볼 수 없는 장소입니다.", this);
        private void HideUnavailableNotice() => SystemNoticeUI.Hide(this);

        private void SceneChanged(Scene previous, Scene next) { CloseInstant(); HideUnavailableNotice(); }
        private void OnDisable() { CloseInstant(); HideUnavailableNotice(); }
        private void OnDestroy()
        {
            HideUnavailableNotice();
            CloseInstant();
            if (tutorialMapButton != null) tutorialMapButton.onClick.RemoveListener(ShowTutorialMap);
            if (giantFarmButton != null) giantFarmButton.onClick.RemoveListener(ShowGiantFarmMap);
            if (backButton != null) backButton.onClick.RemoveListener(ShowOverview);
            SceneManager.activeSceneChanged -= SceneChanged;
            if (Instance == this) Instance = null;
        }
    }
}

