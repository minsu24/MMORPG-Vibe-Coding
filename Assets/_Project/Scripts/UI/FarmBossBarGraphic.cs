using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    // Vector UI stays sharp at every CanvasScaler resolution; no texture dependencies.
    [AddComponentMenu("")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FarmBossBarGraphic : MaskableGraphic
    {
        public bool gaugeFill;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            if (gaugeFill)
            {
                Quad(vh, r.xMin, r.yMin, r.width, r.height,
                    new Color(.34f, .065f, .035f), new Color(.76f, .22f, .10f));
                Quad(vh, r.xMin, r.yMax - 2, r.width, 1, new Color(1f, .68f, .30f, .65f));
                return;
            }

            float half = r.width * .5f;
            Color dark = new Color(.10f, .065f, .035f, .97f);
            Color wood = new Color(.25f, .15f, .075f, .98f);
            Color brass = new Color(.65f, .45f, .21f);
            Color straw = new Color(.83f, .66f, .34f);
            // Beveled wooden trough, dark recessed gauge and a raised name plaque.
            Quad(vh, -half + 24, -39, r.width - 48, 60, new Color(0, 0, 0, .4f));
            Quad(vh, -half + 20, -34, r.width - 40, 55, dark, wood);
            Quad(vh, -half + 23, 18, r.width - 46, 2, brass);
            Quad(vh, -half + 23, -34, r.width - 46, 2, brass * .75f);
            for (int i = 0; i < 8; i++)
            {
                float x = -half + 45 + i * (r.width - 110) / 8;
                Quad(vh, x, -29 + i % 3 * 4, 35 + i % 4 * 11, 1, new Color(.58f, .36f, .16f, .22f));
            }
            Quad(vh, -half + 58, -24, r.width - 116, 27, brass);
            Quad(vh, -half + 60, -22, r.width - 120, 23, new Color(.065f, .035f, .025f));
            Quad(vh, -146, 8, 292, 35, brass);
            Quad(vh, -144, 10, 288, 31, dark, wood);
            // Small brass diamonds connect the plaque to the grain ornaments.
            Diamond(vh, new Vector2(-159, 24), 5, 5, straw);
            Diamond(vh, new Vector2(159, 24), 5, 5, straw);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (half - 40);
                Quad(vh, x - 8, -33, 16, 52, new Color(.15f, .10f, .05f));
                for (int i = 0; i < 8; i++)
                    Line(vh, new Vector2(x - 9, -29 + i * 6), new Vector2(x + 9, -25 + i * 6), 3, straw * .8f);
                Diamond(vh, new Vector2(x, -8), 4, 4, brass);
                float stem = side * (half - 19);
                Line(vh, new Vector2(stem, -18), new Vector2(stem - side * 11, 31), 2, brass);
                for (int i = 0; i < 5; i++)
                {
                    float y = 2 + i * 6;
                    float sx = stem - side * (y + 18) * .22f;
                    Diamond(vh, new Vector2(sx - 5, y), 5, 3, straw);
                    Diamond(vh, new Vector2(sx + 5, y + 3), 5, 3, brass);
                }
                Diamond(vh, new Vector2(stem - side * 11, 34), 3, 5, straw);
            }
        }

        private static void Quad(VertexHelper vh, float x, float y, float w, float h, Color c)
            => Quad(vh, x, y, w, h, c, c);

        private static void Quad(VertexHelper vh, float x, float y, float w, float h, Color bottom, Color top)
        {
            int n = vh.currentVertCount;
            vh.AddVert(new Vector3(x, y), bottom, Vector2.zero);
            vh.AddVert(new Vector3(x, y + h), top, Vector2.zero);
            vh.AddVert(new Vector3(x + w, y + h), top, Vector2.zero);
            vh.AddVert(new Vector3(x + w, y), bottom, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2);
            vh.AddTriangle(n + 2, n + 3, n);
        }

        private static void Diamond(VertexHelper vh, Vector2 p, float w, float h, Color c)
        {
            int n = vh.currentVertCount;
            vh.AddVert(p + Vector2.left * w, c, Vector2.zero);
            vh.AddVert(p + Vector2.up * h, c, Vector2.zero);
            vh.AddVert(p + Vector2.right * w, c, Vector2.zero);
            vh.AddVert(p + Vector2.down * h, c, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2);
            vh.AddTriangle(n + 2, n + 3, n);
        }

        private static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color c)
        {
            Vector2 d = (b - a).normalized;
            Vector2 normal = new Vector2(-d.y, d.x) * width * .5f;
            int n = vh.currentVertCount;
            vh.AddVert(a - normal, c, Vector2.zero);
            vh.AddVert(a + normal, c, Vector2.zero);
            vh.AddVert(b + normal, c, Vector2.zero);
            vh.AddVert(b - normal, c, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2);
            vh.AddTriangle(n + 2, n + 3, n);
        }
    }
}
