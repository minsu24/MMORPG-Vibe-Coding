using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EasternFantasy.EditorTools
{
    [InitializeOnLoad]
    public static class SoulstoneSystemSetup
    {
        private const string Folder = "Assets/_Project/Resources/UI/Soulstone/";
        private const string CatalogPath = "Assets/_Project/Resources/SoulstoneCatalog.asset";
        private const string Report = "Artifacts/Soulstone/Setup-report.txt";
        private static readonly string[] Scenes = { "PrologueVilage", "TempleGate" };
        private static readonly string[] Paths = { "Assets/_Project/Scenes/TutorialMap/PrologueVilage.unity", "Assets/_Project/Scenes/OldTemple/TempleGate.unity" };
        private static readonly string[] Stones = { "귀혼석", "TempleRespornStone" };

        static SoulstoneSystemSetup()
        {
            // Install once when these source files are first imported; never save open scenes.
            EditorApplication.delayCall += () =>
            {
                var existing = AssetDatabase.LoadAssetAtPath<SoulstoneCatalog>(CatalogPath);
                bool ready = existing != null && existing.destinations.Length == 2
                    && existing.destinations.All(d => d != null && d.preview != null);
                if (ready)
                {
                    SoulstonePrefabSetup.EnsurePrefab();
                    if (!Application.isPlaying && !File.Exists("Artifacts/Soulstone/ModularSkin-v3.txt"))
                    {
                        if (existing.travelHover == null || existing.mapFrameOutline == null) ApplyModularSkin(existing);
                        existing.listPanel = SkinSprite("ListPanelPlain.png", new Rect(218, 78, 650, 1292), new Vector4(44, 44, 44, 44));
                        EditorUtility.SetDirty(existing); AssetDatabase.SaveAssets();
                        foreach (var destination in existing.destinations)
                            RenderWindow(existing, destination.sceneName, "Artifacts/Soulstone/" + destination.sceneName + "-UI.png");
                        File.WriteAllText("Artifacts/Soulstone/ModularSkin-v3.txt", "Cover-cropped map photos, midline text, compact centered NPCs, travel subtitle and hover.");
                    }
                    SoulstoneLiveValidation.RunIfRequested();
                    return;
                }
                if (Application.isPlaying || SessionState.GetBool("Soulstone.SetupAttempt.4", false)) return;
                SessionState.SetBool("Soulstone.SetupAttempt.4", true);
                Build();
            };
        }

        [MenuItem("Eastern Fantasy/Soulstone/Build Assets and Verify")]
        public static void Build()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode before building soulstone assets.");
            Directory.CreateDirectory("Artifacts/Soulstone");
            var report = new List<string>();
            try
            {
                Directory.CreateDirectory(Folder);
                var catalog = AssetDatabase.LoadAssetAtPath<SoulstoneCatalog>(CatalogPath);
                if (catalog == null) { catalog = ScriptableObject.CreateInstance<SoulstoneCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
                catalog.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/GowunBatang-Bold SDF.asset");
                catalog.frame = ImportSprite(Folder + "Frame.png");
                catalog.listSlot = SkinSprite("ListSlot.png", new Rect(92, 24, 524, 112), Vector4.zero);
                catalog.destinations = new SoulstoneCatalog.Destination[2];
                for (int i = 0; i < 2; i++)
                {
                    var scene = SceneManager.GetSceneByPath(Paths[i]);
                    bool preview = !scene.isLoaded;
                    if (preview) scene = EditorSceneManager.OpenPreviewScene(Paths[i]);
                    try
                    {
                        var transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                        var stone = transforms.SingleOrDefault(t => t.name.Trim() == Stones[i])
                            ?? transforms.SingleOrDefault(t => i == 1 && t.name.Trim() == "절 문 귀혼석");
                        if (stone == null) throw new InvalidOperationException("Soulstone not found in " + scene.name + ". Candidates: "
                            + string.Join(", ", transforms.Where(t => t.name.Contains("석") || t.name.Contains("Stone") || t.name.Contains("Resporn")).Select(t => t.name)));
                        report.Add(Scenes[i] + " stone: " + stone.name + " at " + stone.position);
                        File.WriteAllLines(Report, report);
                        string screenshot = Folder + Scenes[i] + ".png";
                        CaptureMap(scene, screenshot, report);
                        catalog.destinations[i] = new SoulstoneCatalog.Destination {
                            sceneName = Scenes[i], displayName = i == 0 ? "서막 마을" : "절 문", stoneName = stone.name.Trim(),
                            preview = ImportSprite(screenshot), npcs = ReadMapNpcs(Scenes[i])
                        };
                        report.Add(Scenes[i] + " NPCs: " + string.Join(", ", catalog.destinations[i].npcs.Select(n => n.name)));
                    }
                    finally { if (preview) EditorSceneManager.ClosePreviewScene(scene); }
                }
                ApplyModularSkin(catalog);
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
                SoulstonePrefabSetup.EnsurePrefab();
                foreach (var destination in catalog.destinations)
                {
                    if (destination.preview == null || string.IsNullOrEmpty(destination.stoneName)) throw new InvalidOperationException("Destination asset is incomplete.");
                    RenderWindow(catalog, destination.sceneName, "Artifacts/Soulstone/" + destination.sceneName + "-UI.png");
                }
                report.Add("PASS: two destination anchors, full-map screenshots, shared map NPC portraits, G/ESC bindings, and UI renders created. No scene was saved.");
                File.WriteAllLines(Report, report);
                File.WriteAllText("Artifacts/Soulstone/Layout-v2.txt", "Centered full-map previews and aligned labels verified.");
                Debug.Log("Soulstone system assets ready. " + Report);
                SoulstoneLiveValidation.RunIfRequested();
            }
            catch (Exception exception)
            {
                report.Add("FAILED: " + exception);
                File.WriteAllLines(Report, report);
                Debug.LogException(exception);
            }
        }

        private static void ApplyModularSkin(SoulstoneCatalog catalog)
        {
            catalog.frame = SkinSprite("FrameV2.png", new Rect(0, 0, 1448, 1086), new Vector4(110, 110, 110, 165));
            catalog.mapFrame = SkinSprite("MapFrame.png", new Rect(48, 52, 1527, 871), Vector4.zero);
            catalog.mapNamePanel = SkinSprite("MapNamePanel.png", new Rect(60, 200, 2050, 310), new Vector4(140, 40, 140, 40));
            catalog.listPanel = SkinSprite("ListPanelPlain.png", new Rect(218, 78, 650, 1292), new Vector4(44, 44, 44, 44));
            catalog.npcPanel = SkinSprite("NpcPanel.png", new Rect(56, 420, 1340, 250), new Vector4(70, 45, 70, 45));
            catalog.travelNormal = SkinSprite("TravelNormal.png", new Rect(20, 50, 2130, 590), Vector4.zero);
            catalog.travelHover = SkinSprite("TravelHover.png", new Rect(20, 50, 2130, 590), Vector4.zero);
            const string outlinePath = Folder + "MapFrameOutline.asset";
            var outline = AssetDatabase.LoadAssetAtPath<MapNpcPortrait>(outlinePath);
            if (outline == null) { outline = ScriptableObject.CreateInstance<MapNpcPortrait>(); AssetDatabase.CreateAsset(outline, outlinePath); }
            outline.atlas = catalog.mapFrame.texture;
            outline.region = catalog.mapFrame.rect;
            var r = outline.region;
            float corner = 90f, edge = 16f;
            // Native UV geometry selects the supplied frame edges and corners;
            // the bitmap stays intact and the map photograph remains visible underneath.
            outline.strips = new[] {
                new Vector4(r.xMin, r.yMin, r.xMin + corner, r.yMin + corner),
                new Vector4(r.xMax - corner, r.yMin, r.xMax, r.yMin + corner),
                new Vector4(r.xMin, r.yMax - corner, r.xMin + corner, r.yMax),
                new Vector4(r.xMax - corner, r.yMax - corner, r.xMax, r.yMax),
                new Vector4(r.xMin + corner, r.yMin, r.xMax - corner, r.yMin + edge),
                new Vector4(r.xMin + corner, r.yMax - edge, r.xMax - corner, r.yMax),
                new Vector4(r.xMin, r.yMin + corner, r.xMin + edge, r.yMax - corner),
                new Vector4(r.xMax - edge, r.yMin + corner, r.xMax, r.yMax - corner)
            };
            RemoveCornerBackground(outline);
            catalog.mapFrameOutline = outline;
            EditorUtility.SetDirty(outline); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        }

        // Select only the coloured ornament pixels in each corner. The source image is unchanged;
        // the native UI mesh no longer draws the opaque rectangular backing around the filigree.
        public static void RemoveCornerBackground(MapNpcPortrait outline)
        {
            var texture = outline.atlas;
            var target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            var readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(texture, target); RenderTexture.active = target;
                readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); readable.Apply();
                var pixels = readable.GetPixels32();
                var r = AssetDatabase.LoadAllAssetsAtPath(Folder + "MapFrame.png").OfType<Sprite>().Single().rect;
                // The first 15 source rows are the brown outer lip below the gold border.
                r.yMin += 15f;
                outline.region = r;
                const int corner = 90, edge = 16;
                var regions = new[] {
                    new RectInt((int)r.xMin, (int)r.yMin, corner, corner),
                    new RectInt((int)r.xMax-corner, (int)r.yMin, corner, corner),
                    new RectInt((int)r.xMin, (int)r.yMax-corner, corner, corner),
                    new RectInt((int)r.xMax-corner, (int)r.yMax-corner, corner, corner)
                };
                var strips = new List<Vector4>();
                foreach (var region in regions)
                    for (int y = region.yMin; y < region.yMax; y++)
                    {
                        int start = -1;
                        for (int x = region.xMin; x <= region.xMax; x++)
                        {
                            var c = x < region.xMax ? pixels[y * texture.width + x] : new Color32();
                            bool ornament = x < region.xMax && c.a > 32 && c.r > 48 && c.r > c.b + 12 && c.r > c.g + 5;
                            if (ornament && start < 0) start = x;
                            if (!ornament && start >= 0) { strips.Add(new Vector4(start, y, x, y+1)); start = -1; }
                        }
                    }
                strips.Add(new Vector4(r.xMin+corner, r.yMin, r.xMax-corner, r.yMin+8));
                strips.Add(new Vector4(r.xMin+corner, r.yMax-edge, r.xMax-corner, r.yMax));
                strips.Add(new Vector4(r.xMin, r.yMin+corner, r.xMin+edge, r.yMax-corner));
                strips.Add(new Vector4(r.xMax-edge, r.yMin+corner, r.xMax, r.yMax-corner));
                outline.strips = strips.ToArray(); EditorUtility.SetDirty(outline);
                // A loaded Prefab Stage can retain its previous mesh when only this asset changes.
                foreach (var graphic in Resources.FindObjectsOfTypeAll<MapNpcPortraitGraphic>())
                    if (graphic.portrait == outline) graphic.SetVerticesDirty();
                SceneView.RepaintAll();
            }
            finally
            {
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Object.DestroyImmediate(readable);
            }
        }

        internal static Sprite SkinSprite(string filename, Rect topRect, Vector4 border)
        {
            string path = Folder + filename;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear; importer.maxTextureSize = 4096;
#pragma warning disable CS0618
            importer.spritesheet = new[] { new SpriteMetaData {
                name = "SoulstoneSkin", rect = new Rect(topRect.x, height - topRect.yMax, topRect.width, topRect.height),
                pivot = new Vector2(.5f, .5f), alignment = 0, border = border
            } };
#pragma warning restore CS0618
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
        }

        private static Sprite ImportSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static SoulstoneCatalog.Npc[] ReadMapNpcs(string sceneName)
        {
            var map = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/MapWindow.prefab");
            var icons = map != null ? map.GetComponentInChildren<MapNpcIconsUI>(true) : null;
            if (icons == null) throw new InvalidOperationException("Existing map NPC icon catalog was not found.");
            var entries = new SerializedObject(icons).FindProperty("entries");
            var result = new List<SoulstoneCatalog.Npc>();
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("sceneName").stringValue != sceneName) continue;
                result.Add(new SoulstoneCatalog.Npc {
                    name = entry.FindPropertyRelative("displayName").stringValue,
                    portrait = entry.FindPropertyRelative("portrait").objectReferenceValue as MapNpcPortrait
                });
            }
            return result.ToArray();
        }

        private static void CaptureMap(Scene scene, string path, List<string> report)
        {
            var all = scene.GetRootGameObjects();
            var shapes = all.SelectMany(r => r.GetComponentsInChildren<Collider2D>(true));
            var boundsShape = shapes.First(c => c.name == "CameraCollider");
            Physics2D.SyncTransforms();
            Bounds bounds = boundsShape.bounds;
            if (bounds.size.x < 1f || bounds.size.y < 1f) throw new InvalidOperationException("Map camera bounds are empty.");
            var hidden = all.SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).Where(c => c.enabled).ToArray();
            var cameras = all.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).ToArray();
            var source = cameras.FirstOrDefault(c => c.CompareTag("MainCamera")) ?? cameras.First();
            var cameraObject = new GameObject("Soulstone Full Map Capture");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.CopyFrom(source);
            var pipelineData = source.GetComponent<UniversalAdditionalCameraData>();
            if (pipelineData != null) EditorUtility.CopySerialized(pipelineData, cameraObject.AddComponent<UniversalAdditionalCameraData>());
            camera.scene = scene;
            camera.enabled = false;
            camera.orthographic = true;
            camera.transform.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.center.y, source.transform.position.z), Quaternion.identity);
            int height = Mathf.Clamp(Mathf.RoundToInt(2048f * bounds.size.y / bounds.size.x), 256, 1536);
            camera.aspect = 2048f / height;
            camera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x / camera.aspect);
            var rt = new RenderTexture(2048, height, 24);
            var pixels = new Texture2D(2048, height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                foreach (var canvas in hidden) canvas.enabled = false;
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                pixels.ReadPixels(new Rect(0, 0, 2048, height), 0, 0); pixels.Apply();
                var samples = pixels.GetPixels32();
                int low = 255, high = 0;
                for (int i = 0; i < samples.Length; i += 257)
                {
                    int value = samples[i].r + samples[i].g + samples[i].b;
                    low = Mathf.Min(low, value / 3); high = Mathf.Max(high, value / 3);
                }
                if (high - low < 8) throw new InvalidOperationException(scene.name + " map capture is blank; check its camera renderer.");
                File.WriteAllBytes(path, pixels.EncodeToPNG());
                report.Add(scene.name + " full-map capture: " + bounds + ", 2048x" + height);
            }
            finally
            {
                foreach (var canvas in hidden) if (canvas != null) canvas.enabled = true;
                camera.targetTexture = null; RenderTexture.active = previous;
                Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(pixels); Object.DestroyImmediate(rt);
            }
        }

        public static void RenderWindow(SoulstoneCatalog catalog, string destination, string path)
        {
            var root = new GameObject("Soulstone Window Verification", typeof(RectTransform));
            var cameraObject = new GameObject("Soulstone UI Verification Camera");
            var camera = cameraObject.AddComponent<Camera>();
            var rt = new RenderTexture(1920, 1080, 24);
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                var window = root.AddComponent<SoulstoneWindowUI>();
                window.ShowEditorPreview(catalog, destination);
                var travel = root.GetComponentsInChildren<UnityEngine.UI.Button>().First(b => b.name == "Travel");
                if (destination != SceneManager.GetActiveScene().name)
                {
                    // Editor preview is outside gameplay; show the available travel appearance.
                    travel.interactable = true;
                    travel.GetComponent<UnityEngine.UI.Image>().color = Color.white;
                }
                foreach (var item in root.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 31;
                camera.transform.position = new Vector3(1000, 1000, -10);
                camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.06f, .08f, .1f);
                camera.targetTexture = rt;
                var canvas = root.GetComponentInChildren<Canvas>(true);
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 5;
                Canvas.ForceUpdateCanvases();
                root.GetComponentInChildren<SoulstoneWindowView>(true).FitToCanvas();
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                if (travel.interactable)
                {
                    var graphic = travel.GetComponent<UnityEngine.UI.Image>();
                    if (travel.transition == UnityEngine.UI.Selectable.Transition.SpriteSwap)
                        graphic.overrideSprite = travel.spriteState.highlightedSprite;
                    var effect = travel.GetComponent<MenuButtonHoverEffect>();
                    if (effect != null)
                    {
                        var settings = new SerializedObject(effect);
                        graphic.color = settings.FindProperty("hoverColor").colorValue;
                        travel.transform.localScale *= settings.FindProperty("hoverScale").floatValue;
                    }
                    Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
                    image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
                    File.WriteAllBytes(path.Replace("-UI.png", "-Hover.png"), image.EncodeToPNG());
                }
                foreach (var text in root.GetComponentsInChildren<TMP_Text>())
                    if (text.isTextOverflowing) throw new InvalidOperationException("Soulstone UI text overflow: "
                        + text.transform.parent.name + "/" + text.name + " (" + text.rectTransform.rect.size + ")");
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                Object.DestroyImmediate(root); Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(image); Object.DestroyImmediate(rt);
            }
        }
    }
}
