using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

if (EditorApplication.isPlaying) throw new Exception("Render the saved NPC outside Play Mode.");
var npc = GameObject.Find("GoblinBlacksmith");
var renderer = npc.GetComponent<SpriteRenderer>();
var original = renderer.sprite;
var cameraObject = new GameObject("Temporary blacksmith closeup camera");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, npc.scene);
var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 2.8f;
camera.transform.position = npc.transform.position + new Vector3(0,1.4f,-20);
camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
var rt = new RenderTexture(900,720,24); camera.targetTexture = rt;
var image = new Texture2D(900,720,TextureFormat.RGBA32,false);
var previous = RenderTexture.active;
try
{
    foreach (var phase in new[] { "Idle", "Hammer" })
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(EasternFantasy.Editor.TempleGateBlacksmithSetup.AnimationFolder + "/Blacksmith" + phase + ".anim");
        clip.SampleAnimation(npc, phase == "Idle" ? 0f : .43f);
        camera.Render(); RenderTexture.active = rt;
        image.ReadPixels(new Rect(0,0,900,720),0,0); image.Apply();
        System.IO.File.WriteAllBytes("Artifacts/TempleGateBlacksmith/" + phase.ToLowerInvariant() + ".png", image.EncodeToPNG());
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
}
