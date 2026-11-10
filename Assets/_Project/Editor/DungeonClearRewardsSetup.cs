using System;
using System.Linq;
using EasternFantasy.Dungeon;
using EasternFantasy.Inventory;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class DungeonClearRewardsSetup
    {
        public const string DungeonPath = "Assets/_Project/Data/Dungeons/GiantGarden.asset";
        public const string FabricPath = "Assets/_Project/Data/Items/Fabric Piece.asset";
        public const string WindowPath = "Assets/_Project/Resources/DungeonClearWindow.prefab";
        private const string Art = "Assets/_Project/Art/UI/Dungeon/";
        private static readonly Color Gold = new Color32(241, 215, 160, 255);
        private static TMP_FontAsset font;

        [MenuItem("Eastern Fantasy/Dungeon/Setup Clear Rewards And Temporary Window")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run outside Play Mode.");
            foreach (string name in new[] { "BoarHorn", "GiantHeart" })
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/" + name + ".asset");
                var data = new SerializedObject(item);
                data.FindProperty("firstAcquisitionOnly").boolValue = true;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(item);
            }
            var fabric = AssetDatabase.LoadAssetAtPath<ItemDefinition>(FabricPath);
            // This is one cloth illustration, rather than an animation sheet. The old first slice was transparent.
            string iconPath = AssetDatabase.GetAssetPath(fabric.Icon);
            if (AssetImporter.GetAtPath(iconPath) is TextureImporter importer && importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
            var fabricData = new SerializedObject(fabric);
            fabricData.FindProperty("itemId").stringValue = "fabric_piece";
            fabricData.FindProperty("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            fabricData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fabric);
            var dungeon = AssetDatabase.LoadAssetAtPath<DungeonDefinition>(DungeonPath);
            dungeon.returnSceneName = "PrologueVilage"; dungeon.returnSpawnPointName = "In_Vilage_Portal";
            var rewards = (dungeon.clearRewards ?? Array.Empty<DungeonReward>()).Where(r => r != null && r.item != fabric).ToList();
            rewards.Add(new DungeonReward { item = fabric, quantity = 1 });
            dungeon.clearRewards = rewards.ToArray();
            if (!(dungeon.obtainableRewards ?? Array.Empty<DungeonRewardPreview>()).Any(r => r != null && r.item == fabric))
                dungeon.obtainableRewards = (dungeon.obtainableRewards ?? Array.Empty<DungeonRewardPreview>()).Concat(new[] { new DungeonRewardPreview { item = fabric } }).ToArray();
            EditorUtility.SetDirty(dungeon);
            RebuildWindow();
            AssetDatabase.SaveAssets();
            SaveAssetCatalogBuilder.Rebuild();
        }

        [MenuItem("Eastern Fantasy/Dungeon/Rebuild Clear Window Artwork")]
        public static void RebuildWindow()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run outside Play Mode.");
            var frame = Single("던전 클리어 전체 창 UI.png");
            var banner = Single("던전 클리어 문구.png");
            var rewardFrame = Single("던전 클리어 보상 UI.png");
            var buttons = ButtonSprites();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/GowunBatang-Bold SDF.asset");
            var root = new GameObject("Dungeon Clear Window", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(DungeonClearWindowUI));
            try
            {
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5500;
                var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
                var ui = root.GetComponent<DungeonClearWindowUI>();
                var blocker = Image("Window Root", root.transform, new Color(0, 0, 0, .4f));
                var full = blocker.rectTransform; full.anchorMin = Vector2.zero; full.anchorMax = Vector2.one; full.offsetMin = full.offsetMax = Vector2.zero;
                ui.windowRoot = blocker.gameObject;
                var panel = Image("Panel", blocker.transform, Color.white); panel.sprite = frame; panel.preserveAspect = true;
                Rect(panel.rectTransform, 0, 0, 880, 660);
                var heading = Image("Clear Heading", panel.transform, Color.white); heading.sprite = banner; heading.preserveAspect = true; heading.raycastTarget = false;
                Rect(heading.rectTransform, 0, 160, 530, 530f / 3f);
                ui.title = Text("Dungeon Name", panel.transform, "거인의 농장", 23); ui.title.color = Gold;
                Rect(ui.title.rectTransform, 0, 65, 620, 28);
                var rewards = Image("Rewards", panel.transform, Color.white); rewards.sprite = rewardFrame; rewards.preserveAspect = true; rewards.raycastTarget = false;
                Rect(rewards.rectTransform, 0, -55, 620, 620f / 3f);
                var content = new GameObject("Reward Items", typeof(RectTransform), typeof(HorizontalLayoutGroup)); content.transform.SetParent(rewards.transform, false);
                Rect((RectTransform)content.transform, 0, 15, 540, 100);
                var layout = content.GetComponent<HorizontalLayoutGroup>(); layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = layout.childControlHeight = false; layout.childForceExpandWidth = layout.childForceExpandHeight = false; layout.spacing = 16;
                ui.rewardContent = content.transform;
                var row = new GameObject("Reward Row Template", typeof(RectTransform)); row.transform.SetParent(content.transform, false);
                Rect((RectTransform)row.transform, 0, 0, 330, 100);
                var icon = Image("Icon", row.transform, Color.white); Rect(icon.rectTransform, -112, 0, 76, 76); icon.preserveAspect = true; icon.raycastTarget = false;
                var name = Text("Item And Quantity", row.transform, "천 조각 ×1", 26); name.color = Gold; Rect(name.rectTransform, 45, 0, 220, 90);
                ui.rewardRowTemplate = row; row.SetActive(false);
                var confirm = Button("Confirm", panel.transform, "확인", Color.white, 25); Rect(confirm.GetComponent<RectTransform>(), 0, -210, 250, 134);
                SetButtonSprites(confirm, buttons[2], buttons[3]); confirm.GetComponentInChildren<TMP_Text>().color = Gold;
                ui.confirmButton = confirm;
                var close = Button("Close", panel.transform, "", Color.white, 1); Rect(close.GetComponent<RectTransform>(), 374, 225, 64, 86);
                SetButtonSprites(close, buttons[0], buttons[1]);
                ui.closeButton = close;
                ui.message = Text("Reward Status", rewards.transform, "", 14); ui.message.color = Gold;
                Rect(ui.message.rectTransform, 0, -50, 530, 36);
                ui.windowRoot.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, WindowPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static TextureImporter Importer(string file, SpriteImportMode mode)
        {
            string path = Art + file;
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing dungeon clear artwork: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = mode;
            importer.maxTextureSize = 4096;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            return importer;
        }
        private static Sprite Single(string file)
        {
            var importer = Importer(file, SpriteImportMode.Single);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(Art + file);
        }
        private static Sprite[] ButtonSprites()
        {
            const string file = "던전 클리어 닫기 버튼 밑 확인 버튼 UI.png";
            var importer = Importer(file, SpriteImportMode.Multiple);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            string[] names = { "ClearCloseNormal", "ClearCloseHover", "ClearConfirmNormal", "ClearConfirmHover" };
            // Match the centers of the opaque artwork, rather than the unequal margins of the sheet.
            // Both states of each button use the same canvas dimensions and optical center.
            Rect[] crops = { new Rect(69, 177, 282, 322), new Rect(349.5f, 176.5f, 282, 322),
                new Rect(644, 172, 720, 360), new Rect(1385.5f, 169, 720, 360) };
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet = names.Select((name, index) => new SpriteMetaData
            {
                name = name, alignment = 0, pivot = new Vector2(.5f, .5f),
                rect = new Rect(crops[index].x, height - crops[index].y - crops[index].height, crops[index].width, crops[index].height)
            }).ToArray();
#pragma warning restore CS0618
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Art + file).OfType<Sprite>().ToArray();
            return names.Select(name => sprites.First(sprite => sprite.name == name)).ToArray();
        }
        private static void SetButtonSprites(Button button, Sprite normal, Sprite hover)
        {
            button.image.sprite = normal;
            button.image.preserveAspect = true;
            const string materialPath = Art + "DungeonClearButtons.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Eastern Fantasy/UI/Dungeon Clear Buttons");
                if (shader == null) throw new InvalidOperationException("Dungeon clear button shader is missing.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            button.image.material = material;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { highlightedSprite = hover, selectedSprite = normal, pressedSprite = hover, disabledSprite = normal };
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }
        private static Image Image(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>(); image.color = color; return image;
        }
        private static TMP_Text Text(string name, Transform parent, string text, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var value = go.GetComponent<TextMeshProUGUI>(); value.font = font; value.fontSize = size; value.color = Color.white;
            value.alignment = TextAlignmentOptions.Center; value.text = text; value.raycastTarget = false;
            return value;
        }
        private static Button Button(string name, Transform parent, string text, Color color, float size)
        {
            var image = Image(name, parent, color); var button = image.gameObject.AddComponent<Button>();
            var label = Text("Label", image.transform, text, size); var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(4, 0); rect.offsetMax = new Vector2(-4, 0);
            return button;
        }
        private static void Rect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
        }
    }
}
