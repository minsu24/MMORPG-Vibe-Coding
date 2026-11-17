using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
if(Application.isPlaying)throw new Exception("Edit mode required");
var scene=SceneManager.GetSceneByPath(EasternFantasy.Editor.TempleGateMerchantSetup.ScenePath);bool opened=!scene.isLoaded;
if(opened)scene=EditorSceneManager.OpenScene(EasternFantasy.Editor.TempleGateMerchantSetup.ScenePath,OpenSceneMode.Additive);
var npc=scene.GetRootGameObjects().Single(g=>g.name=="MonkeyMerchant");var visual=npc.GetComponent<SpriteRenderer>();var original=visual.sprite;int originalLayer=npc.layer;
var cameraObject=new GameObject("Temporary transition review camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.8f;camera.transform.position=npc.transform.position+new Vector3(0,1.35f,-20);
camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.09f,.1f);camera.cullingMask=1<<31;npc.layer=31;
var rt=new RenderTexture(384,480,24);camera.targetTexture=rt;var previous=RenderTexture.active;
var tile=new Texture2D(384,480,TextureFormat.RGBA32,false);var sheet=new Texture2D(1536,960,TextureFormat.RGBA32,false);
var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(EasternFantasy.Editor.TempleGateMerchantSetup.AnimationFolder+"/MonkeyMerchantEnterPipe.anim");
var rows=new System.Collections.Generic.List<object>();
try{
var keys=AnimationUtility.GetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"));
for(int i=0;i<keys.Length;i++){
clip.SampleAnimation(npc,keys[i].time+.001f);camera.Render();RenderTexture.active=rt;tile.ReadPixels(new Rect(0,0,384,480),0,0);tile.Apply();
sheet.SetPixels(i%4*384,(1-i/4)*480,384,480,tile.GetPixels());rows.Add(new{frame=i,path=AssetDatabase.GetAssetPath(visual.sprite),height=visual.bounds.size.y,pivot=visual.sprite.pivot.ToString()});
}
sheet.Apply();System.IO.File.WriteAllBytes("Artifacts/TempleGateMerchant/transition-review.png",sheet.EncodeToPNG());
return Newtonsoft.Json.JsonConvert.SerializeObject(rows);
}finally{visual.sprite=original;npc.layer=originalLayer;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tile);UnityEngine.Object.DestroyImmediate(sheet);if(opened)EditorSceneManager.CloseScene(scene,true);}
