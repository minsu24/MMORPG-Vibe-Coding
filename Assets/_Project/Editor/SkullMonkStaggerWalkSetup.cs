using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class SkullMonkStaggerWalkSetup
    {
        const string Path = "Assets/_Project/Animations/Old Temple/SkullMonk/SkullMonkWalk.anim";
        public const string Output = "Artifacts/SkullMonkStaggerWalk/";
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Path);
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keys.Length != 10) throw new InvalidOperationException("Expected the existing ten selected walk poses.");
            Directory.CreateDirectory(Output);
            if (!File.Exists(Output + "WalkBefore.anim")) File.Copy(Path, Output + "WalkBefore.anim");
            // Keep the chosen source frames, their order, scale and foot pivots exactly as authored.
            float[] times = { 0f, .28f, .53f, .75f, .94f, 1.04f, 1.13f, 1.22f, 1.34f, 1.50f };
            for (int i = 0; i < keys.Length; i++) keys[i].time = times[i];
            Undo.RecordObject(clip, "Stagger Skull Monk walking rhythm");
            clip.frameRate = 60f;
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.startTime = 0f; settings.stopTime = 1.7f; settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip); AssetDatabase.SaveAssets();
            File.WriteAllText(Output + "timing.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                duration = 1.7f, keys = keys.Select(k => new { time = k.time, sprite = k.value.name })
            }, Newtonsoft.Json.Formatting.Indented));
        }
        public static void Render()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Path);
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));
            var root = new GameObject("Stagger walk preview"); root.layer = 31;
            var sprite = root.AddComponent<SpriteRenderer>();
            var cameraObject = new GameObject("Stagger walk preview camera");
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 4.15f;
            camera.transform.position = new Vector3(0, 0, -10); camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.075f, .11f, .14f);
            var rt = new RenderTexture(800, 720, 24); camera.targetTexture = rt;
            var capture = new Texture2D(800, 720, TextureFormat.RGBA32, false);
            Directory.CreateDirectory(Output + "frames");
            float traveled = 0;
            try
            {
                for (int i = 0; i < 51; i++)
                {
                    float t = i / 30f;
                    if (i > 0) traveled += 1.5f / 30f * MON_SkullMonk.EvaluateStaggerStride((t - 1f / 60f) / 1.7f);
                    // A constant-speed tracking camera makes the real hesitation and catch-up visible.
                    root.transform.position = new Vector3(traveled - 1.5f * t, 0, 0);
                    sprite.sprite = (Sprite)keys.Last(k => k.time <= t).value;
                    camera.Render(); RenderTexture.active = rt;
                    capture.ReadPixels(new Rect(0, 0, 800, 720), 0, 0); capture.Apply();
                    File.WriteAllBytes(Output + "frames/" + i.ToString("D3") + ".png", capture.EncodeToPNG());
                }
            }
            finally
            {
                RenderTexture.active = null; camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(capture);
            }
        }
    }
}
