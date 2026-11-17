using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
var go=new GameObject("Transition reference merchant");var visual=go.AddComponent<SpriteRenderer>();go.layer=31;
visual.sharedMaterial=new Material(Shader.Find("Sprites/Default"));
var camGo=new GameObject("Transition reference camera");var cam=camGo.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=1.65f;
cam.transform.position=new Vector3(0,1.3f,-10);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;cam.cullingMask=1<<31;
var rt=new RenderTexture(512,640,24);cam.targetTexture=rt;var image=new Texture2D(512,640,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
try{
foreach(var phase in new[]{"idle","pipe"}){
var path="Assets/_Project/Art/Old Temple/NPC/"+(phase=="idle"?"원숭이 상인 대기.png":"원숭이 상인 곰방대.png");
visual.sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).First();
cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,512,640),0,0);image.Apply();
System.IO.File.WriteAllBytes("Artifacts/TempleGateMerchant/transition-reference-"+phase+".png",image.EncodeToPNG());
}
return "Saved exact idle and pipe starting pose references on equal canvases";
}finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(visual.sharedMaterial);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(camGo);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
