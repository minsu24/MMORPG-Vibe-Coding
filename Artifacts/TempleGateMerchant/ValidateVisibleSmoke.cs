using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using EasternFantasy.Shop;
if(Application.isPlaying)throw new Exception("Use edit mode for deterministic rendered smoke validation");
var path="Assets/_Project/Art/Old Temple/NPC/곰방대 연기.png";
var frames=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
var raw=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(raw,System.IO.File.ReadAllBytes(path));
var pixels=raw.GetPixels32();
foreach(var frame in frames){
double mass=0,cx=0,cy=0;var rect=frame.rect;
for(int y=(int)rect.yMin;y<rect.yMax;y++)for(int x=(int)rect.xMin;x<rect.xMax;x++){
double a=pixels[y*raw.width+x].a/255.0;mass+=a;cx+=(x+.5-rect.xMin)*a;cy+=(y+.5-rect.yMin)*a;}
if(Vector2.Distance(new Vector2((float)(cx/mass),(float)(cy/mass)),frame.pivot)>.01f)throw new Exception("Visible smoke pivot mismatch: "+frame.name);
}
var sourceObject=new GameObject("Smoke validation source");var source=sourceObject.AddComponent<SpriteRenderer>();
source.sharedMaterial=new Material(Shader.Find("Sprites/Default"));source.sprite=frames[0];source.enabled=false;
var smoke=MerchantPipeSmoke.Create(source,frames,Vector3.zero,new Vector2(1.4f,1.05f),new Vector3(.45f,.65f,0),1.6f);
foreach(var r in smoke.GetComponentsInChildren<SpriteRenderer>())r.gameObject.layer=31;
var cameraObject=new GameObject("Visible smoke validation camera");var camera=cameraObject.AddComponent<Camera>();
camera.orthographic=true;camera.orthographicSize=1.6f;camera.transform.position=new Vector3(.36f,.52f,-10);
camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<31;
var rt=new RenderTexture(192,192,24);camera.targetTexture=rt;var previous=RenderTexture.active;
var capture=new Texture2D(192,192,TextureFormat.RGBA32,false);var strip=new Texture2D(1536,192,TextureFormat.RGBA32,false);
var points=new System.Collections.Generic.List<Vector2>();float minDx=float.PositiveInfinity,minDy=float.PositiveInfinity;
try{
for(int step=0;step<=75;step++){
if(step>0)smoke.Advance(.02f);
camera.Render();RenderTexture.active=rt;capture.ReadPixels(new Rect(0,0,192,192),0,0);capture.Apply();var rendered=capture.GetPixels32();
double mass=0,cx=0,cy=0;
for(int i=0;i<rendered.Length;i++){double a=rendered[i].a/255.0;mass+=a;cx+=(i%192+.5)*a;cy+=(i/192+.5)*a;}
if(mass<=0)throw new Exception("Rendered smoke disappeared too early");
var point=new Vector2((float)(cx/mass/192-.5)*3.2f+camera.transform.position.x,(float)(cy/mass/192-.5)*3.2f+camera.transform.position.y);
if(points.Count>0){var delta=point-points[points.Count-1];minDx=Mathf.Min(minDx,delta.x);minDy=Mathf.Min(minDy,delta.y);}
points.Add(point);
if(step%10==0)strip.SetPixels(step/10*192,0,192,192,capture.GetPixels());
}
strip.Apply();System.IO.File.WriteAllBytes("Artifacts/TempleGateMerchant/smoke-visible-trajectory.png",strip.EncodeToPNG());
if(minDx<-.002f||minDy<-.002f)throw new Exception("Rendered cloud still reverses: min dx="+minDx+", dy="+minDy);
return new{passed=true,samples=points.Count,minVisibleStepX=minDx,minVisibleStepY=minDy,from=points.First().ToString(),to=points.Last().ToString(),allFramePivotsMatchVisibleCenter=true};
}finally{RenderTexture.active=previous;if(smoke!=null)UnityEngine.Object.DestroyImmediate(smoke.gameObject);UnityEngine.Object.DestroyImmediate(source.sharedMaterial);UnityEngine.Object.DestroyImmediate(sourceObject);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(capture);UnityEngine.Object.DestroyImmediate(strip);UnityEngine.Object.DestroyImmediate(raw);}
