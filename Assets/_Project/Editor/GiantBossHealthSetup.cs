using System;
using System.Linq;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.EditorTools
{
    public static class GiantBossHealthSetup
    {
        public const string PrefabPath = "Assets/_Project/Resources/GiantBossHealthUI.prefab";
        public const string ScenePath = "Assets/_Project/Scenes/GiantFarm/GiantFarm_03_Boss.unity";
        public const string TexturePath = "Assets/_Project/Art/UI/GiantBoss/FarmBossHealthFrame.png";
        public const string FramePath = "Assets/_Project/Art/UI/GiantBoss/FarmBossHealthFrame.asset";

        [MenuItem("Eastern Fantasy/UI/Setup Farm Boss Health Bar")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run outside Play mode.");
            var frameSprite = ImportApprovedFrame();
            var root = new GameObject("GiantBossHealthUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            GiantBossHealthUI prefab;
            try
            {
                root.layer = 5;
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 110; // Below dungeon exit, death and other modal windows.
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = .5f;
                var ui = root.AddComponent<GiantBossHealthUI>();
                ui.visibility = root.AddComponent<CanvasGroup>();
                ui.visibility.alpha = 0;
                ui.visibility.blocksRaycasts = ui.visibility.interactable = false;
                var panel = Rect("FarmBossPanel", root.transform, Vector2.zero, new Vector2(800, 800f * 400f / 2172f));
                panel.anchorMin = panel.anchorMax = new Vector2(.5f, 1);
                panel.anchoredPosition = new Vector2(0, -201);
                var frame = panel.gameObject.AddComponent<Image>();
                frame.sprite = frameSprite;
                frame.raycastTarget = false;
                ui.panel = panel;
                var name = Text("BossName", panel, "농장의 주인 · 거인", Vector2.zero, Vector2.zero, 24);
                // Pixel bounds in the approved 2172x724 image; the sprite trims only transparent vertical padding.
                ArtworkRect(name.rectTransform, 744, 217, 1428, 321);
                name.enableAutoSizing = true;
                name.fontSizeMin = 14;
                name.fontSizeMax = 24;
                var gauge = Rect("HealthGauge", panel, Vector2.zero, Vector2.zero);
                ArtworkRect(gauge, 245, 393, 1931, 465);
                var slider = gauge.gameObject.AddComponent<Slider>();
                slider.minValue = 0;
                slider.maxValue = 1;
                slider.value = 1;
                slider.interactable = false;
                slider.transition = Selectable.Transition.None;
                slider.navigation = new Navigation { mode = Navigation.Mode.None };
                var fillArea = Rect("FillArea", gauge, Vector2.zero, Vector2.zero);
                fillArea.anchorMin = Vector2.zero;
                fillArea.anchorMax = Vector2.one;
                fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;
                var fill = Rect("BloodRedFill", fillArea, Vector2.zero, Vector2.zero);
                fill.anchorMin = Vector2.zero;
                fill.anchorMax = Vector2.one;
                fill.offsetMin = fill.offsetMax = Vector2.zero;
                var art = fill.gameObject.AddComponent<FarmBossBarGraphic>();
                art.gaugeFill = true;
                art.raycastTarget = false;
                slider.fillRect = fill;
                ui.health = slider;
                ui.healthText = Text("HealthValue", panel, "200 / 200", Vector2.zero, Vector2.zero, 16);
                ArtworkRect(ui.healthText.rectTransform, 744, 393, 1428, 465);
                var shadow = ui.healthText.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, .9f);
                shadow.effectDistance = new Vector2(1, -1);
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath).GetComponent<GiantBossHealthUI>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var boss = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MON_Giant>(true)).Single();
                var presenter = boss.GetComponent<GiantBossHealthPresenter>();
                if (presenter == null) presenter = Undo.AddComponent<GiantBossHealthPresenter>(boss.gameObject);
                presenter.Configure(prefab);
                EditorUtility.SetDirty(presenter);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            AssetDatabase.SaveAssets();
        }

        private static Sprite ImportApprovedFrame()
        {
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);
            if (existing != null) return existing;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            // Preserve the approved pixels; trim via sprite UVs rather than altering the PNG.
            var sprite = Sprite.Create(texture, new UnityEngine.Rect(0, 160, 2172, 400), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            sprite.name = "FarmBossHealthFrame";
            AssetDatabase.CreateAsset(sprite, FramePath);
            return sprite;
        }

        private static void ArtworkRect(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = new Vector2(left / 2172f, (564f - bottom) / 400f);
            rect.anchorMax = new Vector2(right / 2172f, (564f - top) / 400f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
            r.anchoredPosition = position;
            r.sizeDelta = size;
            return r;
        }

        private static TMP_Text Text(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
        {
            var r = Rect(name, parent, position, size);
            var text = r.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/GowunBatang-Bold SDF.asset");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.98f, .88f, .66f);
            text.raycastTarget = false;
            return text;
        }
    }
}
