using EasternFantasy.Dialogue;
using EasternFantasy.Quest;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class QuestSystemSetup
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string QuestManagerPrefabPath = PrefabFolder + "/QuestManager.prefab";
        private const string QuestHudPrefabPath = PrefabFolder + "/QuestHUD.prefab";
        private const string FallbackFontPath = "Assets/_Project/Fonts/DNFBitBitv2SDF32 SDF 1.asset";

        private static readonly Color PanelColor = new Color(0.025f, 0.045f, 0.06f, 0.82f);
        private static readonly Color ButtonColor = new Color(0.08f, 0.13f, 0.16f, 0.96f);
        private static readonly Color GoldColor = new Color(0.91f, 0.71f, 0.33f, 1f);
        private static readonly Color IvoryColor = new Color(0.95f, 0.93f, 0.84f, 1f);

        [MenuItem("Eastern Fantasy/Setup Quest System In Active Scene")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                throw new MissingReferenceException("The active scene needs a Canvas before quest UI setup.");

            DialogueManager dialogueManager = Object.FindFirstObjectByType<DialogueManager>();
            TMP_FontAsset font = GetFont(dialogueManager);

            QuestManager questManager = Object.FindFirstObjectByType<QuestManager>();
            if (questManager == null)
            {
                GameObject managerObject = new GameObject("QuestManager");
                questManager = managerObject.AddComponent<QuestManager>();
            }

            QuestTrackerUI tracker = Object.FindFirstObjectByType<QuestTrackerUI>();
            if (tracker == null)
                tracker = CreateTracker(canvas.transform, font);

            if (dialogueManager != null)
                CreateQuestChoiceUi(dialogueManager, font);
            else
                Debug.LogWarning("No DialogueManager was found. Quest choice UI was not created.");

            EnsureFolder(PrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(questManager.gameObject, QuestManagerPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(tracker.gameObject, QuestHudPrefabPath);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("QuestManager, quest tracker, and dialogue choice UI are ready.");
        }

        private static QuestTrackerUI CreateTracker(Transform canvas, TMP_FontAsset font)
        {
            GameObject controllerObject = CreateUiObject("QuestHUD", canvas);
            Stretch(controllerObject.GetComponent<RectTransform>());
            QuestTrackerUI tracker = controllerObject.AddComponent<QuestTrackerUI>();

            GameObject panel = CreateImage("QuestTrackerPanel", controllerObject.transform, PanelColor);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0.72f);
            panelRect.anchorMax = new Vector2(1f, 0.72f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-35f, 0f);
            panelRect.sizeDelta = new Vector2(410f, 330f);

            TMP_Text header = CreateText("QuestTrackerHeader", panel.transform, "ACTIVE QUESTS", font, 28f, GoldColor);
            header.alignment = TextAlignmentOptions.Left;
            SetRect(header.rectTransform, new Vector2(0f, 133f), new Vector2(350f, 45f));

            GameObject divider = CreateImage("Divider", panel.transform, GoldColor);
            SetRect(divider.GetComponent<RectTransform>(), new Vector2(0f, 103f), new Vector2(350f, 2f));

            TMP_Text list = CreateText("QuestList", panel.transform, string.Empty, font, 22f, IvoryColor);
            list.alignment = TextAlignmentOptions.TopLeft;
            list.textWrappingMode = TextWrappingModes.Normal;
            SetRect(list.rectTransform, new Vector2(0f, -35f), new Vector2(350f, 245f));

            SerializedObject trackerData = new SerializedObject(tracker);
            trackerData.FindProperty("trackerRoot").objectReferenceValue = panel;
            trackerData.FindProperty("questListText").objectReferenceValue = list;
            trackerData.ApplyModifiedPropertiesWithoutUndo();
            return tracker;
        }

        private static void CreateQuestChoiceUi(DialogueManager manager, TMP_FontAsset font)
        {
            SerializedObject managerData = new SerializedObject(manager);
            if (managerData.FindProperty("questChoiceRoot").objectReferenceValue != null)
                return;

            GameObject dialogueRoot = (GameObject)managerData.FindProperty("dialogueRoot").objectReferenceValue;
            if (dialogueRoot == null)
                throw new MissingReferenceException("DialogueManager has no Dialogue Root.");

            GameObject choicePanel = CreateImage("QuestChoicePanel", dialogueRoot.transform, PanelColor);
            SetRect(choicePanel.GetComponent<RectTransform>(), new Vector2(0f, 20f), new Vector2(680f, 210f));

            TMP_Text prompt = CreateText(
                "QuestChoicePrompt",
                choicePanel.transform,
                "ACCEPT THIS QUEST?",
                font,
                22f,
                new Color(IvoryColor.r, IvoryColor.g, IvoryColor.b, 0.72f));
            SetRect(prompt.rectTransform, new Vector2(0f, 70f), new Vector2(600f, 34f));

            TMP_Text title = CreateText("QuestChoiceTitle", choicePanel.transform, "QUEST", font, 28f, IvoryColor);
            SetRect(title.rectTransform, new Vector2(0f, 31f), new Vector2(600f, 42f));

            Button accept = CreateButton(choicePanel.transform, "AcceptQuestButton", "ACCEPT", font);
            SetRect(accept.GetComponent<RectTransform>(), new Vector2(-150f, -52f), new Vector2(230f, 58f));

            Button decline = CreateButton(choicePanel.transform, "DeclineQuestButton", "DECLINE", font);
            SetRect(decline.GetComponent<RectTransform>(), new Vector2(150f, -52f), new Vector2(230f, 58f));

            UnityEventTools.AddPersistentListener(accept.onClick, manager.AcceptQuestChoice);
            UnityEventTools.AddPersistentListener(decline.onClick, manager.DeclineQuestChoice);

            managerData.FindProperty("questChoiceRoot").objectReferenceValue = choicePanel;
            managerData.FindProperty("questChoiceTitleText").objectReferenceValue = title;
            managerData.ApplyModifiedPropertiesWithoutUndo();
            choicePanel.SetActive(false);
        }

        private static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font)
        {
            GameObject root = CreateImage(name, parent, ButtonColor);
            Button button = root.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = GoldColor;
            colors.selectedColor = GoldColor;
            colors.pressedColor = new Color(0.72f, 0.54f, 0.24f, 1f);
            button.colors = colors;

            TMP_Text text = CreateText("Label", root.transform, label, font, 23f, IvoryColor);
            Stretch(text.rectTransform, 8f, 5f, 8f, 5f);
            return button;
        }

        private static TMP_FontAsset GetFont(DialogueManager manager)
        {
            if (manager != null)
            {
                SerializedObject data = new SerializedObject(manager);
                TMP_Text dialogueText = (TMP_Text)data.FindProperty("dialogueText").objectReferenceValue;
                if (dialogueText != null && dialogueText.font != null)
                    return dialogueText.font;
            }

            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
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
