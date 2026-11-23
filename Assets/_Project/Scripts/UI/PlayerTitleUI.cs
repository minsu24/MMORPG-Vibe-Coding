using EasternFantasy.Inventory;
using EasternFantasy.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class PlayerTitleUI : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer character;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField, Min(0f)] private float titleAnchorHeight = 1.3125f;

        private PlayerInventory inventory;
        private PlayerEntity player;
        private ItemDefinition title;
        private Canvas canvas;
        private Image banner;
        private TextMeshProUGUI label;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
            player = GetComponent<PlayerEntity>();
        }

        private void OnEnable()
        {
            inventory.InventoryChanged += RefreshTitle;
            RefreshTitle();
        }

        private void RefreshTitle()
        {
            title = inventory.GetEquipped(EquipmentSlot.Title);
            if (title == null || title.TitleBanner == null) return;
            if (banner == null) CreateView();
            banner.sprite = title.TitleBanner;
            banner.rectTransform.sizeDelta = new Vector2(160f, 160f * banner.sprite.rect.height / banner.sprite.rect.width);
            label.text = title.DisplayName;
        }

        private void LateUpdate()
        {
            if (banner == null) return;
            var camera = Camera.main;
            bool visible = title != null && title.TitleBanner != null && character != null && character.enabled &&
                character.gameObject.activeInHierarchy && player != null && !player.IsDead && camera != null &&
                !GameplayHudVisibility.IsMenuScene && !SceneLoadingScreen.IsVisible && !CaveBellInteraction.IsViewing;
            Vector3 screen = Vector3.zero;
            if (visible)
            {
                Vector3 head = transform.TransformPoint(Vector3.up * titleAnchorHeight);
                screen = camera.WorldToScreenPoint(head);
                visible = screen.z > 0f && camera.pixelRect.Contains((Vector2)screen);
            }
            banner.gameObject.SetActive(visible);
            if (!visible) return;
            var canvasRect = (RectTransform)canvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 position);
            banner.rectTransform.anchoredPosition = position + Vector2.up * 1f;
        }

        private void CreateView()
        {
            // Screen-space keeps the text readable through zoom and independent of character flips.
            var root = new GameObject("Player Title Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            DontDestroyOnLoad(root);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 51;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            banner = new GameObject("Equipped Title", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            banner.transform.SetParent(root.transform, false);
            banner.rectTransform.pivot = new Vector2(0.5f, 0f);
            banner.raycastTarget = false;
            banner.gameObject.SetActive(false);

            label = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(banner.transform, false);
            label.rectTransform.anchorMin = new Vector2(0.18f, 0.29f);
            label.rectTransform.anchorMax = new Vector2(0.82f, 0.64f);
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.font = font;
            label.color = new Color32(255, 236, 195, 255);
            label.fontSize = 16f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 11f;
            label.fontSizeMax = 16f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.InventoryChanged -= RefreshTitle;
            if (banner != null) banner.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (canvas != null) Destroy(canvas.gameObject);
        }
    }
}
