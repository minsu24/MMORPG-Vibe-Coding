using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class TempleGateMerchantStallSetup
    {
        public const string ArtPath = "Assets/_Project/Art/Old Temple/NPC/원숭이 상인 노점 배경조화.png";

        [MenuItem("Eastern Fantasy/Old Temple/Apply Muted Merchant Stall")]
        public static string Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop play mode before saving the stall.");
            var scene = SceneManager.GetSceneByPath(TempleGateMerchantSetup.ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(TempleGateMerchantSetup.ScenePath, OpenSceneMode.Additive);
            try
            {
                var npc = scene.GetRootGameObjects().Single(g => g.name == TempleGateMerchantSetup.ObjectName);
                var stall = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SpriteRenderer>(true))
                    .FirstOrDefault(r => r.sprite != null && (AssetDatabase.GetAssetPath(r.sprite).Contains("원숭이 상인 노점 수정") || AssetDatabase.GetAssetPath(r.sprite) == ArtPath));
                if (stall == null)
                {
                    var go = new GameObject("MonkeyMerchantStall"); SceneManager.MoveGameObjectToScene(go, scene);
                    go.transform.position = npc.transform.position + new Vector3(0f, .55f, 0f);
                    stall = go.AddComponent<SpriteRenderer>();
                    var source = npc.GetComponent<SpriteRenderer>();
                    stall.sharedMaterial = source.sharedMaterial; stall.sortingLayerID = source.sortingLayerID; stall.sortingOrder = source.sortingOrder + 1;
                }
                float canvasWidth = stall.sprite != null ? stall.sprite.rect.width / stall.sprite.pixelsPerUnit : 3.344f;
                float groundOffset = stall.sprite != null ? OpaqueBottom(AssetDatabase.GetAssetPath(stall.sprite), stall.sprite) : -.55f;
                AssetDatabase.ImportAsset(ArtPath, ImportAssetOptions.ForceUpdate);
                var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    ImageConversion.LoadImage(raw, File.ReadAllBytes(ArtPath));
                    float ppu = raw.width / canvasWidth;
                    int bottom = AlphaBottom(raw);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath);
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = ppu; importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
                    importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
                    var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = new Vector2(.5f, (bottom - groundOffset * ppu) / raw.height);
                    settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
                }
                finally { UnityEngine.Object.DestroyImmediate(raw); }
                stall.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
                EditorUtility.SetDirty(stall); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Selection.activeGameObject = stall.gameObject;
                return "Muted stall applied with original placement, width, floor contact and render order preserved.";
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        private static float OpaqueBottom(string path, Sprite sprite)
        {
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                ImageConversion.LoadImage(raw, File.ReadAllBytes(path));
                return (AlphaBottom(raw) - sprite.rect.y - sprite.pivot.y) / sprite.pixelsPerUnit;
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
        }

        private static int AlphaBottom(Texture2D raw)
        {
            var pixels = raw.GetPixels32();
            for (int y = 0; y < raw.height; y++) for (int x = 0; x < raw.width; x++)
                if (pixels[y * raw.width + x].a >= 64) return y;
            throw new InvalidOperationException("Stall image has no opaque pixels.");
        }
    }
}
