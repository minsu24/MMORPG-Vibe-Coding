using System;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class MapPlayerLocationUI : MonoBehaviour
    {
        [Serializable]
        public sealed class Location
        {
            public string sceneName;
            public bool giantFarm;
            [Tooltip("Printed banner outline in the 1920x1080 source image, measured from top left.")]
            public Vector2[] bannerOutline;
        }
        [SerializeField] private MapWindowUI window;
        [SerializeField] private Image detailImage;
        [SerializeField] private RectTransform tutorialIsland;
        [SerializeField] private RectTransform giantFarmIsland;
        [SerializeField] private Image overviewIcon;
        [SerializeField] private Image detailIcon;
        [SerializeField] private MapLocationOutlineGraphic locationOutline;
        [SerializeField] private Sprite dosaIcon, monkIcon, swordswomanIcon;
        [SerializeField] private Location[] locations = Array.Empty<Location>();
        [SerializeField] private float bobHeight = 6f;
        [SerializeField] private float bobPeriod = 2.4f;
        private string currentScene;
        private CharacterClassId currentClass;
        private Location location;
        private bool showingDetail, showingFarm;
        private Rect previousRect;
        private Vector2 detailBase;
        private bool initialized;
        internal System.Collections.Generic.IReadOnlyList<Location> Locations => locations;
        internal bool HasCurrentLocation => Array.Exists(locations, x => x.sceneName == SceneManager.GetActiveScene().name);
        internal RectTransform OverviewTutorialTarget => overviewIcon != null ? overviewIcon.rectTransform : null;

        private void LateUpdate()
        {
            if (window == null || !window.IsOpen) return;
            var definition = PlayerClassRuntime.ActiveDefinition;
            CharacterClassId classId = definition != null ? definition.ClassId : CharacterSelectionState.SelectedClass;
            string scene = SceneManager.GetActiveScene().name;
            if (!initialized || scene != currentScene || classId != currentClass
                || showingDetail != window.IsShowingDetail || showingFarm != window.IsShowingGiantFarm)
                ApplyLocation(scene, classId);
            UpdateMarkerLayout();
        }

        // Separate groups keep the detail reveal independent of the overview marker animation.
        public float DetailPresentationAlpha { get; private set; } = 1;
        public void SetDetailPresentationAlpha(float alpha)
        {
            DetailPresentationAlpha = alpha;
            SetPresentationAlpha(detailIcon, alpha);
            SetPresentationAlpha(locationOutline, alpha);
        }
        private static void SetPresentationAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            var group = graphic.GetComponent<CanvasGroup>();
            if (group == null) group = graphic.gameObject.AddComponent<CanvasGroup>();
            group.alpha = alpha;
        }

        public void RefreshLocation()
        {
            var definition = PlayerClassRuntime.ActiveDefinition;
            ApplyLocation(SceneManager.GetActiveScene().name,
                definition != null ? definition.ClassId : CharacterSelectionState.SelectedClass);
            UpdateMarkerLayout();
        }

        private void ApplyLocation(string scene, CharacterClassId classId)
        {
            initialized = true;
            currentScene = scene;
            currentClass = classId;
            showingDetail = window.IsShowingDetail;
            showingFarm = window.IsShowingGiantFarm;
            location = Array.Find(locations, x => x.sceneName == scene);
            Sprite icon = classId == CharacterClassId.Monk ? monkIcon
                : classId == CharacterClassId.Swordswoman ? swordswomanIcon : dosaIcon;
            overviewIcon.sprite = detailIcon.sprite = icon;
            bool matching = location != null && location.giantFarm == showingFarm;
            overviewIcon.gameObject.SetActive(location != null && !showingDetail && icon != null);
            detailIcon.gameObject.SetActive(showingDetail && matching && icon != null);
            locationOutline.gameObject.SetActive(showingDetail && matching);
            previousRect = default;
        }

        private void UpdateMarkerLayout()
        {
            if (location == null) return;
            float bob = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2 / Mathf.Max(.1f, bobPeriod)) * bobHeight;
            if (!showingDetail)
            {
                RectTransform island = location.giantFarm ? giantFarmIsland : tutorialIsland;
                if (island == null) { overviewIcon.gameObject.SetActive(false); return; }
                Rect rect = island.rect;
                Vector3 anchor = island.TransformPoint(new Vector2(rect.center.x, rect.yMin + rect.height * .68f));
                Vector2 local = overviewIcon.rectTransform.parent.InverseTransformPoint(anchor);
                overviewIcon.rectTransform.anchoredPosition = local + Vector2.up * bob;
                return;
            }
            if (!detailIcon.gameObject.activeSelf) return;
            Rect drawn = GetDrawingRect(detailImage);
            if (drawn != previousRect)
            {
                previousRect = drawn;
                var points = location.bannerOutline;
                if (points == null || points.Length < 3) return;
                var local = new Vector2[points.Length];
                float minX = float.MaxValue, maxX = float.MinValue, bottom = 0;
                for (int i = 0; i < points.Length; i++)
                {
                    local[i] = new Vector2(drawn.xMin + points[i].x / 1920f * drawn.width,
                        drawn.yMax - points[i].y / 1080f * drawn.height);
                    minX = Mathf.Min(minX, points[i].x); maxX = Mathf.Max(maxX, points[i].x);
                    bottom = Mathf.Max(bottom, points[i].y);
                }
                locationOutline.SetPath(local);
                detailBase = new Vector2(drawn.xMin + (minX + maxX) * .5f / 1920f * drawn.width,
                    drawn.yMax - bottom / 1080f * drawn.height - 48f);
            }
            detailIcon.rectTransform.anchoredPosition = detailBase + Vector2.up * bob;
        }

        public static Rect GetDrawingRect(Image image)
        {
            Rect rect = image.rectTransform.rect;
            if (!image.preserveAspect || image.sprite == null) return rect;
            float aspect = image.sprite.rect.width / image.sprite.rect.height;
            if (rect.width / rect.height > aspect)
            {
                float width = rect.height * aspect;
                rect.x += (rect.width - width) * .5f; rect.width = width;
            }
            else
            {
                float height = rect.width / aspect;
                rect.y += (rect.height - height) * .5f; rect.height = height;
            }
            return rect;
        }
    }
}
