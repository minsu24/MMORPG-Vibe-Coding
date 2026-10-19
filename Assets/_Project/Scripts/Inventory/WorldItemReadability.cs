using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EasternFantasy.Inventory
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WorldItemPickup))]
    public sealed class WorldItemReadability : MonoBehaviour
    {
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private Material decorationMaterial;
        [SerializeField] private TMP_FontAsset labelFont;
        private static Sprite squareSprite;
        private static Sprite borderSprite;
        private GameObject visualRoot;
        private GameObject nameRoot;
        private SpriteRenderer background;
        private SpriteRenderer border;
        private bool collected;
        private Color gradeColor;

        public bool IsNameVisible => nameRoot != null && nameRoot.activeSelf;

        public void Initialize(ItemDefinition item, SpriteRenderer icon, float size, int quantity)
        {
            if (visualRoot != null) Destroy(visualRoot);
            collected = false;
            EnsureSprites();
            if (outlineMaterial != null) icon.sharedMaterial = outlineMaterial;
            // Sprite sheets and standalone icons can share the contour material.
            // Bind each renderer's texture explicitly so batching cannot reuse another item's atlas.
            if (icon.sprite != null) {
                var properties = new MaterialPropertyBlock();
                icon.GetPropertyBlock(properties);
                properties.SetTexture("_MainTex", icon.sprite.texture);
                icon.SetPropertyBlock(properties);
            }
            gradeColor = item.HasGrade ? item.GradeColor : new Color(.75f, .8f, .85f);
            visualRoot = new GameObject("DropReadability");
            visualRoot.transform.SetParent(transform, false);
            visualRoot.transform.localPosition = icon.sprite != null ? (Vector3)icon.sprite.bounds.center : Vector3.zero;
            visualRoot.transform.localScale = Vector3.one / Mathf.Max(.0001f, Mathf.Abs(transform.lossyScale.x));
            background = Decoration("Background", visualRoot.transform, new Vector2(size, size), new Color(.025f, .035f, .055f, .78f), icon, -2, squareSprite);
            border = Decoration("GradeBorder", visualRoot.transform, new Vector2(size, size), gradeColor, icon, -1, borderSprite);
            nameRoot = new GameObject("HoverName");
            nameRoot.transform.SetParent(visualRoot.transform, false);
            nameRoot.transform.localPosition = new Vector3(0, size * .5f + .29f, 0);
            var textObject = new GameObject("Text", typeof(TextMeshPro));
            textObject.transform.SetParent(nameRoot.transform, false);
            var text = textObject.GetComponent<TextMeshPro>();
            if (labelFont != null) text.font = labelFont;
            text.text = item.GradedDisplayName + (quantity > 1 ? " x" + quantity : "");
            text.fontSize = 3f;
            text.color = gradeColor;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(6, .48f);
            var textRenderer = text.GetComponent<MeshRenderer>();
            textRenderer.sortingLayerID = icon.sortingLayerID;
            textRenderer.sortingOrder = icon.sortingOrder + 21;
            text.ForceMeshUpdate();
            Decoration("NameBackground", nameRoot.transform, new Vector2(Mathf.Clamp(text.preferredWidth + .28f, 1.2f, 6f), .48f), new Color(.015f, .02f, .035f, .94f), icon, 20, squareSprite);
            nameRoot.SetActive(false);
        }

        private SpriteRenderer Decoration(string objectName, Transform parent, Vector2 size, Color color,
            SpriteRenderer icon, int order, Sprite sprite)
        {
            var child = new GameObject(objectName, typeof(SpriteRenderer));
            child.transform.SetParent(parent, false);
            child.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = child.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerID = icon.sortingLayerID;
            renderer.sortingOrder = icon.sortingOrder + order;
            if (decorationMaterial != null) renderer.sharedMaterial = decorationMaterial;
            return renderer;
        }

        private static void EnsureSprites()
        {
            if (squareSprite != null && borderSprite != null) return;
            var solid = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "Loot Backplate", filterMode = FilterMode.Point };
            solid.SetPixel(0, 0, Color.white); solid.Apply();
            squareSprite = Sprite.Create(solid, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            const int resolution = 64;
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false) { name = "Loot Grade Border", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[resolution * resolution];
            for (int y = 0; y < resolution; y++)
                for (int x = 0; x < resolution; x++)
                    pixels[y * resolution + x] = new Color32(255, 255, 255, (byte)(x < 2 || x >= resolution - 2 || y < 2 || y >= resolution - 2 ? 255 : 0));
            texture.SetPixels32(pixels); texture.Apply();
            borderSprite = Sprite.Create(texture, new Rect(0, 0, resolution, resolution), Vector2.one * .5f, resolution);
        }

        private void LateUpdate()
        {
            if (collected || background == null || Mouse.current == null) { ShowName(false); return; }
            Camera camera = Camera.main;
            if (camera == null || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) { ShowName(false); return; }
            Bounds bounds = background.bounds;
            Vector3 min = camera.WorldToScreenPoint(bounds.min), max = camera.WorldToScreenPoint(bounds.max);
            bool hovered = min.z > 0 && Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y)).Contains(Mouse.current.position.ReadValue());
            ShowName(hovered);
        }

        public void ShowName(bool visible)
        {
            if (nameRoot != null) nameRoot.SetActive(visible && !collected);
            if (border != null) border.color = visible ? Color.Lerp(gradeColor, Color.white, .35f) : gradeColor;
        }

        public void OnCollected()
        {
            collected = true;
            if (visualRoot != null) visualRoot.SetActive(false);
        }

        private void OnDisable() => ShowName(false);
    }
}
