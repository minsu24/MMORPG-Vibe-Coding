using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class SkullMonkImpactValidation
    {
        const string Art = "Assets/_Project/Art/Old Temple/Monster/SkullMonk/";
        const string Folder = "Artifacts/SkullMonkImpact/";

        public static void Render()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Old Temple/SkullMonk/SkullMonkAttack.anim");
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, AnimationUtility.GetObjectReferenceCurveBindings(clip)[0]);
            var fxFrames = AssetDatabase.LoadAllAssetsAtPath(Art + "SkullMonkMelee_Effect.png").OfType<Sprite>().OrderBy(s => s.name).ToArray();
            var root = new GameObject("Impact preview"); root.layer = 31;
            var actor = root.AddComponent<SpriteRenderer>();
            var fxObject = new GameObject("Burst"); fxObject.layer = 31; fxObject.transform.SetParent(root.transform, false);
            var fx = fxObject.AddComponent<SpriteRenderer>(); fx.sortingOrder = 1;
            var cameraObject = new GameObject("Impact preview camera");
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 3.7f;
            camera.transform.position = new Vector3(3, -.05f, -10); camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.075f, .11f, .14f);
            var rt = new RenderTexture(960, 576, 24); camera.targetTexture = rt;
            var capture = new Texture2D(960, 576, TextureFormat.RGBA32, false);
            Directory.CreateDirectory(Folder + "frames");
            try
            {
                for (int i = 0; i < 80; i++)
                {
                    float t = i / 60f;
                    actor.sprite = (Sprite)keys.Last(k => k.time <= Mathf.Min(t, clip.length)).value;
                    float age = t - .49f; fx.enabled = age >= 0 && age < .22f;
                    if (fx.enabled)
                    {
                        MON_SkullMonk.EvaluateThrustBurst(age, out int frame, out float travel, out Vector2 size, out Color color);
                        fx.sprite = fxFrames[frame]; fx.color = color;
                        fxObject.transform.localPosition = new Vector3(2.7f + .95f * travel, .45f, 0);
                        fxObject.transform.localScale = new Vector3(size.x, size.y, 1);
                    }
                    camera.Render(); RenderTexture.active = rt;
                    capture.ReadPixels(new Rect(0, 0, 960, 576), 0, 0); capture.Apply();
                    File.WriteAllBytes(Folder + "frames/" + i.ToString("D3") + ".png", capture.EncodeToPNG());
                    if (i == 32) File.WriteAllBytes(Folder + "impact.png", capture.EncodeToPNG());
                }
                File.WriteAllText(Folder + "timing.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    keys = keys.Select(k => new { time = k.time, sprite = k.value.name }),
                    events = AnimationUtility.GetAnimationEvents(clip).Select(e => new { time = e.time, name = e.functionName })
                }, Newtonsoft.Json.Formatting.Indented));
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
