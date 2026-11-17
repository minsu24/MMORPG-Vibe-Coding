using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class SkullMonkPrayerHighResolutionSetup
    {
        private const string Art = "Assets/_Project/Art/Old Temple/Monster/SkullMonk/";
        private const string Rise = Art + "SkullMonkPrayerRise_HD.png";
        private const string Peak = Art + "SkullMonkPrayerPeak_HD.png";
        private const string Fade = Art + "SkullMonkPrayerFade_HD.png";

        [MenuItem("Eastern Fantasy/Skull Monk/Apply High Resolution Prayer Eruptions")]
        public static string Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            Slice(Rise, 4); Slice(Peak, 4); Slice(Fade, 5);
            var scene = SceneManager.GetSceneByPath(SkullMonkWaveSetup.ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(SkullMonkWaveSetup.ScenePath, OpenSceneMode.Additive);
            try
            {
                var boss = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MON_SkullMonk>(true)).Single();
                var data = new SerializedObject(boss);
                if (!AssignFrames(data)) throw new InvalidOperationException("Expected eight eruption and five dissolve frames.");
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                return "Eight high-resolution eruption frames and five dissolve frames applied with grounded pivots.";
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        public static bool AssignFrames(SerializedObject data)
        {
            var eruption = Frames(Rise).Concat(Frames(Peak)).ToArray();
            var fade = Frames(Fade);
            if (eruption.Length != 8 || fade.Length != 5) return false;
            Assign(data, "prayerExplosionFrames", eruption);
            Assign(data, "prayerDissolveFrames", fade);
            return true;
        }

        private static Sprite[] Frames(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        private static void Assign(SerializedObject data, string name, Sprite[] frames)
        {
            var property = data.FindProperty(name); property.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

        private static void Slice(string path, int count)
        {
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(raw, File.ReadAllBytes(path))) throw new InvalidOperationException("Cannot read " + path);
                var pixels = raw.GetPixels32();
                var grounds = new float[count];
                int top = 0;
                for (int i = 0; i < count; i++)
                {
                    int left = Mathf.RoundToInt(i * raw.width / (float)count);
                    int right = Mathf.RoundToInt((i + 1) * raw.width / (float)count);
                    int bottom = raw.height;
                    for (int y = 0; y < raw.height; y++)
                        for (int x = left; x < right; x++)
                            if (pixels[y * raw.width + x].a >= 64)
                            { bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                    if (bottom == raw.height) throw new InvalidOperationException("Empty frame in " + path);
                    // Place the pivot at the bright rupture just above its shallow ground dust.
                    grounds[i] = bottom + raw.height * .012f;
                }
                int cellHeight = Mathf.Min(raw.height, top + 3);
                var rects = Enumerable.Range(0, count).Select(i =>
                {
                    int left = Mathf.RoundToInt(i * raw.width / (float)count);
                    int right = Mathf.RoundToInt((i + 1) * raw.width / (float)count);
                    return new Rect(left, 0, right - left, cellHeight);
                }).ToArray();
                SkullMonkMeleeSetup.Slice(path, rects, raw.height / 12f,
                    grounds.Select(g => new Vector2(.5f, g / cellHeight)).ToArray());
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.maxTextureSize = 4096;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.filterMode = FilterMode.Bilinear;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
        }
    }
}
