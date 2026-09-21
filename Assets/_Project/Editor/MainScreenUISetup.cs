using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class MainScreenUISetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/MainScreen.unity";
        private const string FontPath = "Assets/_Project/Fonts/DNFBitBitv2SDF32 SDF 1.asset";
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string SettingsPrefabPath = PrefabFolder + "/SettingsMenu.prefab";

        private static readonly Color PanelColor = new Color(0.025f, 0.045f, 0.06f, 0.82f);
        private static readonly Color ButtonColor = new Color(0.08f, 0.13f, 0.16f, 0.94f);
        private static readonly Color GoldColor = new Color(0.91f, 0.71f, 0.33f, 1f);
        private static readonly Color IvoryColor = new Color(0.95f, 0.93f, 0.84f, 1f);

        [MenuItem("Eastern Fantasy/Setup Main Screen UI")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (GameObject.Find("MainMenuCanvas") != null)
            {
                Debug.Log("MainScreen UI already exists. Setup was not applied again.");
                return;
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
                throw new MissingReferenceException($"TMP font was not found at {FontPath}.");

            Canvas canvas = CreateCanvas();
            SettingsMenuController settings = CreateSettingsMenu(canvas.transform, font);
            Button startButton = CreateMainMenu(canvas.transform, font, settings);
            CreateEventSystem(startButton.gameObject);

            EnsureFolder(PrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(settings.gameObject, SettingsPrefabPath);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Created MainScreen Canvas, menu buttons, settings UI, and reusable SettingsMenu prefab.");
        }

        private static Canvas CreateCanvas()
        {
            GameObject root = new GameObject(
                "MainMenuCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static Button CreateMainMenu(
            Transform canvas,
            TMP_FontAsset font,
            SettingsMenuController settings)
        {
            GameObject panel = CreateImage("MainMenuPanel", canvas, PanelColor);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0.5f);
            panelRect.anchorMax = new Vector2(1f, 0.5f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(-95f, 0f);
            panelRect.sizeDelta = new Vector2(455f, 650f);
            AddOutline(panel, new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.45f), new Vector2(2f, -2f));

            TMP_Text title = CreateText("Title", panel.transform, "MMORPG", font, 64f, IvoryColor);
            SetRect(title.rectTransform, new Vector2(0f, 245f), new Vector2(390f, 90f));
            title.fontStyle = FontStyles.Bold;

            TMP_Text subtitle = CreateText(
                "Subtitle",
                panel.transform,
                "A NEW JOURNEY AWAITS",
                font,
                24f,
                new Color(IvoryColor.r, IvoryColor.g, IvoryColor.b, 0.72f));
            SetRect(subtitle.rectTransform, new Vector2(0f, 188f), new Vector2(390f, 48f));

            GameObject line = CreateImage("TitleDivider", panel.transform, GoldColor);
            SetRect(line.GetComponent<RectTransform>(), new Vector2(0f, 155f), new Vector2(300f, 2f));

            Button start = CreateButton(panel.transform, "GameStartButton", "START GAME", font, 70f);
            SetRect(start.GetComponent<RectTransform>(), new Vector2(0f, 58f), new Vector2(330f, 76f));

            Button settingsButton = CreateButton(panel.transform, "SettingsButton", "SETTINGS", font, 70f);
            SetRect(settingsButton.GetComponent<RectTransform>(), new Vector2(0f, -48f), new Vector2(330f, 76f));

            Button quit = CreateButton(panel.transform, "QuitButton", "QUIT GAME", font, 70f);
            SetRect(quit.GetComponent<RectTransform>(), new Vector2(0f, -154f), new Vector2(330f, 76f));

            GameObject system = new GameObject("MainMenuSystem");
            MainMenuController controller = system.AddComponent<MainMenuController>();

            SerializedObject controllerData = new SerializedObject(controller);
            controllerData.FindProperty("characterSelectionSceneName").stringValue = "ChooseCharactor";
            controllerData.FindProperty("settingsMenu").objectReferenceValue = settings;
            controllerData.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(start.onClick, controller.StartGame);
            UnityEventTools.AddPersistentListener(settingsButton.onClick, controller.OpenSettings);
            UnityEventTools.AddPersistentListener(quit.onClick, controller.QuitGame);
            return start;
        }

        private static SettingsMenuController CreateSettingsMenu(Transform canvas, TMP_FontAsset font)
        {
            GameObject system = CreateUiObject("SettingsMenu", canvas);
            Stretch(system.GetComponent<RectTransform>());
            SettingsMenuController controller = system.AddComponent<SettingsMenuController>();

            GameObject overlay = CreateImage(
                "SettingsOverlay",
                system.transform,
                new Color(0.01f, 0.015f, 0.02f, 0.76f));
            Stretch(overlay.GetComponent<RectTransform>());

            GameObject panel = CreateImage("SettingsPanel", overlay.transform, PanelColor);
            SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(720f, 540f));
            AddOutline(panel, new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.7f), new Vector2(2f, -2f));

            TMP_Text title = CreateText("SettingsTitle", panel.transform, "SETTINGS", font, 54f, IvoryColor);
            SetRect(title.rectTransform, new Vector2(0f, 205f), new Vector2(620f, 70f));
            title.fontStyle = FontStyles.Bold;

            TMP_Text volumeLabel = CreateText("VolumeLabel", panel.transform, "MASTER VOLUME", font, 24f, IvoryColor);
            volumeLabel.alignment = TextAlignmentOptions.Left;
            SetRect(volumeLabel.rectTransform, new Vector2(-190f, 92f), new Vector2(250f, 50f));

            Slider volume = CreateSlider(panel.transform);
            SetRect(volume.GetComponent<RectTransform>(), new Vector2(135f, 92f), new Vector2(290f, 36f));

            TMP_Text fullscreenLabel = CreateText("FullscreenLabel", panel.transform, "FULLSCREEN", font, 24f, IvoryColor);
            fullscreenLabel.alignment = TextAlignmentOptions.Left;
            SetRect(fullscreenLabel.rectTransform, new Vector2(-190f, 5f), new Vector2(250f, 50f));

            Toggle fullscreen = CreateToggle(panel.transform);
            SetRect(fullscreen.GetComponent<RectTransform>(), new Vector2(5f, 5f), new Vector2(42f, 42f));

            TMP_Text hint = CreateText(
                "SettingsHint",
                panel.transform,
                "PRESS ESC TO CLOSE",
                font,
                22f,
                new Color(IvoryColor.r, IvoryColor.g, IvoryColor.b, 0.62f));
            SetRect(hint.rectTransform, new Vector2(0f, -105f), new Vector2(600f, 42f));

            Button close = CreateButton(panel.transform, "CloseButton", "CLOSE", font, 64f);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0f, -194f), new Vector2(260f, 66f));

            SerializedObject settingsData = new SerializedObject(controller);
            settingsData.FindProperty("settingsRoot").objectReferenceValue = overlay;
            settingsData.FindProperty("masterVolumeSlider").objectReferenceValue = volume;
            settingsData.FindProperty("fullscreenToggle").objectReferenceValue = fullscreen;
            settingsData.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(close.onClick, controller.Close);
            UnityEventTools.AddPersistentListener(volume.onValueChanged, controller.SetMasterVolume);
            UnityEventTools.AddPersistentListener(fullscreen.onValueChanged, controller.SetFullscreen);

            overlay.SetActive(false);
            system.transform.SetAsLastSibling();
            return controller;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            TMP_FontAsset font,
            float height)
        {
            GameObject root = CreateImage(name, parent, ButtonColor);
            Button button = root.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            TMP_Text text = CreateText("Label", root.transform, label, font, height * 0.43f, IvoryColor);
            Stretch(text.rectTransform, 14f, 8f, 14f, 8f);

            MenuButtonHoverEffect hover = root.AddComponent<MenuButtonHoverEffect>();
            SerializedObject hoverData = new SerializedObject(hover);
            hoverData.FindProperty("targetGraphic").objectReferenceValue = root.GetComponent<Image>();
            hoverData.FindProperty("hoverColor").colorValue = GoldColor;
            hoverData.ApplyModifiedPropertiesWithoutUndo();
            return button;
        }

        private static Slider CreateSlider(Transform parent)
        {
            GameObject root = CreateUiObject("MasterVolumeSlider", parent);
            Slider slider = root.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            GameObject background = CreateImage("Background", root.transform, new Color(1f, 1f, 1f, 0.18f));
            Stretch(background.GetComponent<RectTransform>(), 0f, 11f, 0f, 11f);

            GameObject fillArea = CreateUiObject("Fill Area", root.transform);
            Stretch(fillArea.GetComponent<RectTransform>(), 8f, 11f, 8f, 11f);
            GameObject fill = CreateImage("Fill", fillArea.transform, GoldColor);
            Stretch(fill.GetComponent<RectTransform>());

            GameObject handleArea = CreateUiObject("Handle Slide Area", root.transform);
            Stretch(handleArea.GetComponent<RectTransform>(), 10f, 0f, 10f, 0f);
            GameObject handle = CreateImage("Handle", handleArea.transform, IvoryColor);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(24f, 36f);

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            return slider;
        }

        private static Toggle CreateToggle(Transform parent)
        {
            GameObject root = CreateImage("FullscreenToggle", parent, new Color(1f, 1f, 1f, 0.18f));
            Toggle toggle = root.AddComponent<Toggle>();
            toggle.isOn = Screen.fullScreen;

            GameObject checkmark = CreateImage("Checkmark", root.transform, GoldColor);
            Stretch(checkmark.GetComponent<RectTransform>(), 8f, 8f, 8f, 8f);
            toggle.targetGraphic = root.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            return toggle;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            TMP_FontAsset font,
            float fontSize,
            Color color)
        {
            GameObject root = CreateUiObject(name, parent);
            TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            return text;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject root = CreateUiObject(name, parent);
            Image image = root.AddComponent<Image>();
            image.color = color;
            return root;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            root.transform.SetParent(parent, false);
            return root;
        }

        private static void CreateEventSystem(GameObject firstSelected)
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject root = new GameObject("MainMenuEventSystem", typeof(EventSystem));
                eventSystem = root.GetComponent<EventSystem>();
                InputSystemUIInputModule inputModule = root.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
            }

            eventSystem.firstSelectedGameObject = firstSelected;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(
            RectTransform rect,
            float left = 0f,
            float top = 0f,
            float right = 0f,
            float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
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
