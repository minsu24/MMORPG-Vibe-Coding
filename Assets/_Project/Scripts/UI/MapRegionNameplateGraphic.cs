using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapRegionNameplateGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            Color gold = new Color(.66f, .49f, .24f, .95f);
            Plate(vh, rect, 10, new Color(.12f, .075f, .035f, .2f), new Vector2(1, -3));
            Plate(vh, rect, 10, gold, Vector2.zero);
            Rect inner = new Rect(rect.x + 1.2f, rect.y + 1.2f, rect.width - 2.4f, rect.height - 2.4f);
            Plate(vh, inner, 9, new Color(.075f, .09f, .085f, .94f), Vector2.zero);
            Rule(vh, new Rect(rect.center.x - rect.width * .27f, rect.yMax - 5, rect.width * .54f, .7f),
                new Color(.88f, .72f, .4f, .38f));
            Rule(vh, new Rect(rect.center.x - rect.width * .27f, rect.yMin + 4.3f, rect.width * .54f, .7f),
                new Color(.88f, .72f, .4f, .3f));
            Diamond(vh, new Vector2(rect.xMin + 14, rect.center.y), gold);
            Diamond(vh, new Vector2(rect.xMax - 14, rect.center.y), gold);
        }
        private static void Plate(VertexHelper vh, Rect r, float cut, Color tint, Vector2 offset)
        {
            Vector2[] p = { new Vector2(r.xMin + cut, r.yMin), new Vector2(r.xMax - cut, r.yMin),
                new Vector2(r.xMax, r.yMin + cut), new Vector2(r.xMax, r.yMax - cut),
                new Vector2(r.xMax - cut, r.yMax), new Vector2(r.xMin + cut, r.yMax),
                new Vector2(r.xMin, r.yMax - cut), new Vector2(r.xMin, r.yMin + cut) };
            int start = vh.currentVertCount;
            vh.AddVert(r.center + offset, tint, Vector2.zero);
            foreach (Vector2 point in p) vh.AddVert(point + offset, tint, Vector2.zero);
            for (int i = 0; i < p.Length; i++) vh.AddTriangle(start, start + i + 1, start + (i + 1) % p.Length + 1);
        }
        private static void Rule(VertexHelper vh, Rect r, Color color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector2(r.xMin, r.yMin), color, Vector2.zero);
            vh.AddVert(new Vector2(r.xMin, r.yMax), color, Vector2.zero);
            vh.AddVert(new Vector2(r.xMax, r.yMax), color, Vector2.zero);
            vh.AddVert(new Vector2(r.xMax, r.yMin), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
        private static void Diamond(VertexHelper vh, Vector2 center, Color color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(center + Vector2.up * 4, color, Vector2.zero);
            vh.AddVert(center + Vector2.right * 2.5f, color, Vector2.zero);
            vh.AddVert(center + Vector2.down * 4, color, Vector2.zero);
            vh.AddVert(center + Vector2.left * 2.5f, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
