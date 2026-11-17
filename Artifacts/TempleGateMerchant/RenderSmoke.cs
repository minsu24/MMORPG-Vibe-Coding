using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using EasternFantasy.Shop;

if (Application.isPlaying) throw new Exception("Stop play mode first");
var scene = SceneManager.GetSceneByPath(EasternFantasy.Editor.TempleGateMerchantSetup.ScenePath);
bool opened = !scene.isLoaded;
if (opened) scene = EditorSceneManager.OpenScene(EasternFantasy.Editor.TempleGateMerchantSetup.ScenePath, OpenSceneMode.Additive);
var npc = scene.GetRootGameObjects().Single(g => g.name == "MonkeyMerchant");
var source = npc.GetComponent<SpriteRenderer>(); var original = source.sprite;
var data = new SerializedObject(npc.GetComponent<MonkeyMerchantAmbientAnimation>());
var frames = data.FindProperty("smokeFrames");
var sprites = Enumerable.Range(0, frames.arraySize).Select(i => (Sprite)frames.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
var cameraObject = new GameObject("Temporary smoke preview camera"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 2.8f;
camera.transform.position = npc.transform.position + new Vector3(0, 1.4f, -20);
camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
var rt = new RenderTexture(900,720,24); camera.targetTexture = rt;
var texture = new Texture2D(900,720,TextureFormat.RGBA32,false); var previous = RenderTexture.active;
MerchantPipeSmoke smoke = null;
try
{
    foreach (var phase in new[] { "before", "handoff", "drifting", "fading" })
    {
        var folder = EasternFantasy.Editor.TempleGateMerchantSetup.AnimationFolder;
        if (phase == "before") AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + "/MonkeyMerchantPipe.anim").SampleAnimation(npc, 2.39f);
        if (phase == "handoff")
        {
            AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + "/MonkeyMerchantIdle.anim").SampleAnimation(npc, 0f);
            smoke = MerchantPipeSmoke.Create(source, sprites, npc.transform.TransformPoint(data.FindProperty("smokeLocalOffset").vector2Value), data.FindProperty("smokeLocalSize").vector2Value, data.FindProperty("smokeDrift").vector2Value, 1.6f);
        }
        if (phase == "drifting" || phase == "fading") smoke.Advance(.6f);
        camera.Render(); RenderTexture.active = rt; texture.ReadPixels(new Rect(0,0,900,720),0,0); texture.Apply();
        System.IO.File.WriteAllBytes("Artifacts/TempleGateMerchant/smoke-" + phase + ".png", texture.EncodeToPNG());
    }
    return "Saved before, handoff, drifting and fading previews";
}
finally
{
    source.sprite = original; RenderTexture.active = previous;
    if (smoke != null) UnityEngine.Object.DestroyImmediate(smoke.gameObject);
    UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(texture);
    if (opened) EditorSceneManager.CloseScene(scene, true);
}
