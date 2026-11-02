using System;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class FrogSpitImpactSetup
    {
        [MenuItem("Eastern Fantasy/Enemies/Setup Frog Spit Impact")]
        public static void Run()
        {
            const string path = "Assets/_Project/Art/Tutorial Map/TutorialMonster/Frog/두꺼비 투사체 추가.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var raw = new Texture2D(2, 2); raw.LoadImage(System.IO.File.ReadAllBytes(path));
            try
            {
                var pixels = raw.GetPixels32();
                var rects = new Rect[3];
                for (int i = 0; i < 3; i++)
                {
                    int left = raw.width * i / 3, right = raw.width * (i + 1) / 3;
                    int minX = right, maxX = left, minY = raw.height, maxY = 0;
                    for (int y = 0; y < raw.height; y++) for (int x = left; x < right; x++)
                        if (pixels[y * raw.width + x].a >= 16)
                        { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
                    rects[i] = new Rect(Math.Max(left, minX - 2), Math.Max(0, minY - 2),
                        Math.Min(right - Math.Max(left, minX - 2), maxX - minX + 5), maxY - minY + 5);
                }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/FrogSpitProjectile.prefab");
                var data = new SerializedObject(prefab.GetComponent<FrogSpitProjectile>());
                var flight = (Sprite)data.FindProperty("animationFrames").GetArrayElementAtIndex(0).objectReferenceValue;
                var visible = PetCompanionPrefabSetup.VisibleBounds(flight);
                float diameter = visible.height / flight.pixelsPerUnit;
                // The first splash matches the flying glob; the second naturally expands.
                float ppu = rects[0].height / (diameter * 1.25f);
                var frames = data.FindProperty("impactFrames"); frames.arraySize = 3;
                for (int i = 0; i < 3; i++)
                {
                    var sprite = Sprite.Create(texture, rects[i], new Vector2(.5f, .5f), ppu, 0, SpriteMeshType.FullRect);
                    sprite.name = "Frog_SpitImpactFrame" + (i + 1);
                    string assetPath = "Assets/_Project/Animations/TutoMonster/Frog/" + sprite.name + ".asset";
                    var saved = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (saved == null) { saved = sprite; AssetDatabase.CreateAsset(saved, assetPath); }
                    else { EditorUtility.CopySerialized(sprite, saved); UnityEngine.Object.DestroyImmediate(sprite); EditorUtility.SetDirty(saved); }
                    frames.GetArrayElementAtIndex(i).objectReferenceValue = saved;
                }
                data.FindProperty("impactFramesPerSecond").floatValue = 10f;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(prefab); AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
        }
    }
}
