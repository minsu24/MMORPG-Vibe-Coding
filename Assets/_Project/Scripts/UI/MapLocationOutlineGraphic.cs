using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    // UI geometry follows the printed location banner. No extra camera or texture is needed.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapLocationOutlineGraphic : MaskableGraphic
    {
        private Vector2[] points;
        private float[] lengths;
        private float perimeter;
        public void SetPath(Vector2[] path)
        {
            points = path;
            lengths = path == null ? null : new float[path.Length];
            perimeter = 0;
            if (path != null)
                for (int i = 0; i < path.Length; i++)
                {
                    perimeter += Vector2.Distance(path[i], path[(i + 1) % path.Length]);
                    lengths[i] = perimeter;
                }
            SetVerticesDirty();
        }
        private void Update() { if (Application.isPlaying) SetVerticesDirty(); }
        private Vector2 PointAt(float distance)
        {
            distance = Mathf.Repeat(distance, perimeter);
            for (int i = 0; i < points.Length; i++)
            {
                float start = i == 0 ? 0 : lengths[i - 1];
                if (distance <= lengths[i])
                    return Vector2.Lerp(points[i], points[(i + 1) % points.Length],
                        (distance - start) / Mathf.Max(.001f, lengths[i] - start));
            }
            return points[0];
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (points == null || points.Length < 3 || perimeter <= 0) return;
            Color gold = color;
            for (int i = 0; i < points.Length; i++)
                AddLine(vh, points[i], points[(i + 1) % points.Length], 1.5f,
                    new Color(gold.r, gold.g, gold.b, gold.a * .42f));
            float head = Mathf.Repeat(Time.unscaledTime / 3.2f, 1f) * perimeter;
            const int segments = 42;
            float trail = perimeter * .24f;
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments, b = (i + 1f) / segments;
                Vector2 from = PointAt(head - trail + trail * a);
                Vector2 to = PointAt(head - trail + trail * b);
                float alpha = Mathf.Lerp(.04f, 1f, b * b) * gold.a;
                AddLine(vh, from, to, 7f, new Color(gold.r, gold.g, gold.b, alpha * .22f));
                AddLine(vh, from, to, 3.2f, new Color(1f, Mathf.Lerp(gold.g, .96f, b * b), Mathf.Lerp(gold.b, .65f, b * b), alpha));
            }
        }
        private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
            int start = vh.currentVertCount;
            vh.AddVert(a - normal, tint, Vector2.zero);
            vh.AddVert(a + normal, tint, Vector2.zero);
            vh.AddVert(b + normal, tint, Vector2.zero);
            vh.AddVert(b - normal, tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
