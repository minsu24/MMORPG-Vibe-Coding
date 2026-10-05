using System;
using System.Linq;
using EasternFantasy.Dungeon;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class DungeonSystemSetup
    {
        private const string Art = "Assets/_Project/Art/UI/Dungeon/";
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/DungeonWindow.prefab";
        private static TMP_FontAsset font;
        private static readonly Color Gold = new Color(0.95f, 0.83f, 0.57f);
        private static readonly Color Ink = new Color(0.19f, 0.12f, 0.07f);

        [MenuItem("Eastern Fantasy/Setup Dungeon System")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode before setup.");
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "GapInSpace") throw new InvalidOperationException("Open GapInSpace before setup.");
            var portalObject = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Dungeon");
            if (portalObject == null) throw new InvalidOperationException("Dungeon portal is missing.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/GowunBatang-Bold SDF.asset");
            var prefab = BuildPrefab();
            var existing = scene.GetRootGameObjects().FirstOrDefault(g => g.GetComponent<DungeonWindowUI>() != null);
            if (existing == null) existing = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            else PrefabUtility.RevertPrefabInstance(existing, InteractionMode.AutomatedAction);
            var collider = portalObject.GetComponent<Collider2D>();
            if (collider == null) { var box = portalObject.AddComponent<BoxCollider2D>(); box.size = new Vector2(2f, 3f); collider = box; }
            collider.isTrigger = true;
            var portal = portalObject.GetComponent<DungeonPortal>();
            if (portal == null) portal = portalObject.AddComponent<DungeonPortal>();
            portal.Configure(existing.GetComponent<DungeonWindowUI>());
            EditorUtility.SetDirty(portal);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            const string destinationPath = "Assets/_Project/Scenes/GiantGarden.unity";
            var destination = SceneManager.GetSceneByPath(destinationPath);
            bool opened = !destination.isLoaded;
            if (opened) destination = EditorSceneManager.OpenScene(destinationPath, OpenSceneMode.Additive);
            if (!destination.GetRootGameObjects().Any(g => g.name == "DungeonSpawn"))
            {
                var spawn = new GameObject("DungeonSpawn");
                SceneManager.MoveGameObjectToScene(spawn, destination);
                spawn.transform.position = Vector3.zero;
                EditorSceneManager.MarkSceneDirty(destination);
                EditorSceneManager.SaveScene(destination);
            }
            if (opened) EditorSceneManager.CloseScene(destination, true);
            SceneManager.SetActiveScene(scene);
            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (var path in new[] { scene.path, destinationPath })
            {
                var entry = scenes.FirstOrDefault(s => s.path == path);
                if (entry == null) scenes.Add(new EditorBuildSettingsScene(path, true));
                else entry.enabled = true;
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Dungeon system installed: GapInSpace/Dungeon -> GiantGarden/DungeonSpawn.");
        }

        private static Sprite Slice(string file, string name, int x, int top, int width, int height)
        {
            string path = Art + file;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) { AssetDatabase.ImportAsset(path); importer = (TextureImporter)AssetImporter.GetAtPath(path); }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.maxTextureSize = 4096;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
#pragma warning disable CS0618
            var slices = importer.spritesheet.ToList();
            slices.RemoveAll(s => s.name == name);
            slices.Add(new SpriteMetaData { name = name, rect = new Rect(x, sourceHeight - top - height, width, height), pivot = new Vector2(0.5f, 0.5f), alignment = 0 });
            importer.spritesheet = slices.ToArray();
#pragma warning restore CS0618
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First(s => s.name == name);
        }

        private static GameObject BuildPrefab()
        {
            var frame = Slice("던전 UI 틀.png", "DungeonFrame", 0, 0, 1672, 941);
            var parchment = Slice("동양풍 판타지 던전 선택 화면.png", "Parchment", 100, 100, 980, 500);
            var normal = Slice("던전 UI 목록 기본.png", "ListNormal", 20, 100, 2140, 510);
            var hover = Slice("던전 UI 목록 호버.png", "ListHover", 20, 100, 2140, 510);
            var enter = Slice("던전 UI 입장 버튼.png", "EnterButton", 24, 164, 2125, 444);
            var track = Slice("던전 UI 스크롤 바.png", "ScrollTrack", 315, 1080, 95, 800);
            var handle = Slice("던전 UI 스크롤 바.png", "ScrollHandle", 315, 180, 95, 890);
            var arrowTop = Slice("던전 UI 스크롤 바.png", "ScrollTop", 310, 63, 104, 110);
            var arrowBottom = Slice("던전 UI 스크롤 바.png", "ScrollBottom", 310, 1938, 104, 139);
            var namePanel = Slice("동양 판타지 RPG UI 요소 시트.png", "NamePanel", 20, 110, 805, 277);
            var levelPanel = Slice("동양 판타지 RPG UI 요소 시트.png", "LevelPanel", 849, 81, 351, 137);
            var timePanel = Slice("동양 판타지 RPG UI 요소 시트.png", "TimePanel", 1201, 81, 320, 137);
            var monsterPanel = Slice("동양 판타지 RPG UI 요소 시트.png", "MonsterPanel", 1522, 81, 333, 137);
            var rewardPanel = Slice("동양 판타지 RPG UI 요소 시트.png", "RewardPanel", 846, 227, 1010, 185);
            var preview = Slice("거인의 농장_ 안개 속 거대 농원.png", "GiantGardenPreview", 0, 0, 1448, 1086);
            string dataPath = "Assets/_Project/Data/Dungeons/GiantGarden.asset";
            var dungeon = AssetDatabase.LoadAssetAtPath<DungeonDefinition>(dataPath);
            if (dungeon == null)
            {
                dungeon = ScriptableObject.CreateInstance<DungeonDefinition>();
                dungeon.displayName = "거인의 농장";
                dungeon.description = "안개 너머, 거대한 농원이 모습을 드러냅니다.";
                dungeon.sceneName = "GiantGarden";
                dungeon.spawnPointName = "DungeonSpawn";
                dungeon.preview = preview;
                AssetDatabase.CreateAsset(dungeon, dataPath);
            }
            var root = new GameObject("DungeonWindow", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(DungeonWindowUI));
            try
            {
                root.layer = 5;
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                root.GetComponent<Canvas>().sortingOrder = 500;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1800, 1010);
                scaler.matchWidthOrHeight = 0.5f;
                var ui = root.GetComponent<DungeonWindowUI>();
                ui.dungeons = new[] { dungeon };
                var overlay = Image("ModalBackdrop", root.transform, null, 0, 0, 1800, 1010);
                Stretch(overlay.rectTransform);
                overlay.color = new Color(0.015f, 0.015f, 0.02f, 0.82f);
                overlay.raycastTarget = true;
                ui.windowRoot = overlay.gameObject;
                var panel = Rect("Window", overlay.transform, 0, 0, 1672, 941);
                panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
                panel.anchoredPosition = Vector2.zero;
                Image("LeftParchment", panel, parchment, 87, 83, 1000, 595);
                var infoBG = Image("InformationBackground", panel, null, 87, 675, 1000, 186);
                infoBG.color = new Color(0.035f, 0.039f, 0.037f);
                var rightBG = Image("ListBackground", panel, null, 1123, 83, 470, 778);
                rightBG.color = new Color(0.035f, 0.03f, 0.023f);
                var viewport = Rect("PreviewViewport", panel, 96, 89, 982, 570);
                viewport.gameObject.AddComponent<RectMask2D>();
                ui.preview = Image("DungeonPreview", viewport, preview, 0, 0, 982, 570);
                ui.preview.rectTransform.anchorMin = ui.preview.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ui.preview.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                ui.preview.rectTransform.anchoredPosition = Vector2.zero;
                var fitter = ui.preview.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = 1448f / 1086f;
                // AspectRatioFitter's ratio is updated when the selected preview changes.
                var footer = Image("PreviewCaptionBackground", viewport, null, 0, 512, 982, 58);
                footer.color = new Color(0.035f, 0.025f, 0.015f, 0.8f);
                ui.description = Label("Description", viewport, dungeon.description, 22, 518, 935, 46, 24, Gold);

                Image("NamePanel", panel, namePanel, 95, 687, 355, 158);
                var emblem = Image("DungeonEmblem", panel, preview, 132, 738, 68, 68);
                emblem.preserveAspect = true;
                ui.dungeonEmblem = emblem;
                ui.dungeonName = Label("DungeonName", panel, dungeon.displayName, 217, 711, 210, 71, 28, Gold);
                Label("DungeonCategory", panel, "일반 던전", 217, 786, 205, 30, 19, Gold);
                Image("LevelPanel", panel, levelPanel, 465, 683, 197, 58);
                Image("TimePanel", panel, timePanel, 669, 683, 197, 58);
                Image("MonsterPanel", panel, monsterPanel, 871, 683, 197, 58);
                ui.levelText = Label("Level", panel, "", 540, 695, 116, 35, 17, Gold);
                ui.timeText = Label("Time", panel, "", 741, 695, 119, 35, 16, Gold);
                ui.monsterText = Label("Monsters", panel, "", 945, 695, 117, 35, 16, Gold);
                Image("RewardsPanel", panel, rewardPanel, 465, 774, 605, 75);
                ui.rewardIcons = new Image[6];
                ui.rewardAmounts = new TMP_Text[6];
                for (int i = 0; i < 6; i++)
                {
                    ui.rewardIcons[i] = Image("RewardIcon" + i, panel, null, 593 + i * 76, 791, 40, 40);
                    ui.rewardIcons[i].enabled = false;
                    ui.rewardAmounts[i] = Label("RewardCount" + i, panel, "", 587 + i * 76, 824, 59, 18, 14, Gold);
                }
                ui.rewardText = Label("RewardSummary", panel, "", 473, 745, 596, 25, 16, Gold);

                var scroll = Rect("DungeonList", panel, 1125, 97, 433, 650);
                ui.list = scroll.gameObject.AddComponent<ScrollRect>();
                ui.list.horizontal = false;
                ui.list.vertical = true;
                ui.list.movementType = ScrollRect.MovementType.Clamped;
                ui.list.scrollSensitivity = 42;
                var listViewport = Image("Viewport", scroll, null, 0, 0, 433, 650);
                listViewport.color = Color.clear;
                listViewport.raycastTarget = true;
                listViewport.gameObject.AddComponent<RectMask2D>();
                ui.list.viewport = listViewport.rectTransform;
                var content = Rect("Content", listViewport.transform, 0, 0, 433, 0);
                content.anchorMin = new Vector2(0, 1);
                content.anchorMax = Vector2.one;
                content.pivot = new Vector2(0.5f, 1);
                content.sizeDelta = new Vector2(0, 0);
                content.anchoredPosition = Vector2.zero;
                var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 10;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                var contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
                contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                ui.list.content = content;
                var row = Image("EntryTemplate", content, normal, 0, 0, 433, 103);
                row.raycastTarget = true;
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 103;
                var entry = row.gameObject.AddComponent<DungeonListEntryUI>();
                entry.background = row;
                entry.button = row.gameObject.AddComponent<Button>();
                entry.button.targetGraphic = row;
                entry.button.transition = Selectable.Transition.None;
                entry.normalSprite = normal;
                entry.highlightSprite = hover;
                entry.thumbnail = Image("Thumbnail", row.transform, preview, 30, 24, 57, 57);
                entry.thumbnail.preserveAspect = true;
                entry.title = Label("Name", row.transform, "", 111, 22, 198, 56, 22, Ink);
                entry.state = Label("State", row.transform, "", 323, 34, 55, 37, 17, Ink);
                entry.title.alignment = TextAlignmentOptions.Midline;
                entry.state.alignment = TextAlignmentOptions.Center;
                ui.entryTemplate = entry;
                row.gameObject.SetActive(false);

                var scrollbarImage = Image("Scrollbar", panel, track, 1566, 135, 22, 567);
                scrollbarImage.raycastTarget = true;
                var scrollbar = scrollbarImage.gameObject.AddComponent<Scrollbar>();
                var sliding = Rect("SlidingArea", scrollbarImage.transform, 0, 0, 22, 567);
                Stretch(sliding);
                var handleImage = Image("Handle", sliding, handle, 0, 0, 22, 170);
                scrollbar.handleRect = handleImage.rectTransform;
                scrollbar.targetGraphic = handleImage;
                scrollbar.direction = Scrollbar.Direction.BottomToTop;
                ui.list.verticalScrollbar = scrollbar;
                ui.list.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                Image("ScrollTop", panel, arrowTop, 1566, 106, 22, 29);
                Image("ScrollBottom", panel, arrowBottom, 1566, 702, 22, 31);
                var buttonImage = Image("EnterDungeon", panel, enter, 1134, 765, 458, 92);
                buttonImage.raycastTarget = true;
                ui.enterButton = buttonImage.gameObject.AddComponent<Button>();
                ui.enterButton.targetGraphic = buttonImage;
                UnityEditor.Events.UnityEventTools.AddPersistentListener(ui.enterButton.onClick, ui.EnterSelected);
                Label("EnterLabel", buttonImage.transform, "던전 입장", 15, 10, 425, 69, 32, Gold).alignment = TextAlignmentOptions.Center;
                ui.message = Label("Status", panel, "", 1135, 855, 450, 28, 16, Gold);
                Image("Frame", panel, frame, 0, 0, 1672, 941);
                var close = Image("Close", panel, null, 1591, 24, 39, 35);
                close.color = new Color(0.065f, 0.04f, 0.02f, 0.94f);
                close.raycastTarget = true;
                ui.closeButton = close.gameObject.AddComponent<Button>();
                ui.closeButton.targetGraphic = close;
                UnityEditor.Events.UnityEventTools.AddPersistentListener(ui.closeButton.onClick, ui.Close);
                Label("CloseLabel", close.transform, "×", 0, 0, 39, 35, 30, Gold).alignment = TextAlignmentOptions.Center;
                overlay.gameObject.SetActive(false);
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }
        private static Image Image(string name, Transform parent, Sprite sprite, float x, float y, float w, float h)
        {
            var rect = Rect(name, parent, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }
        private static TMP_Text Label(string name, Transform parent, string text, float x, float y, float w, float h, float size, Color color)
        {
            var rect = Rect(name, parent, x, y, w, h);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableAutoSizing = true;
            label.fontSizeMin = size * 0.75f;
            label.fontSizeMax = size;
            label.raycastTarget = false;
            return label;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
