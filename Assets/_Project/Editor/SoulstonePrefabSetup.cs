using System.IO;
using System.Linq;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EasternFantasy.EditorTools
{
    /// <summary>Creates the initial authorable prefab once. Verification never overwrites user edits.</summary>
    public static class SoulstonePrefabSetup
    {
        public const string PrefabPath = "Assets/_Project/Prefab/UI/SoulstoneWindow.prefab";
        private static readonly Color Gold = new Color(1f, .91f, .72f);

        [MenuItem("Eastern Fantasy/Soulstone/Open Editable UI Prefab")]
        public static void Open()
        {
            EnsurePrefab();
            AssetDatabase.OpenAsset(Resources.Load<SoulstoneCatalog>("SoulstoneCatalog").windowPrefab.gameObject);
        }

        public static void EnsurePrefab()
        {
            var catalog = Resources.Load<SoulstoneCatalog>("SoulstoneCatalog");
            if (catalog == null) throw new System.InvalidOperationException("Build the Soulstone catalog first.");
            EnsureTutorialAssets(catalog);
            EnsureInteractionPrompt(catalog);
            if (catalog.windowPrefab != null) return;
            foreach (var path in new[] { PrefabPath, "Assets/_Project/Prefabs/UI/SoulstoneWindow.prefab", "Assets/_Project/Resources/UI/Soulstone/SoulstoneWindow.prefab" })
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing == null) continue;
                catalog.windowPrefab = existing.GetComponent<SoulstoneWindowView>();
                EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); return;
            }
            var root = new GameObject("SoulstoneWindow", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                root.layer = LayerMask.NameToLayer("UI");
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 6400;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
                var view = root.AddComponent<SoulstoneWindowView>();
                var modal = Box("Modal", root.transform, new Rect(0, 0, 1920, 1080));
                Stretch(modal);
                var shade = modal.gameObject.AddComponent<Image>(); shade.color = new Color(0, 0, 0, .65f);
                view.modal = modal.gameObject;
                var panel = Box("Window", modal, new Rect(0, 0, 1560, 1020));
                panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
                panel.anchoredPosition = Vector2.zero; view.panel = panel;
                var backdrop = Box("Backdrop", panel, new Rect(110, 160, 1340, 788)).gameObject.AddComponent<Image>();
                backdrop.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Resources/UI/Soulstone/WindowBackground.png").OfType<Sprite>().FirstOrDefault();
                backdrop.color = backdrop.sprite != null ? Color.white : new Color(.018f, .02f, .028f, .98f); backdrop.raycastTarget = false;
                Picture("Overall Frame", panel, catalog.frame, new Rect(0, 0, 1560, 1020), true);
                Label("Heading", panel, "\uADC0\uD63C\uC11D", 34, new Rect(430, 147, 700, 52), catalog);

                // Contiguous content area: aligned top edges, 16 px column gutter, 10 px row gutters.
                var photo = Box("Map Image", panel, new Rect(114, 204, 918, 522));
                var backing = photo.gameObject.AddComponent<Image>();
                backing.color = new Color(.025f, .03f, .04f); backing.raycastTarget = false;
                var viewport = Box("Image Mask", photo, new Rect(7, 7, 904, 508));
                viewport.gameObject.AddComponent<RectMask2D>();
                view.preview = Picture("Map Screenshot", viewport, catalog.destinations[0].preview, new Rect(0, 0, 904, 508));
                view.preview.rectTransform.pivot = new Vector2(.5f, 0);
                view.previewAspect = view.preview.gameObject.AddComponent<AspectRatioFitter>();
                view.previewAspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                view.previewAspect.aspectRatio = view.preview.sprite.rect.width / view.preview.sprite.rect.height;
                var outline = Box("Map Frame", photo, new Rect(0, 0, 918, 522)).gameObject.AddComponent<MapNpcPortraitGraphic>();
                Stretch((RectTransform)outline.transform);
                outline.portrait = catalog.mapFrameOutline; outline.preserveAspect = false; outline.raycastTarget = false;

                var namePanel = Picture("Map Name", panel, catalog.mapNamePanel, new Rect(114, 734, 918, 86));
                view.mapName = Label("Name", namePanel.transform, catalog.destinations[0].displayName, 30, new Rect(48, 10, 822, 66), catalog);
                var npcPanel = Picture("NPC Panel", panel, catalog.npcPanel, new Rect(114, 830, 432, 112));
                view.npcContent = Box("NPC Icons", npcPanel.transform, new Rect(16, 7, 400, 98));
                var npcLayout = view.npcContent.gameObject.AddComponent<HorizontalLayoutGroup>();
                npcLayout.childAlignment = TextAnchor.MiddleCenter; npcLayout.spacing = 8;
                npcLayout.childControlWidth = npcLayout.childControlHeight = true;
                npcLayout.childForceExpandWidth = true; npcLayout.childForceExpandHeight = false;
                for (int i = 0; i < Mathf.Max(1, catalog.destinations[0].npcs.Length); i++)
                {
                    var slot = Box("NPC " + (i + 1), view.npcContent, new Rect(0, 0, 116, 96));
                    var element = slot.gameObject.AddComponent<LayoutElement>();
                    element.minWidth = 80; element.preferredWidth = 116; element.preferredHeight = 96;
                    var badge = Box("Badge", slot, new Rect(26, 1, 64, 64)).gameObject.AddComponent<MapNpcBadgeGraphic>();
                    CenterTop((RectTransform)badge.transform, 1); badge.raycastTarget = false;
                    var face = Box("Portrait", slot, new Rect(34, 9, 48, 48)).gameObject.AddComponent<MapNpcPortraitGraphic>();
                    CenterTop((RectTransform)face.transform, 9); face.raycastTarget = false;
                    var caption = Label("Name", slot, "", 16, new Rect(0, 66, 116, 28), catalog);
                    caption.rectTransform.anchorMin = Vector2.zero;
                    caption.rectTransform.anchorMax = Vector2.one;
                    caption.rectTransform.offsetMin = new Vector2(0, 2);
                    caption.rectTransform.offsetMax = new Vector2(0, -66);
                    var item = slot.gameObject.AddComponent<SoulstoneNpcSlot>(); item.portrait = face; item.caption = caption;
                    if (i < catalog.destinations[0].npcs.Length) item.Bind(catalog.destinations[0].npcs[i]);
                    view.npcSlots.Add(item);
                }
                view.noNpcs = Label("Empty NPC Message", npcPanel.transform, "\uB4F1\uB85D\uB41C NPC \uC5C6\uC74C", 20, new Rect(16, 7, 400, 98), catalog);
                view.noNpcs.gameObject.SetActive(false);

                var travel = MakeButton("Travel", panel, "", new Rect(554, 804, 478, 140), catalog);
                var travelImage = travel.GetComponent<Image>(); travelImage.sprite = catalog.travelNormal; travelImage.color = Color.white;
                travel.transition = Selectable.Transition.SpriteSwap;
                travel.spriteState = new SpriteState { highlightedSprite = catalog.travelHover, selectedSprite = catalog.travelHover,
                    pressedSprite = catalog.travelHover, disabledSprite = catalog.travelNormal };
                var travelRect = (RectTransform)travel.transform;
                travelRect.pivot = new Vector2(.5f, .5f); travelRect.anchoredPosition += new Vector2(239, -70);
                var travelLabel = travel.GetComponentInChildren<TMP_Text>();
                travelLabel.text = "\uC774\uB3D9";
                travelLabel.rectTransform.anchoredPosition = new Vector2(40, -43);
                travelLabel.rectTransform.sizeDelta = new Vector2(398, 52);
                view.travel = travel;
                view.travelStatus = Label("Description", travel.transform, "\uC120\uD0DD\uD55C \uC9C0\uC5ED\uC758 \uADC0\uD63C\uC11D\uC73C\uB85C \uC774\uB3D9\uD569\uB2C8\uB2E4.", 16, new Rect(40, 91, 398, 28), catalog);
                view.travelHover = travel.gameObject.AddComponent<MenuButtonHoverEffect>();
                view.travelHover.Configure(travelImage, 1.025f, Color.white);

                var list = Picture("Map List", panel, catalog.listPanel, new Rect(1048, 204, 398, 738), true);
                Label("Heading", list.transform, "\uC774\uB3D9 \uC9C0\uC5ED", 27, new Rect(36, 32, 326, 56), catalog);
                var listViewport = Box("Scroll View", list.transform, new Rect(30, 108, 338, 540));
                var hitArea = listViewport.gameObject.AddComponent<Image>(); hitArea.color = new Color(0, 0, 0, .01f);
                listViewport.gameObject.AddComponent<RectMask2D>();
                var scroll = listViewport.gameObject.AddComponent<ScrollRect>();
                scroll.horizontal = false; scroll.vertical = true; scroll.viewport = listViewport;
                scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 28;
                view.destinationContent = Box("Destinations", listViewport, new Rect(0, 0, 338, 540));
                var layout = view.destinationContent.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 6; layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
                var fitter = view.destinationContent.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scroll.content = view.destinationContent;
                for (int i = 0; i < catalog.destinations.Length; i++)
                {
                    var row = MakeButton("Destination " + catalog.destinations[i].sceneName, view.destinationContent,
                        catalog.destinations[i].displayName, new Rect(0, i * 94, 338, 88), catalog);
                    row.GetComponent<Image>().sprite = catalog.listSlot; row.GetComponent<Image>().color = Color.white;
                    row.GetComponent<Image>().preserveAspect = true;
                    var element = row.gameObject.AddComponent<LayoutElement>(); element.preferredHeight = 88;
                    var label = row.GetComponentInChildren<TMP_Text>(); label.fontSize = 24;
                    label.rectTransform.anchoredPosition += Vector2.up * 9;
                    view.destinationRows.Add(row);
                }
                Label("Close Hint", list.transform, "G / ESC \uB2EB\uAE30", 18, new Rect(30, 662, 338, 34), catalog);
                view.close = MakeButton("Close", panel, "\u00D7", new Rect(1396, 156, 46, 46), catalog);
                PolishPanels(root, catalog);
                Canvas.ForceUpdateCanvases();
                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
                catalog.windowPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath).GetComponent<SoulstoneWindowView>();
                EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
                Debug.Log("Editable Soulstone UI created: " + PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        public static void EnsureInteractionPrompt(SoulstoneCatalog catalog)
        {
            if (catalog.villagePromptTop <= 0f || catalog.templePromptTop <= 0f)
            {
                catalog.villagePromptTop = MeasurePromptTop(catalog.villageActivation);
                catalog.templePromptTop = MeasurePromptTop(catalog.templeActivation);
                EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            }
            if (catalog.interactionPromptPrefab != null) return;
            const string path = "Assets/_Project/Prefab/UI/SoulstoneInteractionPrompt.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing == null)
            {
                var npc = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/InteractionPrompt.prefab");
                var root = (GameObject)PrefabUtility.InstantiatePrefab(npc);
                try
                {
                    root.name = "SoulstoneInteractionPrompt";
                    var label = root.GetComponentInChildren<TMP_Text>(true);
                    label.text = "G"; label.name = "G";
                    PrefabUtility.RecordPrefabInstancePropertyModifications(label);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    existing = PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { Object.DestroyImmediate(root); }
            }
            catalog.interactionPromptPrefab = existing;
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        }

        private static float MeasurePromptTop(AnimationClip clip)
        {
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
            var sprite = (Sprite)AnimationUtility.GetObjectReferenceCurve(clip, binding)[0].value;
            var texture = new Texture2D(2,2);
            try
            {
                ImageConversion.LoadImage(texture, File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
                float scale = texture.width / (float)sprite.texture.width;
                var pixels = texture.GetPixels32();
                for (int y = Mathf.Min(texture.height-1,Mathf.CeilToInt(sprite.rect.yMax*scale)-1); y >= sprite.rect.yMin*scale; y--)
                    for (int x = Mathf.FloorToInt(sprite.rect.xMin*scale); x < sprite.rect.xMax*scale; x++)
                        if (pixels[y*texture.width+x].a > 64)
                            return ((y+1)/scale-sprite.rect.yMin-sprite.pivot.y)/sprite.pixelsPerUnit;
                throw new System.InvalidOperationException("Inactive stone has no visible pixels.");
            }
            finally { Object.DestroyImmediate(texture); }
        }

        public static void EnsureTutorialAssets(SoulstoneCatalog catalog)
        {
            if (catalog.villageActivation != null && catalog.villageLoop != null && catalog.templeActivation != null && catalog.templeLoop != null) return;
            catalog.villageActivation = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/RespornStone.anim");
            catalog.villageLoop = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/ActiveRespornStone.anim");
            catalog.templeActivation = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Old Temple/InActiveTempleRespornStone.anim");
            catalog.templeLoop = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Old Temple/TempleRespornStone.anim");
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        }

        public static void PolishPanels(GameObject root, SoulstoneCatalog catalog)
        {
            var view = root.GetComponent<SoulstoneWindowView>();
            var photo = view.panel.Find("Map Image");
            var mask = (RectTransform)photo.Find("Image Mask");
            photo.GetComponent<Image>().enabled = false;
            Inset(mask, 6, 6, 6, 6);
            Inset((RectTransform)photo.Find("Map Frame"), 0, 0, 0, 0);
            photo.Find("Map Frame").GetComponent<MapNpcPortraitGraphic>().preserveAspect = false;
            SoulstoneSystemSetup.RemoveCornerBackground(catalog.mapFrameOutline);

            // Match the left NPC frame and align both lower panels along their top and bottom edges.
            var npcPanel = (RectTransform)view.panel.Find("NPC Panel");
            var travelRect = (RectTransform)view.travel.transform;
            float left = travelRect.anchoredPosition.x - travelRect.rect.width * travelRect.pivot.x;
            travelRect.sizeDelta = new Vector2(travelRect.rect.width, npcPanel.rect.height);
            travelRect.anchoredPosition = new Vector2(left + travelRect.rect.width * travelRect.pivot.x,
                npcPanel.anchoredPosition.y - npcPanel.rect.height * (1-travelRect.pivot.y));
            var image = view.travel.GetComponent<Image>();
            image.sprite = npcPanel.GetComponent<Image>().sprite; image.overrideSprite = null;
            image.type = Image.Type.Simple; image.preserveAspect = false; image.color = Color.white;
            view.travel.transition = Selectable.Transition.ColorTint;
            view.travel.spriteState = new SpriteState();
            var colors = view.travel.colors; colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
            colors.pressedColor = new Color(.8f, .7f, .5f); colors.disabledColor = Color.white; view.travel.colors = colors;
            view.travelHover.Configure(image, 1.025f, new Color(1.15f, 1.08f, .94f));
            var caption = view.travel.transform.Find("Label").GetComponent<TMP_Text>();
            Inset(caption.rectTransform, 24, 12, 24, 48);
            Inset(view.travelStatus.rectTransform, 24, 62, 24, 14);

            // Trim the slot's transparent outer margin using Sprite metadata, then fill the list's inner width.
            catalog.listSlot = SoulstoneSystemSetup.SkinSprite("ListSlot.png", new Rect(92, 24, 524, 112), Vector4.zero);
            var list = view.panel.Find("Map List");
            var viewport = (RectTransform)list.Find("Scroll View");
            Inset(viewport, 44, 108, 44, 90);
            view.destinationContent.anchorMin = new Vector2(0, 1);
            view.destinationContent.anchorMax = Vector2.one;
            view.destinationContent.pivot = new Vector2(0, 1);
            view.destinationContent.anchoredPosition = Vector2.zero;
            view.destinationContent.sizeDelta = new Vector2(0, view.destinationContent.sizeDelta.y);
            view.destinationContent.GetComponent<VerticalLayoutGroup>().spacing = 14;
            foreach (var row in view.destinationRows)
            {
                var rowImage = row.GetComponent<Image>(); rowImage.sprite = catalog.listSlot;
                rowImage.type = Image.Type.Simple; rowImage.preserveAspect = false;
                row.GetComponent<LayoutElement>().preferredHeight = 72;
                Inset(row.GetComponentInChildren<TMP_Text>(true).rectTransform, 12, 0, 12, 0);
            }
            EditorUtility.SetDirty(catalog);
        }

        private static void Inset(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
        }

        private static RectTransform Box(string name, Transform parent, Rect bounds)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = LayerMask.NameToLayer("UI"); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(bounds.x, -bounds.y); rect.sizeDelta = bounds.size; return rect;
        }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static void CenterTop(RectTransform rect, float top)
        { rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -top); }
        private static Image Picture(string name, Transform parent, Sprite sprite, Rect bounds, bool sliced = false)
        {
            var image = Box(name, parent, bounds).gameObject.AddComponent<Image>(); image.sprite = sprite; image.raycastTarget = false;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple; return image;
        }
        private static TMP_Text Label(string name, Transform parent, string text, float size, Rect bounds, SoulstoneCatalog catalog)
        {
            var label = Box(name, parent, bounds).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = catalog.font; label.text = text; label.fontSize = size; label.color = Gold;
            label.alignment = TextAlignmentOptions.Midline; label.textWrappingMode = TextWrappingModes.Normal; label.raycastTarget = false; return label;
        }
        private static Button MakeButton(string name, Transform parent, string caption, Rect bounds, SoulstoneCatalog catalog)
        {
            var rect = Box(name, parent, bounds); var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0, 0, 0, .03f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1, .88f, .65f); colors.pressedColor = new Color(.7f, .5f, .3f); button.colors = colors;
            Label("Label", rect, caption, 30, new Rect(0, 0, bounds.width, bounds.height), catalog); return button;
        }
    }
}
