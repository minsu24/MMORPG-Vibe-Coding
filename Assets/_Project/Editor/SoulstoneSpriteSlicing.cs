using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasternFantasy.UI;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasternFantasy.EditorTools
{
    public static class SoulstoneSpriteSlicing
    {
        private const string Folder = "Artifacts/SoulstoneSlicing/";
        [Serializable] private sealed class Frame
        {
            public string name, id;
            public Rect rect;
            public Vector2 foot, pivot;
        }
        private sealed class Sheet
        {
            public string path;
            public int width, height;
            public float ppu;
            public Frame[] frames;
        }

        [MenuItem("Eastern Fantasy/Soulstone/Align Sprite Frames")]
        public static void Run()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before slicing.");
            Directory.CreateDirectory(Folder);
            var data = Resources.Load<SoulstoneCatalog>("SoulstoneCatalog");
            var clips = new[] { data.villageActivation, data.villageLoop, data.templeActivation, data.templeLoop };
            var report = new List<object>();
            for (int family = 0; family < 2; family++)
            {
                Vector2 fixedFoot = Vector2.zero;
                for (int stage = 0; stage < 2; stage++)
                {
                    var clip = clips[family * 2 + stage];
                    var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
                    var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                    var original = keys.Select(k => k.value as Sprite).ToArray();
                    if (original.Length != 8 || original.Any(s => s == null)) throw new InvalidOperationException("Expected eight sprite frames: " + clip.name);
                    string path = AssetDatabase.GetAssetPath(original[0]);
                    if (!File.Exists(Folder + family + "-" + stage + ".meta.before"))
                        File.Copy(path + ".meta", Folder + family + "-" + stage + ".meta.before");
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    var factories = new SpriteDataProviderFactories(); factories.Init();
                    var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
                    var rects = provider.GetSpriteRects().OrderBy(r => r.rect.x).ToArray();
                    var sheet = Measure(path, importer.spritePixelsPerUnit, rects);
                    if (stage == 0)
                    {
                        // Keep the first inactive frame's world-space placement, then anchor all other frames to it.
                        var first = original[0];
                        float scale = sheet.width / (float)first.texture.width;
                        var oldPivot = first.pivot * scale;
                        var sourcePoint = sheet.frames[0].foot - new Vector2(first.rect.x, first.rect.y) * scale;
                        fixedFoot = (sourcePoint - oldPivot) / sheet.ppu;
                    }
                    for (int i = 0; i < rects.Length; i++)
                    {
                        var frame = sheet.frames[i];
                        var localFoot = frame.foot - frame.rect.position;
                        var pivotPixels = localFoot - fixedFoot * sheet.ppu;
                        frame.pivot = new Vector2(pivotPixels.x / frame.rect.width, pivotPixels.y / frame.rect.height);
                        rects[i].rect = frame.rect;
                        rects[i].alignment = SpriteAlignment.Custom;
                        rects[i].pivot = frame.pivot;
                    }
                    importer.maxTextureSize = 4096; importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.alphaIsTransparency = true; importer.filterMode = FilterMode.Bilinear;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                    provider.SetSpriteRects(rects); provider.Apply(); importer.SaveAndReimport();
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
                    for (int i = 0; i < 8; i++)
                    {
                        if (!sprites.ContainsKey(sheet.frames[i].name)) throw new InvalidOperationException("Sprite name lost.");
                        if (keys[i].value == null) throw new InvalidOperationException("Animation sprite reference lost.");
                    }
                    report.Add(new { clip = clip.name, path, ppu = sheet.ppu, fixedFoot = new[] { fixedFoot.x, fixedFoot.y },
                        frames = sheet.frames.Select(f => new { f.name, f.id,
                            rect = new[] { f.rect.x, f.rect.y, f.rect.width, f.rect.height },
                            foot = new[] { f.foot.x, f.foot.y }, pivot = new[] { f.pivot.x, f.pivot.y } }).ToArray() });
                }
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText(Folder + "alignment.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
            Debug.Log("Resliced and foot-aligned 32 Soulstone sprites; original textures and sprite IDs retained.");
        }

        public static void RenderContacts(string suffix)
        {
            Directory.CreateDirectory(Folder);
            var data = Resources.Load<SoulstoneCatalog>("SoulstoneCatalog");
            var clips = new[] { data.villageActivation, data.villageLoop, data.templeActivation, data.templeLoop };
            var cameraObject = new GameObject("Soulstone Frame Verification Camera");
            var stone = new GameObject("Soulstone Frame Verification Sprite"); stone.layer = 31;
            var renderer = stone.AddComponent<SpriteRenderer>();
            var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
            camera.orthographic = true; camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.1f,.12f,.14f);
            var target = new RenderTexture(256,512,24); camera.targetTexture = target;
            var tile = new Texture2D(256,512,TextureFormat.RGB24,false);
            var contact = new Texture2D(2048,1024,TextureFormat.RGB24,false);
            var previous = RenderTexture.active;
            try
            {
                for (int family=0;family<2;family++)
                {
                    var first = (Sprite)AnimationUtility.GetObjectReferenceCurve(clips[family*2], AnimationUtility.GetObjectReferenceCurveBindings(clips[family*2])[0])[0].value;
                    string path = AssetDatabase.GetAssetPath(first);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    var factories = new SpriteDataProviderFactories(); factories.Init();
                    var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
                    var measure = Measure(path,importer.spritePixelsPerUnit,provider.GetSpriteRects().OrderBy(r=>r.rect.x).ToArray());
                    float scale = measure.width/(float)first.texture.width;
                    var foot = (measure.frames[0].foot - (first.rect.position+first.pivot)*scale)/measure.ppu;
                    camera.orthographicSize = family==0?2.4f:4.5f; camera.aspect=.5f;
                    camera.transform.position = new Vector3(foot.x,foot.y+(family==0?1.9f:3.6f),-10);
                    for(int stage=0;stage<2;stage++)
                    {
                        var clip=clips[family*2+stage];
                        var keys=AnimationUtility.GetObjectReferenceCurve(clip,AnimationUtility.GetObjectReferenceCurveBindings(clip)[0]);
                        for(int i=0;i<8;i++)
                        {
                            renderer.sprite=(Sprite)keys[i].value;
                            camera.Render(); RenderTexture.active=target;
                            tile.ReadPixels(new Rect(0,0,256,512),0,0); tile.Apply();
                            var pixels=tile.GetPixels32();
                            int line=Mathf.RoundToInt((foot.y-camera.transform.position.y+camera.orthographicSize)/(camera.orthographicSize*2)*512);
                            for(int x=0;x<256;x++) if(x%8<4) pixels[line*256+x]=new Color32(70,230,145,255);
                            contact.SetPixels32(i*256,(1-stage)*512,256,512,pixels);
                        }
                    }
                    contact.Apply(); File.WriteAllBytes(Folder+(family==0?"Village-":"Temple-")+suffix+".png",contact.EncodeToPNG());
                }
            }
            finally
            {
                camera.targetTexture=null; RenderTexture.active=previous;
                Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(stone); Object.DestroyImmediate(target);
                Object.DestroyImmediate(tile); Object.DestroyImmediate(contact);
            }
        }

        private static Sheet Measure(string path, float ppu, SpriteRect[] rects)
        {
            var texture = new Texture2D(2, 2);
            try
            {
                ImageConversion.LoadImage(texture, File.ReadAllBytes(path));
                int width = texture.width, height = texture.height;
                var pixels = texture.GetPixels32();
                int ground = 0;
                // Ignore near-transparent glow and fringe; the first substantial opaque row is the stone's foot.
                for (int y = 0; y < height; y++)
                {
                    int count = 0;
                    for (int x = 0; x < width; x++) if (pixels[y*width+x].a >= 224) count++;
                    if (count > width * .02f) { ground = y; break; }
                }
                int[] profile = new int[width];
                for (int x = 0; x < width; x++)
                    for (int y = ground+12; y <= ground+38; y++)
                        if (pixels[y*width+x].a >= 224) profile[x]++;
                var cuts = new int[9]; cuts[8] = width;
                float nominal = width / 8f;
                for (int i = 1; i < 8; i++)
                {
                    int expected = Mathf.RoundToInt(nominal*i), best = expected;
                    float bestScore = float.MaxValue;
                    for (int x = Mathf.RoundToInt(expected-nominal*.18f); x <= expected+nominal*.18f; x++)
                    {
                        float score = profile[x] + Mathf.Abs(x-expected)*.015f;
                        if (score < bestScore) { bestScore = score; best = x; }
                    }
                    cuts[i] = best;
                }
                var frames = new Frame[8];
                for (int i = 0; i < 8; i++)
                {
                    int left = cuts[i]+1, right = cuts[i+1]-1;
                    int bottom = ground;
                    for (int y = Mathf.Max(0,ground-8); y < ground+15; y++)
                    {
                        int count = 0;
                        for (int x = left; x < right; x++) if (pixels[y*width+x].a >= 224) count++;
                        if (count >= Math.Max(5,(right-left)*.03f)) { bottom = y; break; }
                    }
                    double sum = 0, mass = 0;
                    for (int y = bottom+12; y <= bottom+32; y++)
                        for (int x = left; x < right; x++)
                            if (pixels[y*width+x].a >= 224) { sum += x+.5f; mass++; }
                    if (mass < 50) throw new InvalidOperationException("Could not locate the stone base in " + path + " frame " + i);
                    frames[i] = new Frame {
                        name = rects[i].name, id = rects[i].spriteID.ToString(),
                        rect = new Rect(left, 0, right-left, height), foot = new Vector2((float)(sum/mass),bottom)
                    };
                }
                return new Sheet { path = path, width = width, height = height, ppu = ppu, frames = frames };
            }
            finally { Object.DestroyImmediate(texture); }
        }
    }
}
