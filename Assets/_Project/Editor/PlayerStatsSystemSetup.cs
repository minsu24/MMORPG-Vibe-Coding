using EasternFantasy.Player;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class PlayerStatsSystemSetup
    {
        private const string DataFolder = "Assets/_Project/Data/Player";
        private const string GrowthAssetPath = DataFolder + "/DosaStatGrowth.asset";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        private const string UiPrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string UiPrefabPath = UiPrefabFolder + "/PlayerStatsWindow.prefab";
        private const string FontPath = "Assets/_Project/Fonts/DNFBitBitv2SDF32 SDF 1.asset";

        private static readonly Color PanelColor = new Color(0.025f, 0.045f, 0.06f, 0.96f);
        private static readonly Color GoldColor = new Color(0.91f, 0.71f, 0.33f, 1f);
        private static readonly Color IvoryColor = new Color(0.95f, 0.93f, 0.84f, 1f);

        [MenuItem("Eastern Fantasy/Setup Player Stats System In Active Scene")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            PlayerEntity player = Object.FindFirstObjectByType<PlayerEntity>();
            PlayerProgression progression = Object.FindFirstObjectByType<PlayerProgression>();
            if (player == null || progression == null)
                throw new MissingReferenceException("The active scene needs a PlayerEntity and PlayerProgression.");

            EnsureFolder(DataFolder);
            PlayerStatGrowthTable growth = AssetDatabase.LoadAssetAtPath<PlayerStatGrowthTable>(GrowthAssetPath);
            if (growth == null)
            {
                growth = ScriptableObject.CreateInstance<PlayerStatGrowthTable>();
                AssetDatabase.CreateAsset(growth, GrowthAssetPath);
            }

            AssignGrowthToPlayerPrefab(growth);
            AssignGrowth(player, growth);

            PlayerStatsWindowUI window = Object.FindFirstObjectByType<PlayerStatsWindowUI>();
            if (window == null)
                window = CreateWindow(player, progression);

            EnsureFolder(UiPrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(window.gameObject, UiPrefabPath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Player stat growth and C-key stats window are ready.");
        }

        private static void AssignGrowthToPlayerPrefab(PlayerStatGrowthTable growth)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                PlayerEntity entity = prefabRoot.GetComponent<PlayerEntity>();
                if (entity == null)
                    throw new MissingComponentException("Player prefab needs PlayerEntity.");

                AssignGrowth(entity, growth);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void AssignGrowth(PlayerEntity entity, PlayerStatGrowthTable growth)
        {
            SerializedObject data = new SerializedObject(entity);
            data.FindProperty("statGrowth").objectReferenceValue = growth;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PlayerStatsWindowUI CreateWindow(
            PlayerEntity player,
            PlayerProgression progression)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            GameObject root = new GameObject(
                "PlayerStatsOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            root.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 400;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            PlayerStatsWindowUI controller = root.AddComponent<PlayerStatsWindowUI>();

            GameObject panel = CreateImage("StatsWindow", root.transform, PanelColor);
            SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(620f, 820f));

            TMP_Text header = CreateText("Header", panel.transform, "CHARACTER STATUS", font, 34f, GoldColor);
            SetRect(header.rectTransform, new Vector2(0f, 350f), new Vector2(540f, 55f));

            GameObject divider = CreateImage("Divider", panel.transform, GoldColor);
            SetRect(divider.GetComponent<RectTransform>(), new Vector2(0f, 312f), new Vector2(530f, 2f));

            TMP_Text classAndLevel = CreateText("ClassAndLevel", panel.transform, string.Empty, font, 23f, IvoryColor);
            SetRect(classAndLevel.rectTransform, new Vector2(0f, 275f), new Vector2(530f, 42f));

            TMP_Text stats = CreateText("Stats", panel.transform, string.Empty, font, 22f, IvoryColor);
            stats.alignment = TextAlignmentOptions.TopLeft;
            stats.textWrappingMode = TextWrappingModes.NoWrap;
            SetRect(stats.rectTransform, new Vector2(0f, -35f), new Vector2(470f, 570f));

            TMP_Text hint = CreateText("CloseHint", panel.transform, "[ C ]  CLOSE", font, 20f, GoldColor);
            SetRect(hint.rectTransform, new Vector2(0f, -375f), new Vector2(530f, 35f));

            SerializedObject controllerData = new SerializedObject(controller);
            controllerData.FindProperty("windowRoot").objectReferenceValue = panel;
            controllerData.FindProperty("classAndLevelText").objectReferenceValue = classAndLevel;
            controllerData.FindProperty("statsText").objectReferenceValue = stats;
            controllerData.FindProperty("playerEntity").objectReferenceValue = player;
            controllerData.FindProperty("playerProgression").objectReferenceValue = progression;
            controllerData.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return controller;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            TMP_FontAsset font,
            float size,
            Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            root.transform.SetParent(parent, false);
            TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            root.transform.SetParent(parent, false);
            Image image = root.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
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
