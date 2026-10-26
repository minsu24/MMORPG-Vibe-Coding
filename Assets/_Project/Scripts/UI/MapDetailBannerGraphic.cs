using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapDetailBannerGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r=rectTransform.rect;
            Plate(mesh,r,new Color(.60f,.49f,.29f,1));
            float border=Mathf.Min(1.4f,r.width*.04f);
            Plate(mesh,new Rect(r.x+border,r.y+border*2,r.width-border*2,r.height-border*3),new Color(.025f,.032f,.034f,1));
        }
        private static void Plate(VertexHelper mesh,Rect r,Color tint)
        {
            float tip=Mathf.Min(r.width*.60f,r.height*.24f);
            var points=new[]{new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin+tip),new Vector2(r.center.x,r.yMin),new Vector2(r.xMin,r.yMin+tip)};
            int start=mesh.currentVertCount;
            mesh.AddVert(r.center,tint,Vector2.zero);
            foreach(var point in points)mesh.AddVert(point,tint,Vector2.zero);
            for(int i=0;i<points.Length;i++)mesh.AddTriangle(start,start+i+1,start+(i+1)%points.Length+1);
        }
    }
}
