using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class SkullMonkGiantStaffImpactSetup
    {
        public const string FileName = "SkullMonkGiantStaffImpact_HD.png";
        public const string SpritePath = SkullMonkWaveSetup.Art + FileName;

        [MenuItem("Eastern Fantasy/Skull Monk/Apply Painted Giant Staff Fracture")]
        public static string Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(raw, File.ReadAllBytes(SpritePath));
            int width = raw.width, height = raw.height;
            UnityEngine.Object.DestroyImmediate(raw);
            // Keep the complete native-width strip in every frame. A fixed camera,
            // shared ground pivot and equal strip extents prevent footpoint drift.
            var cells = Enumerable.Range(0, 8).Select(i =>
            {
                int bottom = Mathf.RoundToInt(height * (7 - i) / 8f);
                int top = Mathf.RoundToInt(height * (8 - i) / 8f);
                return new Rect(0, bottom, width, top - bottom);
            }).ToArray();
            SkullMonkMeleeSetup.Slice(SpritePath, cells, 100f,
                cells.Select(r => new Vector2(.5f, .25f)).ToArray());
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.maxTextureSize = 4096; importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
            var frames = SkullMonkWaveSetup.Frames(FileName);
            if (frames.Length != 8) throw new InvalidOperationException("Expected eight fracture frames.");
            var scene = SceneManager.GetSceneByPath(SkullMonkWaveSetup.ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(SkullMonkWaveSetup.ScenePath, OpenSceneMode.Additive);
            var boss = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MON_SkullMonk>(true)).Single();
            var data = new SerializedObject(boss); var sprites = data.FindProperty("giantStaffImpactFrames");
            sprites.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) sprites.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            data.FindProperty("giantStaffImpactHeight").floatValue = 2.4f;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            if (opened) EditorSceneManager.CloseScene(scene, true);
            return $"Imported {width}x{height}, eight full-width alpha frames, 2.4-unit ground depth.";
        }
    }
}
