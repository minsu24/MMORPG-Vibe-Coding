using System;
using UnityEngine;
var art="Assets/_Project/Art/Old Temple/NPC/";
var rows=new System.Collections.Generic.List<object>();
foreach(var name in new[]{"곰방대 연기.png","원숭이 상인 곰방대.png"})
{
    var raw=new Texture2D(2,2,TextureFormat.RGBA32,false);
    try {
        ImageConversion.LoadImage(raw,System.IO.File.ReadAllBytes(art+name));
        var pixels=raw.GetPixels32();var groups=new System.Collections.Generic.List<object>();
        int start=-1;
        for(int x=0;x<=raw.width;x++) {
            bool occupied=false;
            if(x<raw.width)for(int y=0;y<raw.height;y++)if(pixels[y*raw.width+x].a>=32){occupied=true;break;}
            if(occupied&&start<0)start=x;
            if(!occupied&&start>=0){if(x-start>3)groups.Add(new{left=start,right=x-1});start=-1;}
        }
        int left=raw.width,right=0,bottom=raw.height,top=0;
        if(name.StartsWith("원숭이"))for(int y=raw.height/2;y<raw.height;y++)for(int x=Mathf.RoundToInt(raw.width*7/8f);x<raw.width;x++){
            var p=pixels[y*raw.width+x];
            if(p.a>=128&&p.r>180&&p.g>170&&p.b>150&&Mathf.Abs(p.r-p.b)<65){left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
        }
        rows.Add(new{name,width=raw.width,height=raw.height,groups,whiteCloud=new{left,right,bottom,top}});
    }finally{UnityEngine.Object.DestroyImmediate(raw);}
}
return Newtonsoft.Json.JsonConvert.SerializeObject(rows);
