using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class MinimapSetup
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/Minimap.prefab";
        private const string QuestSheet = "Assets/_Project/Art/UI/퀘스트 아이콘 스프라이트 시트.png";

        [MenuItem("Eastern Fantasy/Setup Minimap")]
        public static void Apply()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = CreatePrefab();
            AddToScene(prefab, "Assets/_Project/Scenes/MovementPrototype.unity");
            AddToScene(prefab, "Assets/_Project/Scenes/PrologueVilage.unity");
            Debug.Log("Minimap installed in the tutorial and village scenes.");
        }

        private static GameObject CreatePrefab()
        {
            GameObject root = new GameObject("Minimap", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(CanvasGroup), typeof(MinimapUI));
            try
            {
                root.layer = 5;
                CanvasGroup opacity = root.GetComponent<CanvasGroup>();
                opacity.alpha = 0.55f;
                opacity.interactable = false;
                opacity.blocksRaycasts = false;
                Canvas canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 150;
                CanvasScaler scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                Image panel = CreateImage("Panel", root.transform, new Color(0.68f, 0.5f, 0.23f, 1f));
                RectTransform panelRect = panel.rectTransform;
                panelRect.anchorMin = panelRect.anchorMax = new Vector2(1f, 1f);
                panelRect.pivot = new Vector2(1f, 1f);
                panelRect.sizeDelta = new Vector2(360f, 212f);
                panelRect.anchoredPosition = new Vector2(-20f, -130f);
                Image background = CreateImage("Panel Background", panel.transform, new Color(0.025f, 0.045f, 0.05f, 0.96f));
                background.rectTransform.anchorMin = Vector2.zero;
                background.rectTransform.anchorMax = Vector2.one;
                background.rectTransform.offsetMin = new Vector2(2f, 2f);
                background.rectTransform.offsetMax = new Vector2(-2f, -2f);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/_Project/Fonts/GowunBatang-Bold SDF.asset");
                TMP_Text title = CreateLabel("Region Name", panel.transform, "지역 지도", font, 18f);
                RectTransform titleRect = title.rectTransform;
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.sizeDelta = new Vector2(-28f, 30f);
                titleRect.anchoredPosition = new Vector2(0f, -5f);
                Image viewport = CreateImage("Map Viewport", panel.transform, new Color(0.055f, 0.09f, 0.10f, 1f));
                viewport.rectTransform.anchorMin = Vector2.zero;
                viewport.rectTransform.anchorMax = Vector2.one;
                viewport.rectTransform.offsetMin = new Vector2(14f, 32f);
                viewport.rectTransform.offsetMax = new Vector2(-14f, -40f);
                viewport.gameObject.AddComponent<RectMask2D>();
                GameObject content = new GameObject("Map Content", typeof(RectTransform));
                content.layer = 5;
                content.transform.SetParent(viewport.transform, false);
                TMP_Text legend = CreateLabel("Legend", panel.transform, "하늘색: 나   P: 포탈   S: 상점", font, 12f);
                legend.rectTransform.anchorMin = new Vector2(0f, 0f);
                legend.rectTransform.anchorMax = new Vector2(1f, 0f);
                legend.rectTransform.pivot = new Vector2(0.5f, 0f);
                legend.rectTransform.sizeDelta = new Vector2(-28f, 26f);
                legend.rectTransform.anchoredPosition = new Vector2(0f, 3f);
                SerializedObject data = new SerializedObject(root.GetComponent<MinimapUI>());
                data.FindProperty("panelRoot").objectReferenceValue = panel.gameObject;
                data.FindProperty("mapViewport").objectReferenceValue = viewport.rectTransform;
                data.FindProperty("mapContent").objectReferenceValue = content.GetComponent<RectTransform>();
                data.FindProperty("regionName").objectReferenceValue = title;
                data.FindProperty("markerSprite").objectReferenceValue = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(QuestSheet))
                {
                    Sprite sprite = asset as Sprite;
                    if (sprite == null) continue;
                    if (sprite.name.EndsWith("_3")) data.FindProperty("availableQuestSprite").objectReferenceValue = sprite;
                    if (sprite.name.EndsWith("_4")) data.FindProperty("activeQuestSprite").objectReferenceValue = sprite;
                    if (sprite.name.EndsWith("_5")) data.FindProperty("readyQuestSprite").objectReferenceValue = sprite;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
            item.layer = 5;
            item.transform.SetParent(parent, false);
            Image image = item.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateLabel(string name, Transform parent, string value, TMP_FontAsset font, float size)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            item.layer = 5;
            item.transform.SetParent(parent, false);
            TMP_Text label = item.GetComponent<TMP_Text>();
            label.font = font;
            label.text = value;
            label.fontSize = size;
            label.color = new Color(0.95f, 0.82f, 0.54f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        private static void AddToScene(GameObject prefab, string path)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                    if (root.GetComponentInChildren<MinimapUI>(true) != null) return;
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                SceneManager.MoveGameObjectToScene(instance, scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}