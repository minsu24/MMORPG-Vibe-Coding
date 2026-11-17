using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using UnityEngine.SceneManagement;

if (EditorApplication.isPlaying) throw new Exception("Render the saved NPC outside Play Mode.");
var scenePath = EasternFantasy.Editor.TempleGateMerchantSetup.ScenePath;
var scene = SceneManager.GetSceneByPath(scenePath);
bool opened = !scene.isLoaded;
if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
var npc = scene.GetRootGameObjects().Single(g => g.name == "MonkeyMerchant");
var renderer = npc.GetComponent<SpriteRenderer>();
var original = renderer.sprite;
var cameraObject = new GameObject("Temporary merchant closeup camera");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, npc.scene);
var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 2.8f;
camera.transform.position = npc.transform.position + new Vector3(0,1.4f,-20);
camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
var rt = new RenderTexture(900,720,24); camera.targetTexture = rt;
var image = new Texture2D(900,720,TextureFormat.RGBA32,false);
var previous = RenderTexture.active;
try
{
    System.IO.Directory.CreateDirectory("Artifacts/TempleGateMerchant");
    foreach (var phase in new[] { "Idle", "Pipe" })
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(EasternFantasy.Editor.TempleGateMerchantSetup.AnimationFolder + "/MonkeyMerchant" + phase + ".anim");
        clip.SampleAnimation(npc, phase == "Idle" ? 0f : 2.1f);
        camera.Render(); RenderTexture.active = rt;
        image.ReadPixels(new Rect(0,0,900,720),0,0); image.Apply();
        System.IO.File.WriteAllBytes("Artifacts/TempleGateMerchant/" + phase.ToLowerInvariant() + ".png", image.EncodeToPNG());
    }
    Selection.activeGameObject = npc;
    return new { npc.name, position = npc.transform.position.ToString(), spriteFrames = 16, interact = "T" };
}
finally
{
    renderer.sprite = original;
    RenderTexture.active = previous;
    UnityEngine.Object.DestroyImmediate(cameraObject);
    UnityEngine.Object.DestroyImmediate(rt);
    UnityEngine.Object.DestroyImmediate(image);
    if (opened) EditorSceneManager.CloseScene(scene, true);
}
