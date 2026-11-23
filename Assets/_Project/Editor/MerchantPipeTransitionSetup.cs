using System;
using System.Linq;
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
        private const string IdleSheet = TempleGateBlacksmithSetup.Art + "원숭이 상인 대기 선명화.png";
        private const string BridgeSheet = TempleGateBlacksmithSetup.Art + "원숭이 상인 연결동작 선명화.png";
        internal const string PipeSheet = TempleGateBlacksmithSetup.Art + "원숭이 상인 곰방대 8프레임.png";
        internal const int ExhalePoseIndex = 7;

        [MenuItem("Eastern Fantasy/Old Temple/Configure Merchant Pipe Transitions")]
        public static string Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop play mode before editing the merchant.");
            var scene = SceneManager.GetSceneByPath(TempleGateMerchantSetup.ScenePath); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(TempleGateMerchantSetup.ScenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("Save TempleGate before rebuilding the merchant poses.");
                var (idle, bridge, pipe) = ImportCharacterPoses();
                TempleGateMerchantSetup.MakeIdleClip(idle);
                ConfigureClips(idle[0], pipe, bridge);
                var npc = scene.GetRootGameObjects().Single(g => g.name == TempleGateMerchantSetup.ObjectName);
                var renderer = npc.GetComponent<SpriteRenderer>();
                renderer.sprite = idle[0];
                renderer.sharedMaterial = ToneMaterial(IdleSheet);
                var data = new SerializedObject(npc.GetComponent<MonkeyMerchantAmbientAnimation>());
                data.FindProperty("transitionDuration").floatValue = MonkeyMerchantAmbientAnimation.EnterPipeDuration;
                data.FindProperty("pipeDuration").floatValue = MonkeyMerchantAmbientAnimation.PipeDuration;
                data.FindProperty("idleAnchorFrame").objectReferenceValue = idle[0];
                MerchantPipeSmokeSetup.Configure(data, pipe[ExhalePoseIndex]);
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); Selection.activeGameObject = npc;
                return "Aligned the merchant idle, bridge, and pipe scale and color tone.";
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        internal static void ConfigureClips(Sprite idle, Sprite[] pipe, Sprite[] bridge)
        {
            // The sheet starts at the lips and ends in the resting pose.
            var enter = Clip(MonkeyMerchantAmbientAnimation.EnterPipeState,
                new[] { idle }.Concat(bridge.Reverse()).Concat(new[] { pipe[0] }).ToArray(),
                new[] { 0f, .12f, .24f, .36f, .48f, .60f, .72f, .84f, .96f, 1.08f },
                MonkeyMerchantAmbientAnimation.EnterPipeDuration);
            // Skip the bridge's two mouth poses so the pipe keeps moving away after the exhale.
            var exit = Clip(MonkeyMerchantAmbientAnimation.ReturnFromPipeState,
                new[] { pipe[ExhalePoseIndex] }.Concat(bridge.Skip(2)).Concat(new[] { idle }).ToArray(),
                new[] { 0f, .14f, .28f, .43f, .58f, .73f, .88f, 1.03f },
                MonkeyMerchantAmbientAnimation.ReturnFromPipeDuration);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(TempleGateMerchantSetup.AnimationFolder + "/MonkeyMerchant.controller");
            var machine = controller.layers[0].stateMachine;
            foreach (var clip in new[] { enter, ConfigurePipeClip(pipe), exit })
            {
                var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == clip.name) ?? machine.AddState(clip.name);
                state.motion = clip;
                state.speed = 1f;
                EditorUtility.SetDirty(state);
            }
            EditorUtility.SetDirty(machine);
            EditorUtility.SetDirty(controller);
        }

        internal static AnimationClip ConfigurePipeClip(Sprite[] pipe)
        {
            // The bridge raises the pipe. These frames inhale, release the stem and exhale.
            return Clip(MonkeyMerchantAmbientAnimation.PipeState, pipe,
                new[] { 0f, .22f, .44f, .66f, .91f, 1.20f, 1.49f, 1.77f },
                MonkeyMerchantAmbientAnimation.PipeDuration);
        }

        internal static (Sprite[] idle, Sprite[] bridge, Sprite[] pipe) ImportCharacterPoses()
        {
            return (TempleGateMerchantSetup.Slice(IdleSheet, rows: 2), TempleGateMerchantSetup.Slice(BridgeSheet, rows: 2),
                TempleGateMerchantSetup.Slice(PipeSheet, rows: 2));
        }

        internal static Material IdleToneMaterial() => ToneMaterial(IdleSheet);

        internal static void ApplyTone(AnimationClip clip, ObjectReferenceKeyframe[] spriteKeys)
        {
            var materials = new System.Collections.Generic.Dictionary<string, Material>();
            var keys = spriteKeys.Select(key =>
            {
                string sheet = AssetDatabase.GetAssetPath(key.value);
                if (!materials.TryGetValue(sheet, out var material))
                {
                    material = ToneMaterial(sheet);
                    materials.Add(sheet, material);
                }
                return new ObjectReferenceKeyframe { time = key.time, value = material };
            }).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Materials.Array.data[0]"), keys);
        }

        private static Material ToneMaterial(string sheet)
        {
            var (label, brightness, contrast, saturation, tint) = sheet switch
            {
                IdleSheet => ("Idle", 1f, 1f, 1f, Color.white),
                BridgeSheet => ("Bridge", .975f, 1.05f, .96f, Color.white),
                PipeSheet => ("Pipe", .82f, 1.13f, .94f, new Color(.97f, 1.02f, 1.04f).gamma),
                _ => throw new InvalidOperationException("Unknown merchant sheet: " + sheet)
            };
            var path = TempleGateMerchantSetup.AnimationFolder + "/MonkeyMerchantTone" + label + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Eastern Fantasy/Village Harmony");
                if (shader == null) throw new InvalidOperationException("Missing sprite color grading shader.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetFloat("_Brightness", brightness);
            material.SetFloat("_Contrast", contrast);
            material.SetFloat("_Saturation", saturation);
            material.SetColor("_Color", tint);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static AnimationClip Clip(string name, Sprite[] poses, float[] times, float duration)
        {
            string path = TempleGateMerchantSetup.AnimationFolder + "/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.name = name; clip.frameRate = 100f;
            var keys = poses.Select((s, i) => new ObjectReferenceKeyframe { time = times[i], value = s }).ToList();
            float lastSample = (Mathf.RoundToInt(duration * clip.frameRate) - 1) / clip.frameRate;
            keys.Add(new ObjectReferenceKeyframe { time = lastSample, value = poses[poses.Length - 1] });
            var spriteKeys = keys.ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), spriteKeys);
            ApplyTone(clip, spriteKeys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = false; settings.startTime = 0f; settings.stopTime = duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings); EditorUtility.SetDirty(clip); return clip;
        }
    }
}
