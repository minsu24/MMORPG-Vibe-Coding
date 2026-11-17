using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
var result = new List<object>();
foreach (var name in new[] { "원숭이 상인 대기.png", "원숭이 상인 곰방대.png" })
{
    var raw = new Texture2D(2,2); ImageConversion.LoadImage(raw, System.IO.File.ReadAllBytes("Assets/_Project/Art/Old Temple/NPC/"+name));
    var p = raw.GetPixels32(); var seen = new bool[p.Length]; var blobs = new List<object>();
    for (int i=0;i<p.Length;i++)
    {
        if(seen[i] || p[i].a<32) continue;
        var q=new Queue<int>();q.Enqueue(i);seen[i]=true;int count=0,l=raw.width,r=0,b=raw.height,t=0;
        while(q.Count>0){int v=q.Dequeue(),x=v%raw.width,y=v/raw.width;count++;l=Math.Min(l,x);r=Math.Max(r,x);b=Math.Min(b,y);t=Math.Max(t,y);
        for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++){int nx=x+dx,ny=y+dy;if(nx<0||nx>=raw.width||ny<0||ny>=raw.height)continue;int n=ny*raw.width+nx;if(seen[n]||p[n].a<32)continue;seen[n]=true;q.Enqueue(n);}}
        if(count>100)blobs.Add(new{count,l,r,b,t});
    }
    result.Add(new{name,blobs});UnityEngine.Object.DestroyImmediate(raw);
}
return Newtonsoft.Json.JsonConvert.SerializeObject(result);
