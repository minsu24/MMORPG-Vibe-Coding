using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
var path="Assets/_Project/Art/Old Temple/NPC/곰방대 연기.png";
var raw=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(raw,System.IO.File.ReadAllBytes(path));
try{
var pixels=raw.GetPixels32();
return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).Select(s=>{
double sum=0,xSum=0,ySum=0;
for(int y=(int)s.rect.yMin;y<s.rect.yMax;y++)for(int x=(int)s.rect.xMin;x<s.rect.xMax;x++){
var p=pixels[y*raw.width+x];double weight=p.a/255.0;
sum+=weight;xSum+=(x+.5-s.rect.xMin)*weight;ySum+=(y+.5-s.rect.yMin)*weight;}
var center=new Vector2((float)(xSum/sum),(float)(ySum/sum));
return new{s.name,rect=s.rect.ToString(),pivot=s.pivot.ToString(),alphaCenter=center.ToString(),visibleOffset=((center-s.pivot)/s.pixelsPerUnit).ToString()};
}).ToArray();
}finally{UnityEngine.Object.DestroyImmediate(raw);}
