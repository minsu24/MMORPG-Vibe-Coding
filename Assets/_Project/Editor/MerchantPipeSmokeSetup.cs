using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EasternFantasy.Shop;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class MerchantPipeSmokeSetup
    {
        private const string Art = TempleGateBlacksmithSetup.Art;
        private const string SmokePath = Art + "곰방대 연기.png";

        [MenuItem("Eastern Fantasy/Old Temple/Configure Detached Merchant Pipe Smoke")]
        public static string Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before saving the NPC.");
            var scene = SceneManager.GetSceneByPath(TempleGateMerchantSetup.ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(TempleGateMerchantSetup.ScenePath, OpenSceneMode.Additive);
            try
            {
                var npc = scene.GetRootGameObjects().Single(g => g.name == TempleGateMerchantSetup.ObjectName);
                var frames = AssetDatabase.LoadAllAssetsAtPath(Art + "원숭이 상인 곰방대.png").OfType<Sprite>().OrderBy(s => s.name).ToArray();
                var data = new SerializedObject(npc.GetComponent<MonkeyMerchantAmbientAnimation>());
                Configure(data, frames[7]); data.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Selection.activeGameObject = npc;
                return "Eighth frame smoke matched; detached smoke drifts up-right for 1.6 seconds while the merchant returns to idle.";
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        public static void Configure(SerializedObject data, Sprite finalPipeFrame)
        {
            var frames = SliceSmoke();
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                ImageConversion.LoadImage(raw, File.ReadAllBytes(Art + "원숭이 상인 곰방대.png"));
                var pixels = raw.GetPixels32(); var rect = finalPipeFrame.rect;
                int left = raw.width, right = 0, bottom = raw.height, top = 0;
                int largest = 0;
                Vector2 cloudCenter = Vector2.zero;
                var visited = new HashSet<int>();
                for (int x = (int)rect.xMin; x < rect.xMax; x++)
                    for (int y = Mathf.Max((int)rect.yMin, raw.height / 2); y < rect.yMax; y++)
                    {
                        int index = y * raw.width + x;
                        if (visited.Contains(index) || !IsSmoke(pixels[index])) continue;
                        var queue = new Queue<Vector2Int>(); queue.Enqueue(new Vector2Int(x, y)); visited.Add(index);
                        int count = 0, l = x, r = x, b = y, t = y;
                        double mass = 0, weightedX = 0, weightedY = 0;
                        while (queue.Count > 0)
                        {
                            var point = queue.Dequeue(); count++;
                            double weight = pixels[point.y * raw.width + point.x].a / 255.0;
                            mass += weight; weightedX += (point.x + .5) * weight; weightedY += (point.y + .5) * weight;
                            l = Mathf.Min(l, point.x); r = Mathf.Max(r, point.x);
                            b = Mathf.Min(b, point.y); t = Mathf.Max(t, point.y);
                            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                            {
                                int nx = point.x + dx, ny = point.y + dy;
                                if (nx < rect.xMin || nx >= rect.xMax || ny < raw.height / 2 || ny >= rect.yMax) continue;
                                int next = ny * raw.width + nx;
                                if (visited.Contains(next) || !IsSmoke(pixels[next])) continue;
                                visited.Add(next); queue.Enqueue(new Vector2Int(nx, ny));
                            }
                        }
                        // The cloud is the largest connected light region; facial highlights are separate.
                        if (count > largest)
                        {
                            largest = count; left = l; right = r; bottom = b; top = t;
                            cloudCenter = new Vector2((float)(weightedX / mass), (float)(weightedY / mass));
                        }
                    }
                if (left > right || bottom > top) throw new InvalidOperationException("Could not locate the last frame's white smoke.");
                var array = data.FindProperty("smokeFrames"); array.arraySize = frames.Length;
                for (int i = 0; i < frames.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                data.FindProperty("smokeLocalOffset").vector2Value = (cloudCenter - rect.position - finalPipeFrame.pivot) / finalPipeFrame.pixelsPerUnit;
                data.FindProperty("smokeLocalSize").vector2Value = new Vector2(right - left + 1, top - bottom + 1) / finalPipeFrame.pixelsPerUnit;
                data.FindProperty("smokeDrift").vector2Value = new Vector2(.45f, .65f);
                data.FindProperty("smokeLifetime").floatValue = 1.6f;
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
        }

        private static bool IsSmoke(Color32 p) => p.a >= 128 && p.r > 180 && p.g > 170 && p.b > 150 && Mathf.Abs(p.r - p.b) < 65;

        private static Sprite[] SliceSmoke()
        {
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                ImageConversion.LoadImage(raw, File.ReadAllBytes(SmokePath));
                var pixels = raw.GetPixels32();
                var spans = new List<Vector2Int>(); int start = -1, last = -1;
                for (int x = 0; x < raw.width; x++)
                {
                    bool occupied = false;
                    for (int y = 0; y < raw.height; y++)
                        if (pixels[y * raw.width + x].a >= 32) { occupied = true; break; }
                    if (!occupied) continue;
                    if (start >= 0 && x - last > 16) { spans.Add(new Vector2Int(start, last)); start = -1; }
                    if (start < 0) start = x;
                    last = x;
                }
                if (start >= 0) spans.Add(new Vector2Int(start, last));
                if (spans.Count != 8) throw new InvalidOperationException("Expected eight separated smoke frames.");
                var rects = spans.Select(span =>
                {
                    int bottom = raw.height, top = 0;
                    for (int x = span.x; x <= span.y; x++) for (int y = 0; y < raw.height; y++)
                        if (pixels[y * raw.width + x].a >= 32) { bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                    int left = Mathf.Max(0, span.x - 2), right = Mathf.Min(raw.width - 1, span.y + 2);
                    bottom = Mathf.Max(0, bottom - 2); top = Mathf.Min(raw.height - 1, top + 2);
                    return new Rect(left, bottom, right - left + 1, top - bottom + 1);
                }).ToArray();
                var pivots = rects.Select(rect =>
                {
                    double mass = 0, weightedX = 0, weightedY = 0;
                    for (int y = (int)rect.yMin; y < rect.yMax; y++) for (int x = (int)rect.xMin; x < rect.xMax; x++)
                    {
                        double alpha = pixels[y * raw.width + x].a / 255.0;
                        mass += alpha; weightedX += (x + .5 - rect.xMin) * alpha; weightedY += (y + .5 - rect.yMin) * alpha;
                    }
                    // Anchor the visible smoke mass, not a bounding box whose tail changes each frame.
                    return new Vector2((float)(weightedX / mass / rect.width), (float)(weightedY / mass / rect.height));
                }).ToArray();
                SkullMonkMeleeSetup.Slice(SmokePath, rects, 136f, pivots);
                var importer = (TextureImporter)AssetImporter.GetAtPath(SmokePath);
                importer.filterMode = FilterMode.Bilinear; importer.npotScale = TextureImporterNPOTScale.None;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
            return AssetDatabase.LoadAllAssetsAtPath(SmokePath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        }
    }
}
