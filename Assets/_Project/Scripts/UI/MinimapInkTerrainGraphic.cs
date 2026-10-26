using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MinimapInkTerrainGraphic : MaskableGraphic
    {
        private readonly List<Rect> surfaces = new List<Rect>();
        public void Configure(List<Bounds> terrain, Rect world, Vector2 size)
        {
            raycastTarget = false; surfaces.Clear();
            foreach (var bounds in terrain)
            {
                // Walls enclosing the level are not traversable paths.
                if (bounds.size.x < bounds.size.y * 1.5f) continue;
                Vector2 left = MinimapUI.WorldToMapPosition(new Vector2(bounds.min.x, bounds.max.y), world, size);
                float width = bounds.size.x / world.width * size.x;
                if (width >= 4) surfaces.Add(new Rect(left.x, left.y, width, 1));
            }
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            foreach (var path in surfaces)
            {
                int count = Mathf.Clamp(Mathf.CeilToInt(path.width / 3f), 2, 140);
                for (int i = 0; i < count; i++)
                {
                    float x0 = path.x + path.width * i / count;
                    float x1 = path.x + path.width * (i+1) / count;
                    float feather = .65f + .35f * Mathf.PerlinNoise(x0*.21f,path.y*.19f);
                    var ink = new Color(.12f,.27f,.23f,.76f);
                    var wash = new Color(.12f,.27f,.23f,0);
                    Quad(mesh,new Vector2(x0,path.y-4*feather),new Vector2(x1,path.y-4*feather),
                        new Vector2(x1,path.y),new Vector2(x0,path.y),wash,ink);
                    Quad(mesh,new Vector2(x0,path.y),new Vector2(x1,path.y),
                        new Vector2(x1,path.y+1.4f*feather),new Vector2(x0,path.y+1.4f*feather),ink,new Color(.38f,.48f,.32f,.9f));
                }
            }
        }
        private static void Quad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color bottom,Color top)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a,bottom,Vector2.zero); mesh.AddVert(b,bottom,Vector2.zero);
            mesh.AddVert(c,top,Vector2.zero); mesh.AddVert(d,top,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2); mesh.AddTriangle(start,start+2,start+3);
        }
    }
}
