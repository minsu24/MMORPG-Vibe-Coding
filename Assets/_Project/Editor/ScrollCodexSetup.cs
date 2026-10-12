using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.EditorTools
{
    public static class ScrollCodexSetup
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/UI/ScrollCodex.prefab";
        private const string Art = "Assets/_Project/Art/UI/ScrollCodex/";
        [MenuItem("Eastern Fantasy/UI/Setup Scroll Codex Animation")]
        public static void Run()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Run setup outside Play mode.");
            var root = new GameObject("ScrollCodexOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.SetActive(false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 450;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            var ui = root.AddComponent<ScrollCodexWindowUI>();
            var window = Rect("Window", root.transform, new Vector2(1920,1080));
            window.anchorMin = Vector2.zero; window.anchorMax = Vector2.one; window.sizeDelta = Vector2.zero;
            var dim = Image("Backdrop", window, null, new Vector2(1920,1080));
            dim.color = new Color(.025f,.035f,.06f,.72f); dim.raycastTarget = true;
            dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one; dim.rectTransform.sizeDelta = Vector2.zero;
            var backdropButton = dim.gameObject.AddComponent<Button>(); backdropButton.transition = Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(backdropButton.onClick, ui.Close);
            var scroll = Rect("ScrollCodexRoot", window, new Vector2(1672,941));
            scroll.localScale = Vector3.one * 1.08f;
            var animation = scroll.gameObject.AddComponent<ScrollCodexAnimator>();
            var unrolled = Rect("Unrolled", scroll, new Vector2(1672,941));
            var group = unrolled.gameObject.AddComponent<CanvasGroup>();
            var viewport = Rect("PaperViewport", unrolled, new Vector2(0,941)); viewport.gameObject.AddComponent<RectMask2D>();
            Image("Paper", viewport, Import("Paper.png"), new Vector2(1412,941));
            // Reserved for future codex contents, clipped with the paper.
            Rect("Content", viewport, new Vector2(1150,540));
            var left = Image("LeftRoller", unrolled, Import("LeftRoller.png"), new Vector2(148,941)).rectTransform;
            var right = Image("RightRoller", unrolled, Import("RightRoller.png"), new Vector2(172,941)).rectTransform;
            var sprite = Import("두루마리 묶음1.png");
            var bound = Image("BoundScroll", scroll, sprite, sprite.rect.size * (941f / sprite.rect.height));
            var boundGroup = bound.gameObject.AddComponent<CanvasGroup>();
            animation.Configure(viewport, left, right, group, boundGroup);
            var hint = Rect("AnimationHint", window, new Vector2(800,40));
            hint.anchorMin = hint.anchorMax = new Vector2(.5f,0); hint.anchoredPosition = new Vector2(0,32);
            var text = hint.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/GowunBatang-Bold SDF.asset");
            text.text = "도감  ·  U 펼치기 / 접기  ·  ESC 닫기"; text.fontSize = 22; text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.95f,.85f,.64f); text.raycastTarget = false;
            ui.Configure(window.gameObject, scroll, animation);
            window.gameObject.SetActive(false);
            root.SetActive(true);
            GameObject prefab;
            try { prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); }
            finally { Object.DestroyImmediate(root); }
            // Add to the current gameplay scene, preserving its existing objects.
            var existing = Object.FindFirstObjectByType<ScrollCodexWindowUI>(FindObjectsInactive.Include);
            if (!existing) {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.SetActive(true); Undo.RegisterCreatedObjectUndo(instance, "Add scroll codex");
                EditorSceneManager.MarkSceneDirty(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Scroll codex animation configured. U toggles; Escape/backdrop closes. Scene changes are ready to save.");
        }
        private static Sprite Import(string name)
        {
            string path = Art + name;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048; importer.filterMode = FilterMode.Bilinear;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false); var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f,.5f); rt.sizeDelta = size;
            return rt;
        }
        private static Image Image(string name, Transform parent, Sprite sprite, Vector2 size)
        {
            var rt = Rect(name,parent,size); var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.raycastTarget = false; return image;
        }
    }
}


