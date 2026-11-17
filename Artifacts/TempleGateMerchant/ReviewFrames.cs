using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
var scene=SceneManager.GetSceneByPath(EasternFantasy.Editor.TempleGateMerchantSetup.ScenePath);
bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(EasternFantasy.Editor.TempleGateMerchantSetup.ScenePath,OpenSceneMode.Additive);
var npc=scene.GetRootGameObjects().Single(g=>g.name=="MonkeyMerchant");
var source=npc.GetComponent<SpriteRenderer>();var original=source.sprite;int layer=npc.layer;
var cameraObject=new GameObject("Frame review camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.9f;
camera.transform.position=npc.transform.position+new Vector3(0,1.45f,-20);camera.cullingMask=1<<31;npc.layer=31;
camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.09f,.1f);
var rt=new RenderTexture(400,450,24);camera.targetTexture=rt;var previous=RenderTexture.active;
var tile=new Texture2D(400,450,TextureFormat.RGBA32,false);var sheet=new Texture2D(1600,1800,TextureFormat.RGBA32,false);
var rows=new System.Collections.Generic.List<object>();
try{
int index=0;
foreach(var name in new[]{"원숭이 상인 대기.png","원숭이 상인 곰방대.png"})
foreach(var frame in AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Old Temple/NPC/"+name).OfType<Sprite>().OrderBy(s=>s.name)){
source.sprite=frame;camera.Render();RenderTexture.active=rt;tile.ReadPixels(new Rect(0,0,400,450),0,0);tile.Apply();
sheet.SetPixels(index%4*400,(3-index/4)*450,400,450,tile.GetPixels());
rows.Add(new{name,frame=frame.name,rect=frame.rect.ToString(),pivot=frame.pivot.ToString(),vertices=frame.vertices.Length});index++;
}
sheet.Apply();System.IO.File.WriteAllBytes("Artifacts/TempleGateMerchant/repaired-frames.png",sheet.EncodeToPNG());
return Newtonsoft.Json.JsonConvert.SerializeObject(rows);
}finally{source.sprite=original;npc.layer=layer;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tile);UnityEngine.Object.DestroyImmediate(sheet);if(opened)EditorSceneManager.CloseScene(scene,true);}
