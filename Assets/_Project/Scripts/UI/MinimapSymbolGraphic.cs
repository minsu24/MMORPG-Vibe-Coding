using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MinimapSymbolGraphic : MaskableGraphic
    {
        public enum Kind { Player, Portal, Shop, Blacksmith }
        private Kind kind;
        public void Configure(Kind value, Color tint) { kind=value; color=tint; raycastTarget=false; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float r = Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.46f;
            var dark = new Color(.055f,.08f,.075f,.95f);
            Disc(mesh,r,dark);
            Ring(mesh,r,r-1.2f,color);
            if(kind==Kind.Player)
            {
                Triangle(mesh,new Vector2(0,r*.7f),new Vector2(-r*.44f,-r*.45f),new Vector2(r*.44f,-r*.45f),new Color(.9f,1,.95f));
                return;
            }
            if(kind==Kind.Portal)
            {
                Line(mesh,new Vector2(-r*.55f,r*.35f),new Vector2(r*.55f,r*.35f),1.6f,color);
                Line(mesh,new Vector2(-r*.38f,r*.2f),new Vector2(-r*.38f,-r*.5f),1.4f,color);
                Line(mesh,new Vector2(r*.38f,r*.2f),new Vector2(r*.38f,-r*.5f),1.4f,color);
            }
            else if(kind==Kind.Shop)
            {
                Line(mesh,new Vector2(-r*.6f,0),new Vector2(0,r*.5f),1.4f,color);
                Line(mesh,new Vector2(0,r*.5f),new Vector2(r*.6f,0),1.4f,color);
                Line(mesh,new Vector2(-r*.4f,-r*.05f),new Vector2(-r*.4f,-r*.5f),1.4f,color);
                Line(mesh,new Vector2(r*.4f,-r*.05f),new Vector2(r*.4f,-r*.5f),1.4f,color);
                Line(mesh,new Vector2(-r*.5f,-r*.5f),new Vector2(r*.5f,-r*.5f),1.4f,color);
            }
            else
            {
                Line(mesh,new Vector2(-r*.35f,-r*.5f),new Vector2(r*.2f,r*.35f),1.7f,color);
                Line(mesh,new Vector2(-r*.15f,r*.6f),new Vector2(r*.5f,r*.15f),2.6f,color);
            }
        }
        private static void Disc(VertexHelper mesh,float radius,Color tint)
        {
            for(int i=0;i<28;i++) Triangle(mesh,Vector2.zero,Point(i,radius),Point(i+1,radius),tint);
        }
        private static void Ring(VertexHelper mesh,float outer,float inner,Color tint)
        {
            for(int i=0;i<28;i++)
            {
                int start=mesh.currentVertCount;
                mesh.AddVert(Point(i,inner),tint,Vector2.zero); mesh.AddVert(Point(i,outer),tint,Vector2.zero);
                mesh.AddVert(Point(i+1,outer),tint,Vector2.zero); mesh.AddVert(Point(i+1,inner),tint,Vector2.zero);
                mesh.AddTriangle(start,start+1,start+2); mesh.AddTriangle(start,start+2,start+3);
            }
        }
        private static Vector2 Point(int i,float r) => new Vector2(Mathf.Cos(i*Mathf.PI/14),Mathf.Sin(i*Mathf.PI/14))*r;
        private static void Triangle(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Color tint)
        {
            int start=mesh.currentVertCount; mesh.AddVert(a,tint,Vector2.zero); mesh.AddVert(b,tint,Vector2.zero); mesh.AddVert(c,tint,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2);
        }
        private static void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color tint)
        {
            Vector2 normal=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
            int start=mesh.currentVertCount;
            mesh.AddVert(a-normal,tint,Vector2.zero); mesh.AddVert(a+normal,tint,Vector2.zero);
            mesh.AddVert(b+normal,tint,Vector2.zero); mesh.AddVert(b-normal,tint,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2); mesh.AddTriangle(start,start+2,start+3);
        }
    }
}
