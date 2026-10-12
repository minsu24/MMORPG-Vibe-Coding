using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasternFantasy.Inventory;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class ItemIconAlignmentSetup
    {
        [MenuItem("Eastern Fantasy/Inventory/Center All Item Icons")]
        public static void Run()
        {
            var targets = new Dictionary<string, HashSet<string>>();
            foreach (string guid in AssetDatabase.FindAssets("t:ItemDefinition"))
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                Add(targets, item.Icon);
                Add(targets, item.GradeFrame);
            }
            int adjusted = 0;
            foreach (var pair in targets)
            {
                var importer = AssetImporter.GetAtPath(pair.Key) as TextureImporter;
                if (importer == null) continue;
                if (importer.spriteImportMode != SpriteImportMode.Multiple)
                    throw new InvalidOperationException("Centering requires sliced sprites: " + pair.Key);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!texture.LoadImage(File.ReadAllBytes(pair.Key)))
                        throw new InvalidOperationException("Cannot read icon texture: " + pair.Key);
                    Color32[] pixels = texture.GetPixels32();
                    var factories = new SpriteDataProviderFactories();
                    factories.Init();
                    var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
                    provider.InitSpriteEditorDataProvider();
                    SpriteRect[] rects = provider.GetSpriteRects();
                    bool changed = false;
                    foreach (SpriteRect sprite in rects)
                    {
                        if (!pair.Value.Contains(sprite.name)) continue;
                        Rect bounds = ArmorSetImageSetup.IsSquareIcon(pair.Key) ? new Rect(0, 0, texture.width, texture.height) : VisibleBounds(pixels, texture.width, texture.height, sprite.rect);
                        if (sprite.rect == bounds && sprite.pivot == new Vector2(.5f, .5f) &&
                            sprite.alignment == (int)SpriteAlignment.Center) continue;
                        sprite.rect = bounds;
                        sprite.pivot = new Vector2(.5f, .5f);
                        sprite.alignment = (int)SpriteAlignment.Center;
                        changed = true;
                        adjusted++;
                    }
                    if (!changed) continue;
                    provider.SetSpriteRects(rects);
                    provider.Apply();
                    AssetDatabase.ImportAsset(pair.Key, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            Debug.Log("Centered item icons and frames; updated " + adjusted + " sprite rectangles (sprite IDs preserved).");
        }

        private static void Add(Dictionary<string, HashSet<string>> targets, Sprite sprite)
        {
            if (sprite == null) return;
            string path = AssetDatabase.GetAssetPath(sprite);
            if (!targets.TryGetValue(path, out var names))
            {
                names = new HashSet<string>();
                targets.Add(path, names);
            }
            names.Add(sprite.name);
        }

        internal static Rect VisibleBounds(Color32[] pixels, int width, int height, Rect region)
        {
            int left = Mathf.Clamp(Mathf.RoundToInt(region.xMin), 0, width);
            int right = Mathf.Clamp(Mathf.RoundToInt(region.xMax), 0, width);
            int bottom = Mathf.Clamp(Mathf.RoundToInt(region.yMin), 0, height);
            int top = Mathf.Clamp(Mathf.RoundToInt(region.yMax), 0, height);
            int minX = right, minY = top, maxX = left - 1, maxY = bottom - 1;
            for (int y = bottom; y < top; y++)
                for (int x = left; x < right; x++)
                {
                    // Include every non-transparent pixel, including faint effects.
                    if (pixels[y * width + x].a == 0) continue;
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                }
            return maxX < minX ? region : new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
    }
}