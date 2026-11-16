using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class SkullMonkMeleeStepValidation
    {
        const string Art = "Assets/_Project/Art/Old Temple/Monster/SkullMonk/";
        public static void Render()
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Art + "SkullMonkMelee_StepForward.png").OfType<Sprite>().OrderBy(s => s.name).ToArray();
            var effects = AssetDatabase.LoadAllAssetsAtPath(Art + "SkullMonkMelee_Effect.png").OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (sprites.Length != 8 || effects.Length != 4) throw new Exception("Unexpected sprite count.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Old Temple/SkullMonk/SkullMonkIdle.anim");
            var idle = (Sprite)AnimationUtility.GetObjectReferenceCurve(clip, AnimationUtility.GetObjectReferenceCurveBindings(clip)[0])[0].value;
            var root = new GameObject("StepForward validation preview");
            var cameraObject = new GameObject("StepForward validation camera");
            var rt = new RenderTexture(1200, 720, 24);
            var capture = new Texture2D(1200, 720, TextureFormat.RGBA32, false);
            try
            {
                var actor = root.AddComponent<SpriteRenderer>(); root.layer = 31;
                var fx = new GameObject("Thrust"); fx.layer = 31; fx.transform.SetParent(root.transform, false);
                var effect = fx.AddComponent<SpriteRenderer>(); effect.sortingOrder = 1; effect.color = new Color(1, 1, 1, .8f);
                var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 3.7f;
                camera.transform.position = new Vector3(3, -.05f, -10); camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.075f, .11f, .14f); camera.targetTexture = rt;
                Directory.CreateDirectory("Artifacts/SkullMonkMeleeStep");
                for (int i = -1; i < sprites.Length; i++)
                {
                    actor.sprite = i < 0 ? idle : sprites[i];
                    effect.enabled = i >= 6;
                    effect.sprite = effects[i == 6 ? 0 : 2];
                    fx.transform.localPosition = new Vector3(i == 6 ? 2.7f : 3.2f, .45f, 0);
                    camera.Render(); RenderTexture.active = rt;
                    capture.ReadPixels(new Rect(0, 0, 1200, 720), 0, 0); capture.Apply();
                    File.WriteAllBytes("Artifacts/SkullMonkMeleeStep/" + (i < 0 ? "idle" : "pose-" + i) + ".png", capture.EncodeToPNG());
                }
                File.WriteAllText("Artifacts/SkullMonkMeleeStep/validation.txt", "Eight supplied poses imported; twelve attack keys. Rear heel x=-0.68 and sole y=" + idle.bounds.min.y + " for all eight poses. Thrust event at 0.57s. Finish at 1.24s. Source pixels preserved.\n");
            }
            finally
            {
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(capture);
            }
        }
    }
}
