using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapNpcPortraitGraphic : MaskableGraphic
    {
        public MapNpcPortrait portrait;
        public override Texture mainTexture => portrait != null ? portrait.atlas : base.mainTexture;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (portrait == null || portrait.atlas == null || portrait.strips == null) return;
            Rect rect = rectTransform.rect, source = portrait.region;
            float scale = Mathf.Min(rect.width/source.width,rect.height/source.height);
            Vector2 center = rect.center;
            float width=portrait.atlas.width,height=portrait.atlas.height;
            foreach (var strip in portrait.strips)
            {
                int n=vh.currentVertCount;
                var a=new Vector2(strip.x,strip.y); var b=new Vector2(strip.x,strip.w);
                var c=new Vector2(strip.z,strip.w); var d=new Vector2(strip.z,strip.y);
                vh.AddVert(center+(a-source.center)*scale,color,new Vector2(a.x/width,a.y/height));
                vh.AddVert(center+(b-source.center)*scale,color,new Vector2(b.x/width,b.y/height));
                vh.AddVert(center+(c-source.center)*scale,color,new Vector2(c.x/width,c.y/height));
                vh.AddVert(center+(d-source.center)*scale,color,new Vector2(d.x/width,d.y/height));
                vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
