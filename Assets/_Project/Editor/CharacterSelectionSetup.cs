using System.Linq;
using EasternFantasy.CharacterSelection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class CharacterSelectionSetup
    {
        private const string DataFolder = "Assets/_Project/Data/Characters";
        private const string FontPath = "Assets/_Project/Fonts/GowunBatang-Bold SDF.asset";
        private const string DosaPortraitPath = "Assets/_Project/Art/도사 캐릭터 이미지.png";
        private const string MonkPortraitPath = "Assets/_Project/Art/CharacterSelect_Monk.png";
        private const string SwordswomanPortraitPath = "Assets/_Project/Art/CharacterSelect_Swordswoman.png";
        private const string BackgroundPath = "Assets/_Project/Art/게임 배경.png";

        private static readonly Color Ink = new Color(0.025f, 0.03f, 0.04f, 0.96f);
        private static readonly Color Panel = new Color(0.035f, 0.045f, 0.055f, 0.93f);
        private static readonly Color Gold = new Color(0.93f, 0.72f, 0.28f, 1f);
        private static readonly Color Ivory = new Color(0.95f, 0.92f, 0.82f, 1f);

        [MenuItem("Eastern Fantasy/Setup Character Selection Scene")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "ChooseCharactor")
                throw new System.InvalidOperationException("Open ChooseCharactor before running this setup.");

            ImportPortrait(MonkPortraitPath);
            ImportPortrait(SwordswomanPortraitPath);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            CharacterClassDefinition dosa = CreateDefinition(
                "Dosa", CharacterClassId.Dosa, "도사", "원거리 · 술법가",
                "부적과 술법으로 먼 거리에서 적을 제압합니다. 높은 사거리와 안정적인 기동성을 활용해 피해를 피하며 전투합니다.",
                LoadSprite(DosaPortraitPath), true, 4, 2, 4, 5);
            CharacterClassDefinition monk = CreateDefinition(
                "Monk", CharacterClassId.Monk, "수도승", "근접 · 전사",
                "단단한 육체와 권법으로 전선을 지킵니다. 세 직업 중 가장 높은 방어력을 바탕으로 적의 공격을 버티는 근접 전사입니다.",
                LoadSprite(MonkPortraitPath), false, 3, 5, 3, 1);
            CharacterClassDefinition swordswoman = CreateDefinition(
                "Swordswoman", CharacterClassId.Swordswoman, "여검사", "근접 · 딜러",
                "빠른 검술로 짧은 시간에 강한 피해를 줍니다. 높은 공격력과 기동성을 지녔지만 방어에는 주의가 필요한 근접 딜러입니다.",
                LoadSprite(SwordswomanPortraitPath), false, 5, 3, 5, 1);

            GameObject oldRoot = FindRoot(scene, "CharacterSelectionUI");
            if (oldRoot != null)
                Object.DestroyImmediate(oldRoot);

            GameObject root = CreateSceneObject(scene, "CharacterSelectionUI", typeof(RectTransform));
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            CharacterSelectionController controller = root.AddComponent<CharacterSelectionController>();

            CreateBackground(root.transform);
            CreateImage("Dim", root.transform, new Color(0.015f, 0.02f, 0.025f, 0.62f), true);

            TMP_Text title = CreateText("Title", root.transform, "직업 선택", font, 54f, Gold);
            SetRect(title.rectTransform, new Vector2(0f, 463f), new Vector2(900f, 76f));
            TMP_Text subtitle = CreateText("Subtitle", root.transform, "하나의 직업에는 하나의 캐릭터만 생성할 수 있습니다", font, 22f, Ivory);
            subtitle.color = new Color(Ivory.r, Ivory.g, Ivory.b, 0.72f);
            SetRect(subtitle.rectTransform, new Vector2(0f, 411f), new Vector2(1000f, 42f));

            CharacterSelectionCard[] cards =
            {
                CreateCard(root.transform, controller, dosa, -500f, font),
                CreateCard(root.transform, controller, monk, 0f, font),
                CreateCard(root.transform, controller, swordswoman, 500f, font)
            };

            GameObject details = CreateImage("DetailsPanel", root.transform, Panel, false);
            SetRect(details.GetComponent<RectTransform>(), new Vector2(0f, -407f), new Vector2(1640f, 190f));
            Outline detailsOutline = details.AddComponent<Outline>();
            detailsOutline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.62f);
            detailsOutline.effectDistance = new Vector2(2f, -2f);

            TMP_Text className = CreateText("ClassName", details.transform, "도사", font, 34f, Gold);
            className.alignment = TextAlignmentOptions.Left;
            SetRect(className.rectTransform, new Vector2(-655f, 52f), new Vector2(250f, 50f));
            TMP_Text role = CreateText("Role", details.transform, "원거리 · 술법가", font, 24f, Ivory);
            role.alignment = TextAlignmentOptions.Left;
            SetRect(role.rectTransform, new Vector2(-655f, 8f), new Vector2(250f, 42f));
            TMP_Text description = CreateText("Description", details.transform, string.Empty, font, 20f, Ivory);
            description.alignment = TextAlignmentOptions.TopLeft;
            description.textWrappingMode = TextWrappingModes.Normal;
            SetRect(description.rectTransform, new Vector2(-230f, 17f), new Vector2(560f, 115f));
            TMP_Text stats = CreateText("Stats", details.transform, string.Empty, font, 20f, Gold);
            stats.alignment = TextAlignmentOptions.Left;
            SetRect(stats.rectTransform, new Vector2(440f, 35f), new Vector2(740f, 70f));
            TMP_Text status = CreateText("Status", details.transform, string.Empty, font, 18f, Ivory);
            status.alignment = TextAlignmentOptions.Left;
            SetRect(status.rectTransform, new Vector2(440f, -42f), new Vector2(740f, 55f));

            Button back = CreateButton("BackButton", root.transform, "뒤로", font);
            RectTransform backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 1f);
            backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(42f, -38f);
            backRect.sizeDelta = new Vector2(150f, 54f);
            back.onClick.AddListener(controller.ReturnToMainMenu);

            SerializedObject controllerData = new SerializedObject(controller);
            SerializedProperty cardArray = controllerData.FindProperty("cards");
            cardArray.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
                cardArray.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            controllerData.FindProperty("classNameText").objectReferenceValue = className;
            controllerData.FindProperty("roleText").objectReferenceValue = role;
            controllerData.FindProperty("descriptionText").objectReferenceValue = description;
            controllerData.FindProperty("statsText").objectReferenceValue = stats;
            controllerData.FindProperty("statusText").objectReferenceValue = status;
            controllerData.ApplyModifiedPropertiesWithoutUndo();

            if (FindRoot(scene, "EventSystem") == null)
            {
                GameObject eventSystem = CreateSceneObject(
                    scene,
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("ChooseCharactor selection UI is ready.");
        }

        private static CharacterSelectionCard CreateCard(
            Transform parent,
            CharacterSelectionController controller,
            CharacterClassDefinition definition,
            float x,
            TMP_FontAsset font)
        {
            GameObject cardRoot = CreateImage(definition.name + "Card", parent, Ink, false);
            SetRect(cardRoot.GetComponent<RectTransform>(), new Vector2(x, 55f), new Vector2(420f, 660f));
            Outline outline = cardRoot.AddComponent<Outline>();
            outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.78f);
            outline.effectDistance = new Vector2(3f, -3f);

            Image portrait = CreateImage("Portrait", cardRoot.transform, Color.white, false).GetComponent<Image>();
            portrait.sprite = definition.Portrait;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            SetRect(portrait.rectTransform, new Vector2(0f, 58f), new Vector2(390f, 520f));

            GameObject footer = CreateImage("Footer", cardRoot.transform, new Color(0.02f, 0.025f, 0.03f, 0.92f), false);
            RectTransform footerRect = footer.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0f, 0f);
            footerRect.anchorMax = new Vector2(1f, 0f);
            footerRect.pivot = new Vector2(0.5f, 0f);
            footerRect.anchoredPosition = Vector2.zero;
            footerRect.sizeDelta = new Vector2(0f, 145f);

            TMP_Text name = CreateText("ClassName", footer.transform, definition.DisplayName, font, 34f, Gold);
            SetRect(name.rectTransform, new Vector2(0f, 96f), new Vector2(370f, 45f));
            TMP_Text role = CreateText("Role", footer.transform, definition.CombatRole, font, 21f, Ivory);
            SetRect(role.rectTransform, new Vector2(0f, 57f), new Vector2(370f, 35f));
            TMP_Text availability = CreateText("Availability", footer.transform, definition.Playable ? "선택" : "준비 중", font, 18f, Ivory);
            SetRect(availability.rectTransform, new Vector2(0f, 20f), new Vector2(370f, 30f));

            CharacterSelectionCard card = cardRoot.AddComponent<CharacterSelectionCard>();
            SerializedObject cardData = new SerializedObject(card);
            cardData.FindProperty("definition").objectReferenceValue = definition;
            cardData.FindProperty("portraitImage").objectReferenceValue = portrait;
            cardData.FindProperty("cardBackground").objectReferenceValue = cardRoot.GetComponent<Image>();
            cardData.FindProperty("classNameText").objectReferenceValue = name;
            cardData.FindProperty("roleText").objectReferenceValue = role;
            cardData.FindProperty("availabilityText").objectReferenceValue = availability;
            cardData.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }

        private static CharacterClassDefinition CreateDefinition(
            string assetName,
            CharacterClassId classId,
            string displayName,
            string role,
            string description,
            Sprite portrait,
            bool playable,
            int attack,
            int defense,
            int mobility,
            int range)
        {
            EnsureFolder(DataFolder);
            string path = DataFolder + "/" + assetName + ".asset";
            CharacterClassDefinition definition = AssetDatabase.LoadAssetAtPath<CharacterClassDefinition>(path);
            if (definition != null)
                return definition;

            definition = ScriptableObject.CreateInstance<CharacterClassDefinition>();
            SerializedObject data = new SerializedObject(definition);
            data.FindProperty("classId").enumValueIndex = (int)classId;
            data.FindProperty("displayName").stringValue = displayName;
            data.FindProperty("combatRole").stringValue = role;
            data.FindProperty("description").stringValue = description;
            data.FindProperty("portrait").objectReferenceValue = portrait;
            data.FindProperty("playable").boolValue = playable;
            data.FindProperty("attack").intValue = attack;
            data.FindProperty("defense").intValue = defense;
            data.FindProperty("mobility").intValue = mobility;
            data.FindProperty("range").intValue = range;
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(definition, path);
            return definition;
        }

        private static void ImportPortrait(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }

        private static void CreateBackground(Transform parent)
        {
            GameObject background = CreateImage("Background", parent, Color.white, true);
            Image image = background.GetComponent<Image>();
            image.sprite = LoadSprite(BackgroundPath);
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        private static Button CreateButton(string name, Transform parent, string label, TMP_FontAsset font)
        {
            GameObject root = CreateImage(name, parent, new Color(0.05f, 0.065f, 0.075f, 0.98f), false);
            Button button = root.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Gold;
            colors.pressedColor = new Color(0.7f, 0.52f, 0.2f, 1f);
            button.colors = colors;
            TMP_Text text = CreateText("Label", root.transform, label, font, 22f, Ivory);
            Stretch(text.rectTransform, 8f, 6f, 8f, 6f);
            return button;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            TMP_FontAsset font,
            float size,
            Color color)
        {
            GameObject root = CreateUiObject(name, parent);
            TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color, bool stretch)
        {
            GameObject root = CreateUiObject(name, parent);
            Image image = root.AddComponent<Image>();
            image.color = color;
            if (stretch)
                Stretch(root.GetComponent<RectTransform>());
            return root;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            root.transform.SetParent(parent, false);
            return root;
        }

        private static GameObject CreateSceneObject(Scene scene, string name, params System.Type[] components)
        {
            GameObject root = new GameObject(name, components);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }
    }
}
