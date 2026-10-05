using System.Collections;
using System.Collections.Generic;
using EasternFantasy.Player;
using EasternFantasy.Quest;
using EasternFantasy.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class MinimapUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform mapViewport;
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private TMP_Text regionName;
        [SerializeField] private Sprite markerSprite;
        [SerializeField] private Sprite availableQuestSprite;
        [SerializeField] private Sprite activeQuestSprite;
        [SerializeField] private Sprite readyQuestSprite;
        [SerializeField, Min(0.1f)] private float mapPadding = 1.5f;
        [SerializeField] private Color terrainColor = new Color(0.32f, 0.43f, 0.41f, 1f);

        private sealed class Marker
        {
            public Transform Target;
            public RectTransform Rect;
            public Image Image;
            public QuestGiver QuestGiver;
        }

        private static MinimapUI instance;
        private readonly List<GameObject> generatedObjects = new List<GameObject>();
        private readonly List<Marker> markers = new List<Marker>();
        private Rect worldBounds;
        private Vector2 contentSize;
        private PlayerMovement2D player;
        private RectTransform playerMarker;
        private Coroutine rebuildRoutine;
        private float nextQuestRefresh;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                enabled = false;
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (instance == this)
                SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start() => QueueRebuild();

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => QueueRebuild();

        private void QueueRebuild()
        {
            panelRoot.SetActive(false);
            if (rebuildRoutine != null)
                StopCoroutine(rebuildRoutine);
            rebuildRoutine = StartCoroutine(RebuildAfterSceneLoad());
        }

        private IEnumerator RebuildAfterSceneLoad()
        {
            // Wait for persistent-player duplicates to be removed and scene transforms to settle.
            yield return null;
            Rebuild(SceneManager.GetActiveScene());
            rebuildRoutine = null;
        }

        private void ClearMap()
        {
            foreach (GameObject item in generatedObjects)
            {
                if (item == null) continue;
                item.SetActive(false);
                Destroy(item);
            }
            generatedObjects.Clear();
            markers.Clear();
            playerMarker = null;
        }

        public void Rebuild(Scene scene)
        {
            ClearMap();
            player = FindFirstObjectByType<PlayerMovement2D>();
            var terrain = new List<Bounds>();
            var portals = new List<PortalManager>();
            var shops = new List<Shopkeeper>();
            var quests = new List<QuestGiver>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Collider2D collider in root.GetComponentsInChildren<Collider2D>())
                {
                    if (!collider.enabled || collider.isTrigger || collider.attachedRigidbody != null
                        || collider.GetComponentInParent<PlayerEntity>() != null
                        || collider.GetComponentInParent<QuestGiver>() != null
                        || collider.GetComponentInParent<Shopkeeper>() != null
                        || collider.GetComponentInParent<PortalManager>() != null)
                        continue;
                    if (collider.bounds.size.x > 0f && collider.bounds.size.y > 0f)
                        terrain.Add(collider.bounds);
                }
                portals.AddRange(root.GetComponentsInChildren<PortalManager>());
                shops.AddRange(root.GetComponentsInChildren<Shopkeeper>());
                quests.AddRange(root.GetComponentsInChildren<QuestGiver>());
            }
            if (terrain.Count == 0 || player == null)
            {
                panelRoot.SetActive(false);
                return;
            }

            Bounds bounds = terrain[0];
            foreach (Bounds item in terrain) bounds.Encapsulate(item);
            foreach (PortalManager item in portals) bounds.Encapsulate(item.transform.position);
            foreach (Shopkeeper item in shops) bounds.Encapsulate(item.transform.position);
            foreach (QuestGiver item in quests) bounds.Encapsulate(item.transform.position);
            worldBounds = Rect.MinMaxRect(bounds.min.x - mapPadding, bounds.min.y - mapPadding,
                bounds.max.x + mapPadding, bounds.max.y + mapPadding);
            panelRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
            Vector2 viewportSize = mapViewport.rect.size;
            float scale = Mathf.Min(viewportSize.x / worldBounds.width, viewportSize.y / worldBounds.height);
            contentSize = worldBounds.size * scale;
            mapContent.sizeDelta = contentSize;
            regionName.text = scene.name == "MovementPrototype" ? "마을 외곽"
                : scene.name == "PrologueVilage" ? "서막 마을" : scene.name;

            foreach (Bounds item in terrain)
            {
                Image image = CreateImage("Terrain", terrainColor, null, mapContent);
                RectTransform rect = image.rectTransform;
                rect.anchoredPosition = WorldToMapPosition(item.center, worldBounds, contentSize);
                rect.sizeDelta = new Vector2(Mathf.Max(2f, item.size.x * scale), Mathf.Max(2f, item.size.y * scale));
            }
            foreach (PortalManager item in portals)
                CreateMarker(item.transform, new Color(0.66f, 0.48f, 1f), 14f, "P", null);
            foreach (Shopkeeper item in shops)
                CreateMarker(item.transform, new Color(1f, 0.76f, 0.3f), 14f, "S", null);
            foreach (QuestGiver item in quests)
            {
                if (item.Quest != null)
                    CreateMarker(item.transform, Color.white, 20f, null, item);
            }
            playerMarker = CreateMarker(player.transform, new Color(0.25f, 0.92f, 1f), 11f, null, null).Rect;
            nextQuestRefresh = 0f;
            UpdateMarkers();
        }

        private Image CreateImage(string objectName, Color color, Sprite sprite, Transform parent)
        {
            var item = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            item.layer = gameObject.layer;
            item.transform.SetParent(parent, false);
            Image image = item.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = sprite != null;
            generatedObjects.Add(item);
            return image;
        }

        private Marker CreateMarker(Transform target, Color color, float size, string label, QuestGiver giver)
        {
            Image image = CreateImage("Marker " + target.name, color, giver == null ? markerSprite : null, mapContent);
            image.rectTransform.sizeDelta = Vector2.one * size;
            var marker = new Marker { Target = target, Rect = image.rectTransform, Image = image, QuestGiver = giver };
            markers.Add(marker);
            if (!string.IsNullOrEmpty(label))
            {
                var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.layer = gameObject.layer;
                textObject.transform.SetParent(image.transform, false);
                RectTransform rect = textObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                TMP_Text text = textObject.GetComponent<TMP_Text>();
                text.font = regionName.font;
                text.text = label;
                text.fontSize = 10f;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color(0.05f, 0.07f, 0.08f);
                text.raycastTarget = false;
            }
            return marker;
        }

        private void LateUpdate()
        {
            if (panelRoot.activeSelf) UpdateMarkers();
        }

        private void UpdateMarkers()
        {
            if (player == null) player = FindFirstObjectByType<PlayerMovement2D>();
            bool refreshQuests = Time.unscaledTime >= nextQuestRefresh;
            if (refreshQuests) nextQuestRefresh = Time.unscaledTime + 0.1f;
            foreach (Marker marker in markers)
            {
                if (marker.Rect == playerMarker)
                    marker.Target = player != null ? player.transform : null;
                if (marker.Target == null || !marker.Target.gameObject.activeInHierarchy)
                {
                    marker.Rect.gameObject.SetActive(false);
                    continue;
                }
                if (marker.QuestGiver != null)
                {
                    if (refreshQuests)
                    {
                        Sprite sprite = GetQuestSprite(marker.QuestGiver);
                        marker.Image.sprite = sprite;
                        marker.Rect.gameObject.SetActive(sprite != null);
                    }
                }
                else marker.Rect.gameObject.SetActive(true);
                Vector2 position = WorldToMapPosition(marker.Target.position, worldBounds, contentSize);
                Vector2 limit = (contentSize - marker.Rect.sizeDelta) * 0.5f;
                marker.Rect.anchoredPosition = new Vector2(
                    Mathf.Clamp(position.x, -Mathf.Max(0f, limit.x), Mathf.Max(0f, limit.x)),
                    Mathf.Clamp(position.y, -Mathf.Max(0f, limit.y), Mathf.Max(0f, limit.y)));
            }
        }

        private Sprite GetQuestSprite(QuestGiver giver)
        {
            if (!giver.isActiveAndEnabled || QuestManager.Instance == null) return null;
            if (giver.CanOfferQuest) return availableQuestSprite;
            QuestProgress progress = QuestManager.Instance.FindProgress(giver.Quest);
            if (progress == null) return null;
            if (progress.Status == QuestStatus.Active) return activeQuestSprite;
            return progress.Status == QuestStatus.ReadyToTurnIn ? readyQuestSprite : null;
        }

        public static Vector2 WorldToMapPosition(Vector2 position, Rect bounds, Vector2 size)
        {
            return new Vector2(
                (Mathf.InverseLerp(bounds.xMin, bounds.xMax, position.x) - 0.5f) * size.x,
                (Mathf.InverseLerp(bounds.yMin, bounds.yMax, position.y) - 0.5f) * size.y);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (rebuildRoutine != null) StopCoroutine(rebuildRoutine);
            rebuildRoutine = null;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}