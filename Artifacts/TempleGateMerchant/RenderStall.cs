using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
if (Application.isPlaying) throw new Exception("Edit mode required");
var scene=SceneManager.GetSceneByPath(EasternFantasy.Editor.TempleGateMerchantSetup.ScenePath);
bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(EasternFantasy.Editor.TempleGateMerchantSetup.ScenePath,OpenSceneMode.Additive);
var npc=scene.GetRootGameObjects().Single(g=>g.name=="MonkeyMerchant");
var source=npc.GetComponent<SpriteRenderer>();var original=source.sprite;
var stall=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true)).Single(r=>r.sprite!=null && AssetDatabase.GetAssetPath(r.sprite)==EasternFantasy.Editor.TempleGateMerchantStallSetup.ArtPath);
var corrected=stall.sprite;
var before=AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Old Temple/NPC/원숭이 상인 노점 수정.png").OfType<Sprite>().OrderByDescending(s=>s.rect.width*s.rect.height).First();
var cameraObject=new GameObject("Temporary stall comparison camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=2.3f;
camera.transform.position=npc.transform.position+new Vector3(0,1.15f,-20);
camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
var rt=new RenderTexture(1100,750,24);camera.targetTexture=rt;var previous=RenderTexture.active;
var texture=new Texture2D(1100,750,TextureFormat.RGBA32,false);
try{
AssetDatabase.LoadAssetAtPath<AnimationClip>(EasternFantasy.Editor.TempleGateMerchantSetup.AnimationFolder+"/MonkeyMerchantIdle.anim").SampleAnimation(npc,0f);
foreach(var phase in new[]{"before","after"}){
stall.sprite=phase=="before"?before:corrected;
camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1100,750),0,0);texture.Apply();
System.IO.File.WriteAllBytes("Artifacts/TempleGateMerchant/stall-"+phase+".png",texture.EncodeToPNG());
}
return new{stall=stall.name,sprite=AssetDatabase.GetAssetPath(corrected),position=stall.transform.position.ToString(),width=stall.bounds.size.x,renderOrder=stall.sortingOrder};
}finally{source.sprite=original;stall.sprite=corrected;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);if(opened)EditorSceneManager.CloseScene(scene,true);}
