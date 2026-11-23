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
        [SerializeField] private Button oldTempleButton;
        [SerializeField] private Sprite oldTempleFullMap;
        [SerializeField] private Sprite oldTempleSidePathMap;
        [SerializeField] private Sprite oldTemplePageArrow;
        [SerializeField] private Texture2D oldTempleTransitionClouds;
        private Button oldTemplePageButton;
        private MapPageCloudGraphic detailPageClouds;
        private Sequence detailPageSequence;
        private const float DetailPageTransitionDuration = .46f;
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
        private bool IsShowingOldTempleSidePath => IsShowingDetail && detailImage != null
            && oldTempleSidePathMap != null && detailImage.sprite == oldTempleSidePathMap;
        internal string DetailRegion => !IsShowingDetail || detailImage == null ? null
            : detailImage.sprite == oldTempleFullMap || IsShowingOldTempleSidePath ? MapRegionDiscovery.OldTemple
            : IsShowingGiantFarm ? MapRegionDiscovery.GiantFarm
            : IsShowingTutorial ? MapRegionDiscovery.Tutorial : null;
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;
        internal RectTransform TutorialPanel => windowRoot != null ? windowRoot.transform as RectTransform : null;
        internal RectTransform TutorialOverview => mapBackground;
        internal RectTransform TutorialDetail => detailImage != null ? detailImage.rectTransform : null;
        internal RectTransform TutorialBackButton => backButton != null ? backButton.transform as RectTransform : null;
        internal bool IsShowingCurrentRegionForTutorial => IsShowingDetail && DetailRegion == CurrentRegion;
        internal RectTransform TutorialIsland
        {
            get
            {
                Button button = CurrentRegion == MapRegionDiscovery.OldTemple ? oldTempleButton
                    : CurrentRegion == MapRegionDiscovery.GiantFarm ? giantFarmButton : tutorialMapButton;
                return button != null ? button.transform as RectTransform : null;
            }
        }
        private static string CurrentRegion => MapRegionDiscovery.RegionForScene(SceneManager.GetActiveScene().name);
        internal bool IsSceneOnDetailPage(string scene)
        {
            string region = DetailRegion;
            return region != null && region == MapRegionDiscovery.RegionForScene(scene)
                && (region != MapRegionDiscovery.OldTemple
                    || MapRegionDiscovery.IsOldTempleSidePathScene(scene) == IsShowingOldTempleSidePath);
        }

        private Sprite GetOldTempleMapForScene(string scene)
            => oldTempleSidePathMap != null && MapRegionDiscovery.IsOldTempleSidePathScene(scene)
                ? oldTempleSidePathMap : oldTempleFullMap;

        internal void ShowCurrentRegionForTutorial()
        {
            if (CurrentRegion == MapRegionDiscovery.OldTemple) ShowOldTempleMap();
            else if (CurrentRegion == MapRegionDiscovery.GiantFarm) ShowGiantFarmMap();
            else ShowTutorialMap();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            HideUnavailableNotice();
            BindMapButtons();
            CreateOldTemplePageButton();
            PreparePopup();
            GetComponent<MapDetailDiscoveryUI>()?.Initialize();
            CloseInstant();
            SceneManager.activeSceneChanged += SceneChanged;
            if (GetComponent<MapTutorialUI>() == null) gameObject.AddComponent<MapTutorialUI>();
        }

        private void Update()
        {
            if (SoulstoneWindowUI.IsOpen || SoulstoneTutorialUI.IsRunning) return;
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
            if (oldTempleSidePathMap != null && detailImage != null && tutorialDetailRoot != null
                && MapRegionDiscovery.IsOldTempleSidePathScene(scene))
                ApplyDetailMap(oldTempleSidePathMap);
            else
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
            if (oldTempleButton != null)
            {
                oldTempleButton.onClick.RemoveListener(ShowOldTempleMap);
                oldTempleButton.onClick.AddListener(ShowOldTempleMap);
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
        public void ShowOldTempleMap()
        {
            if (Application.isPlaying && !MapRegionDiscovery.IsVisited(MapRegionDiscovery.OldTemple)) return;
            ShowDetail(GetOldTempleMapForScene(SceneManager.GetActiveScene().name));
        }

        private void ShowDetail(Sprite fullMap)
        {
            if (!IsOpen || closing || tutorialDetailRoot == null || detailImage == null || fullMap == null) return;
            StopDetailPageTransition();
            popupSequence?.Kill(); popupSequence = null;
            inkReveal?.Restore();
            if (windowGroup != null) { windowGroup.alpha = 1; windowGroup.interactable = true; }
            ApplyDetailMap(fullMap);
        }

        private void ApplyDetailMap(Sprite fullMap, bool revealMap = true)
        {
            detailImage.sprite = fullMap;
            RefreshOldTemplePageButton();
            tutorialDetailRoot.SetActive(true);
            GetComponent<MapDetailDiscoveryUI>()?.Refresh(revealMap);
            if (mapBackground != null) mapBackground.gameObject.SetActive(false);
            RefreshPlayerLocation();
        }

        private void CreateOldTemplePageButton()
        {
            if (tutorialDetailRoot == null || detailImage == null || oldTempleFullMap == null
                || oldTempleSidePathMap == null || oldTemplePageArrow == null) return;

            var buttonObject = new GameObject("Old Temple Page Arrow", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.layer = tutorialDetailRoot.layer;
            var arrow = buttonObject.GetComponent<Image>();
            var rect = arrow.rectTransform;
            rect.SetParent(tutorialDetailRoot.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, .5f);
            rect.anchoredPosition = new Vector2(-58f, 0f);
            rect.sizeDelta = new Vector2(72f, 72f);
            arrow.sprite = oldTemplePageArrow;
            arrow.preserveAspect = true;
            oldTemplePageButton = buttonObject.GetComponent<Button>();
            oldTemplePageButton.targetGraphic = arrow;
            oldTemplePageButton.onClick.AddListener(ChangeOldTemplePage);
            buttonObject.SetActive(false);

            var cloudObject = new GameObject("Old Temple Page Clouds", typeof(RectTransform), typeof(MapPageCloudGraphic));
            cloudObject.layer = tutorialDetailRoot.layer;
            detailPageClouds = cloudObject.GetComponent<MapPageCloudGraphic>();
            detailPageClouds.raycastTarget = false;
            detailPageClouds.SetTexture(oldTempleTransitionClouds);
            var cloudRect = detailPageClouds.rectTransform;
            cloudRect.SetParent(tutorialDetailRoot.transform, false);
            cloudRect.anchorMin = Vector2.zero;
            cloudRect.anchorMax = Vector2.one;
            cloudRect.offsetMin = cloudRect.offsetMax = Vector2.zero;
            cloudObject.SetActive(false);
        }

        private void RefreshOldTemplePageButton()
        {
            if (oldTemplePageButton == null) return;
            bool sidePath = detailImage.sprite == oldTempleSidePathMap;
            oldTemplePageButton.gameObject.SetActive(sidePath || detailImage.sprite == oldTempleFullMap);
            oldTemplePageButton.transform.localRotation = Quaternion.Euler(0f, 0f, sidePath ? 180f : 0f);
        }

        private void ChangeOldTemplePage()
        {
            if (closing || detailPageSequence != null || DetailRegion != MapRegionDiscovery.OldTemple) return;

            bool goingUp = !IsShowingOldTempleSidePath;
            Sprite nextMap = goingUp ? oldTempleSidePathMap : oldTempleFullMap;
            oldTemplePageButton.interactable = false;
            detailPageClouds.SetFrame(0f, goingUp);
            detailPageClouds.gameObject.SetActive(true);
            detailPageSequence = DOTween.Sequence().SetUpdate(true);
            detailPageSequence.Append(DOVirtual.Float(0f, 1f, DetailPageTransitionDuration,
                progress => detailPageClouds.SetFrame(progress, goingUp)).SetEase(Ease.InOutSine));
            detailPageSequence.InsertCallback(DetailPageTransitionDuration * .5f, () => ApplyDetailMap(nextMap, false));
            detailPageSequence.OnComplete(FinishDetailPageTransition);
        }

        private void StopDetailPageTransition()
        {
            if (detailPageSequence == null) return;
            detailPageSequence.Kill();
            FinishDetailPageTransition();
        }

        private void FinishDetailPageTransition()
        {
            detailPageSequence = null;
            detailPageClouds.gameObject.SetActive(false);
            oldTemplePageButton.interactable = true;
        }

        public void ShowOverview()
        {
            StopDetailPageTransition();
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
            StopDetailPageTransition();
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

        private void ShowUnavailableNotice() => SystemNoticeUI.ShowFailure("지도를 볼 수 없는 장소입니다.", this);
        private void HideUnavailableNotice() => SystemNoticeUI.Hide(this);

        private void SceneChanged(Scene previous, Scene next) { CloseInstant(); HideUnavailableNotice(); }
        private void OnDisable() { CloseInstant(); HideUnavailableNotice(); }
        private void OnDestroy()
        {
            HideUnavailableNotice();
            CloseInstant();
            if (tutorialMapButton != null) tutorialMapButton.onClick.RemoveListener(ShowTutorialMap);
            if (giantFarmButton != null) giantFarmButton.onClick.RemoveListener(ShowGiantFarmMap);
            if (oldTempleButton != null) oldTempleButton.onClick.RemoveListener(ShowOldTempleMap);
            if (backButton != null) backButton.onClick.RemoveListener(ShowOverview);
            SceneManager.activeSceneChanged -= SceneChanged;
            if (Instance == this) Instance = null;
        }
    }
}

