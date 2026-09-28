using System.Collections.Generic;
using System.Linq;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.EditorTools
{
    public static class InventoryVisualSetup
    {
        private const string SheetPath = "Assets/_Project/Art/UI/InventorySlotsAndTabs.png";
        private const string BackgroundPath = "Assets/_Project/Art/UI/InventoryWindowBackground.png";
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/InventoryWindow.prefab";

        private const string SlotNormal = "InventorySlot_Normal";
        private const string SlotHover = "InventorySlot_Hover";
        private const string SlotSelected = "InventorySlot_Selected";
        private const string SlotLocked = "InventorySlot_Locked";
        private const string SlotDisabled = "InventorySlot_Disabled";
        private const string TabNormal = "InventoryTab_Normal";
        private const string TabPressed = "InventoryTab_Pressed";
        private const string TabHover = "InventoryTab_Hover";
        private const string TabDisabled = "InventoryTab_Disabled";

        [MenuItem("Eastern Fantasy/UI/Apply Inventory Visual Assets")]
        public static void Apply()
        {
            ConfigureSpriteSheet();
            ConfigureBackground();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name, sprite => sprite);
            Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);

            if (background == null || !sprites.ContainsKey(SlotNormal) || !sprites.ContainsKey(TabNormal))
            {
                Debug.LogError("Inventory UI sprites could not be imported. Check the source image paths and slicing data.");
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ApplyTo(prefabRoot, background, sprites);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            foreach (InventoryWindowUI inventory in Resources.FindObjectsOfTypeAll<InventoryWindowUI>())
            {
                if (!inventory.gameObject.scene.IsValid() || EditorUtility.IsPersistent(inventory))
                    continue;

                ApplyTo(inventory.gameObject, background, sprites);
                EditorSceneManager.MarkSceneDirty(inventory.gameObject.scene);
            }

            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Inventory UI visual assets were applied to the prefab and open scene.");
        }

        private static void ConfigureSpriteSheet()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.spritePixelsPerUnit = 100f;

#pragma warning disable 618
            importer.spritesheet = new[]
            {
                Slice(SlotNormal,   46, 716, 250, 248),
                Slice(SlotHover,   321, 710, 252, 253),
                Slice(SlotSelected, 581, 686, 290, 300),
                Slice(SlotLocked,  881, 713, 248, 252),
                Slice(SlotDisabled, 1151, 713, 248, 252),
                Slice(TabNormal,    74, 516, 642, 140),
                Slice(TabPressed,  736, 516, 642, 140),
                Slice(TabHover,     74, 354, 642, 140),
                Slice(TabDisabled, 736, 354, 642, 140),
            };
#pragma warning restore 618
            importer.SaveAndReimport();
        }

        private static SpriteMetaData Slice(string name, float x, float y, float width, float height)
        {
            return new SpriteMetaData
            {
                name = name,
                rect = new Rect(x, y, width, height),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = new Vector4(24f, 24f, 24f, 24f)
            };
        }

        private static void ConfigureBackground()
        {
            TextureImporter importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.spritePixelsPerUnit = 100f;
            importer.SaveAndReimport();
        }

        private static void ApplyTo(GameObject root, Sprite background, IReadOnlyDictionary<string, Sprite> sprites)
        {
            InventoryWindowUI inventory = root.GetComponent<InventoryWindowUI>();
            if (inventory == null)
                inventory = root.GetComponentInChildren<InventoryWindowUI>(true);
            if (inventory == null)
                return;

            Transform panel = inventory.transform.Find("InventoryWindow");
            if (panel == null)
                return;

            RectTransform panelRect = panel as RectTransform;
            panelRect.sizeDelta = new Vector2(900f, 900f);
            Image panelImage = panel.GetComponent<Image>();
            panelImage.sprite = background;
            panelImage.type = Image.Type.Simple;
            panelImage.preserveAspect = false;
            panelImage.color = Color.white;

            SetRect(panel.Find("Title") as RectTransform, new Vector2(0f, 374f), new Vector2(600f, 54f));
            SetRect(panel.Find("Currency") as RectTransform, new Vector2(285f, 323f), new Vector2(220f, 42f));
            SetRect(panel.Find("CloseHint") as RectTransform, new Vector2(0f, -394f), new Vector2(500f, 36f));

            SetupTab(panel.Find("장비Tab"), new Vector2(-228f, 303f), sprites);
            SetupTab(panel.Find("소비Tab"), new Vector2(0f, 303f), sprites);
            SetupTab(panel.Find("기타Tab"), new Vector2(228f, 303f), sprites);

            Transform gridTransform = panel.Find("ItemGrid");
            SetRect(gridTransform as RectTransform, new Vector2(-160f, -45f), new Vector2(492f, 570f));
            GridLayoutGroup grid = gridTransform != null ? gridTransform.GetComponent<GridLayoutGroup>() : null;
            if (grid != null)
            {
                grid.cellSize = new Vector2(94f, 94f);
                grid.spacing = new Vector2(18f, 18f);
            }

            foreach (InventorySlotUI slot in inventory.GetComponentsInChildren<InventorySlotUI>(true))
            {
                Image image = slot.GetComponent<Image>();
                Button button = slot.GetComponent<Button>();
                if (image == null || button == null)
                    continue;

                image.sprite = sprites[SlotNormal];
                image.type = Image.Type.Simple;
                image.color = Color.white;
                ConfigureSpriteSwap(button, image, sprites[SlotHover], sprites[SlotSelected], sprites[SlotSelected], sprites[SlotNormal]);

                RectTransform icon = slot.transform.Find("Icon") as RectTransform;
                if (icon != null)
                    icon.sizeDelta = new Vector2(66f, 66f);
            }

            Transform tooltip = panel.Find("ItemTooltipController/ItemTooltip");
            SetRect(tooltip as RectTransform, new Vector2(284f, -47f), new Vector2(285f, 520f));

            foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.transform.parent != null && text.transform.parent.GetComponent<Button>() != null)
                    text.color = new Color(0.96f, 0.88f, 0.68f, 1f);
            }

            EditorUtility.SetDirty(inventory);
        }

        private static void SetupTab(Transform tab, Vector2 position, IReadOnlyDictionary<string, Sprite> sprites)
        {
            if (tab == null)
                return;

            SetRect(tab as RectTransform, position, new Vector2(205f, 52f));
            Image image = tab.GetComponent<Image>();
            Button button = tab.GetComponent<Button>();
            if (image == null || button == null)
                return;

            image.sprite = sprites[TabNormal];
            image.type = Image.Type.Simple;
            image.color = Color.white;
            // The selected category button is intentionally non-interactable in
            // InventoryWindowUI, so its disabled sprite doubles as selected state.
            ConfigureSpriteSwap(button, image, sprites[TabHover], sprites[TabPressed], sprites[TabHover], sprites[TabHover]);
        }

        private static void ConfigureSpriteSwap(
            Button button,
            Graphic target,
            Sprite highlighted,
            Sprite pressed,
            Sprite selected,
            Sprite disabled)
        {
            button.targetGraphic = target;
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState state = button.spriteState;
            state.highlightedSprite = highlighted;
            state.pressedSprite = pressed;
            state.selectedSprite = selected;
            state.disabledSprite = disabled;
            button.spriteState = state;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            button.colors = colors;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
