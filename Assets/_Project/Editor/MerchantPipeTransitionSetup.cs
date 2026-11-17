using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EasternFantasy.Shop;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class MerchantPipeTransitionSetup
    {
        public const string SheetPath = "Assets/_Project/Art/Old Temple/NPC/원숭이 상인 곰방대 연결.png";
        public const string FramesFolder = "Assets/_Project/Art/Old Temple/NPC/MonkeyMerchantTransition";
        public const float Duration = .64f;

        [MenuItem("Eastern Fantasy/Old Temple/Configure Merchant Pipe Transitions")]
        public static string Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop play mode before editing the merchant.");
            var idle = First("원숭이 상인 대기.png"); var pipe = First("원숭이 상인 곰방대.png");
            var frames = ImportFrames(idle, pipe);
            var poses = new[] { idle }.Concat(frames).Concat(new[] { pipe }).ToArray();
            var enter = Clip(MonkeyMerchantAmbientAnimation.EnterPipeState, poses);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(TempleGateMerchantSetup.AnimationFolder + "/MonkeyMerchant.controller");
            foreach (var clip in new[] { enter })
            {
                var machine = controller.layers[0].stateMachine;
                var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == clip.name) ?? machine.AddState(clip.name);
                state.motion = clip; state.speed = 1f; EditorUtility.SetDirty(state); EditorUtility.SetDirty(machine);
            }
            EditorUtility.SetDirty(controller);
            // Only the entry is extended. The established eighth-frame smoke/idle handoff stays immediate.
            var unusedReturn = controller.layers[0].stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == "MonkeyMerchantReturnFromPipe");
            if (unusedReturn != null) controller.layers[0].stateMachine.RemoveState(unusedReturn);
            string unusedReturnPath = TempleGateMerchantSetup.AnimationFolder + "/MonkeyMerchantReturnFromPipe.anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(unusedReturnPath) != null) AssetDatabase.DeleteAsset(unusedReturnPath);
            var scene = SceneManager.GetSceneByPath(TempleGateMerchantSetup.ScenePath); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(TempleGateMerchantSetup.ScenePath, OpenSceneMode.Additive);
            try
            {
                var npc = scene.GetRootGameObjects().Single(g => g.name == TempleGateMerchantSetup.ObjectName);
                var data = new SerializedObject(npc.GetComponent<MonkeyMerchantAmbientAnimation>());
                data.FindProperty("transitionDuration").floatValue = Duration;
                data.FindProperty("idleAnchorFrame").objectReferenceValue = idle;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); Selection.activeGameObject = npc;
                return "Added six new in-between poses, exact original endpoint sprites, 0.64s entry, grounded individual pivots and source-matched sizes. Existing smoke/idle handoff preserved.";
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        private static Sprite First(string name) => AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Old Temple/NPC/" + name).OfType<Sprite>().OrderBy(s => s.name).First();

        private static Sprite[] ImportFrames(Sprite idle, Sprite pipe)
        {
            Directory.CreateDirectory(FramesFolder); AssetDatabase.Refresh();
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                ImageConversion.LoadImage(raw, File.ReadAllBytes(SheetPath));
                var result = new Sprite[6];
                for (int i = 0; i < 6; i++)
                {
                    int x = Mathf.RoundToInt(i % 3 * raw.width / 3f), xEnd = Mathf.RoundToInt((i % 3 + 1) * raw.width / 3f);
                    int y = Mathf.RoundToInt((1 - i / 3) * raw.height / 2f), yEnd = Mathf.RoundToInt((2 - i / 3) * raw.height / 2f);
                    var cell = new Texture2D(xEnd - x, yEnd - y, TextureFormat.RGBA32, false);
                    try
                    {
                        cell.SetPixels(raw.GetPixels(x, y, cell.width, cell.height)); cell.Apply();
                        var pixels = cell.GetPixels32();
                        int left = cell.width, right = 0, bottom = cell.height, top = 0;
                        for (int cy = 0; cy < cell.height; cy++) for (int cx = 0; cx < cell.width; cx++)
                            if (pixels[cy * cell.width + cx].a >= 64)
                            { left = Mathf.Min(left, cx); right = Mathf.Max(right, cx); bottom = Mathf.Min(bottom, cy); top = Mathf.Max(top, cy); }
                        if (right <= left || top <= bottom) throw new InvalidOperationException("Missing transition pose " + i);
                        float t = (i + 1f) / 7f;
                        // Individual import PPU matches the existing poses without changing the NPC transform or proportions.
                        float desiredHeight = Mathf.Lerp(idle.bounds.size.y, pipe.bounds.size.y, t);
                        float ppu = (top - bottom + 1) / desiredHeight;
                        float footBand = 36f * ppu / 136f;
                        var foot = new List<Vector2Int>();
                        for (int cy = bottom; cy < bottom + footBand && cy < cell.height; cy++) for (int cx = left; cx <= right; cx++)
                            if (cx > left + (right - left) * .65f && pixels[cy * cell.width + cx].a >= 128) foot.Add(new Vector2Int(cx, cy));
                        if (foot.Count == 0) throw new InvalidOperationException("Missing planted foot in transition " + i);
                        string path = FramesFolder + "/MonkeyMerchantBridge_" + i.ToString("D2") + ".png";
                        File.WriteAllBytes(path, cell.EncodeToPNG()); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                        importer.spritePixelsPerUnit = ppu; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
                        importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.npotScale = TextureImporterNPOTScale.None; importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
                        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                        settings.spriteMeshType = SpriteMeshType.Tight; settings.spriteAlignment = (int)SpriteAlignment.Custom;
                        settings.spritePivot = new Vector2(((float)foot.Average(p => p.x) - 70f / 136f * ppu) / cell.width, (float)foot.Min(p => p.y) / cell.height);
                        importer.SetTextureSettings(settings); importer.SaveAndReimport();
                        result[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(cell); }
                }
                return result;
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
        }

        private static AnimationClip Clip(string name, Sprite[] poses)
        {
            string path = TempleGateMerchantSetup.AnimationFolder + "/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.name = name; clip.frameRate = 100f;
            var keys = poses.Select((s, i) => new ObjectReferenceKeyframe { time = i * Duration / poses.Length, value = s }).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = false; settings.startTime = 0f; settings.stopTime = Duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings); EditorUtility.SetDirty(clip); return clip;
        }
    }
}
