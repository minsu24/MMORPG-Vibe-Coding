using EasternFantasy.Player;
using EasternFantasy.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class LevelUpEffectSetup
    {
        private const string SpritePath = "Assets/_Project/Art/레벨 업 이미지.png";
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/LevelUpEffect.prefab";

        [MenuItem("Eastern Fantasy/Setup Level Up Effect In Active Scene")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            PlayerProgression progression = Object.FindFirstObjectByType<PlayerProgression>();
            if (progression == null)
                throw new MissingReferenceException("The active scene needs a PlayerProgression.");

            LevelUpEffectUI existing = Object.FindFirstObjectByType<LevelUpEffectUI>();
            if (existing != null && existing.GetComponent<Canvas>() == null)
            {
                Object.DestroyImmediate(existing.gameObject);
                existing = null;
            }

            if (existing == null)
                existing = CreateEffect(progression);

            EnsureFolder(PrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(existing.gameObject, PrefabPath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Level-up screen effect is ready.");
        }

        private static LevelUpEffectUI CreateEffect(PlayerProgression progression)
        {
            GameObject root = new GameObject(
                "LevelUpEffectOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            root.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GameObject imageObject = new GameObject(
                "LevelUpImage",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            imageObject.layer = LayerMask.NameToLayer("UI");
            imageObject.transform.SetParent(root.transform, false);

            CanvasGroup group = imageObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            Image image = imageObject.GetComponent<Image>();
            image.sprite = LoadLevelUpSprite();
            image.preserveAspect = true;
            image.raycastTarget = false;
            RectTransform imageRect = image.rectTransform;
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = Vector2.zero;
            imageRect.sizeDelta = new Vector2(1050f, 350f);

            LevelUpEffectUI controller = root.AddComponent<LevelUpEffectUI>();
            SerializedObject data = new SerializedObject(controller);
            data.FindProperty("playerProgression").objectReferenceValue = progression;
            data.FindProperty("effectGroup").objectReferenceValue = group;
            data.FindProperty("effectImage").objectReferenceValue = image;
            data.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static Sprite LoadLevelUpSprite()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SpritePath);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                    return sprite;
            }

            throw new MissingReferenceException("No Sprite was found at " + SpritePath);
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
