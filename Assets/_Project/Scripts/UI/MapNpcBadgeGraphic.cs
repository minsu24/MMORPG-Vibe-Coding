using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapNpcBadgeGraphic : MaskableGraphic
    {
        public bool namePlate;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r=rectTransform.rect;
            if(namePlate)
            {
                Quad(vh,r,new Color(.025f,.035f,.04f,.96f));
                Quad(vh,new Rect(r.xMin,r.yMax-1,r.width,1),new Color(.66f,.49f,.25f,.9f));
                return;
            }
            float radius=Mathf.Min(r.width,r.height)*.5f;
            Disc(vh,r.center+new Vector2(0,-3),radius+4,new Color(0,0,0,.6f));
            Disc(vh,r.center,radius,new Color(.025f,.035f,.04f,.98f));
            Ring(vh,r.center,radius-1,2,new Color(.85f,.67f,.36f,1));
            Ring(vh,r.center,radius-5,1,new Color(.44f,.33f,.18f,1));
        }
        private static void Disc(VertexHelper vh,Vector2 center,float radius,Color color)
        {
            int start=vh.currentVertCount; vh.AddVert(center,color,Vector2.zero);
            const int segments=64;
            for(int i=0;i<=segments;i++)
            {
                float angle=i*Mathf.PI*2/segments;
                vh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,color,Vector2.zero);
                if(i>0)vh.AddTriangle(start,start+i,start+i+1);
            }
        }
        private static void Ring(VertexHelper vh,Vector2 center,float radius,float thickness,Color color)
        {
            const int segments=64;int start=vh.currentVertCount;
            for(int i=0;i<=segments;i++)
            {
                float angle=i*Mathf.PI*2/segments;var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                vh.AddVert(center+direction*radius,color,Vector2.zero);
                vh.AddVert(center+direction*(radius-thickness),color,Vector2.zero);
                if(i>0){int n=start+i*2;vh.AddTriangle(n-2,n-1,n);vh.AddTriangle(n,n-1,n+1);}
            }
        }
        private static void Quad(VertexHelper vh,Rect r,Color color)
        {
            int n=vh.currentVertCount;
            vh.AddVert(new Vector2(r.xMin,r.yMin),color,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMax),color,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),color,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),color,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
}
