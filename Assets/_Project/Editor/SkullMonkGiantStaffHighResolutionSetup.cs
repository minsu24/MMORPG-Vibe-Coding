using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class SkullMonkGiantStaffHighResolutionSetup
    {
        public const string SpritePath = SkullMonkWaveSetup.Art + "SkullMonkGiantStaff_HD.png";

        [MenuItem("Eastern Fantasy/Skull Monk/Apply High Resolution Giant Staff")]
        public static string Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(raw, File.ReadAllBytes(SpritePath));
            int width = raw.width, height = raw.height;
            Rect rect; Vector2 pivot;
            try
            {
                var pixels = raw.GetPixels32();
                int left = width, right = 0, bottom = height, top = 0, transparent = 0;
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].a < 8) { transparent++; continue; }
                    left = Math.Min(left, x); right = Math.Max(right, x);
                    bottom = Math.Min(bottom, y); top = Math.Max(top, y);
                }
                if (transparent < width * height / 5) throw new InvalidOperationException("Expected a transparent weapon cutout.");
                left = Math.Max(0, left - 2); right = Math.Min(width - 1, right + 2);
                bottom = Math.Max(0, bottom - 2); top = Math.Min(height - 1, top + 2);
                rect = new Rect(left, bottom, right - left + 1, top - bottom + 1);
                // Align the horizontal shaft with the original landing baseline.
                var shaftBottoms = new List<int>();
                for (int x = (int)(width * .65f); x < width * .90f; x++)
                    for (int y = bottom; y <= top; y++) if (pixels[y * width + x].a >= 128) { shaftBottoms.Add(y); break; }
                shaftBottoms.Sort();
                float shaftGround = shaftBottoms[shaftBottoms.Count / 2];
                pivot = new Vector2(.5f, (shaftGround - rect.y) / rect.height);
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
            SkullMonkMeleeSetup.Slice(SpritePath, new[] { rect }, 100f, new[] { pivot });
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.filterMode = FilterMode.Bilinear; importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAllAssetsAtPath(SpritePath).OfType<Sprite>().Single();
            var scene = SceneManager.GetSceneByPath(SkullMonkWaveSetup.ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(SkullMonkWaveSetup.ScenePath, OpenSceneMode.Additive);
            var boss = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MON_SkullMonk>(true)).Single();
            var data = new SerializedObject(boss);
            data.FindProperty("giantStaffHighResolutionSprite").objectReferenceValue = sprite;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            if (opened) EditorSceneManager.CloseScene(scene, true);
            return $"Native image {width}x{height}; weapon rect {rect}; shaft pivot {pivot}; no texture compression.";
        }
    }
}
