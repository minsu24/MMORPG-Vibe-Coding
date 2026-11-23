using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class MapPageCloudGraphic : MaskableGraphic
    {
        private const int Columns = 16;
        private const float CoreRadius = .98f;
        private const float FeatherRadius = 1.36f;
        private static readonly float[] CloudRows = { -FeatherRadius, -CoreRadius, CoreRadius, FeatherRadius };
        private static readonly Color FallbackTint = new Color(.09f, .12f, .19f);
        private Texture2D clouds;
        private float progress;
        private float direction;

        public override Texture mainTexture => clouds != null ? clouds : Texture2D.whiteTexture;

        internal void SetTexture(Texture2D texture)
        {
            clouds = texture;
            SetMaterialDirty();
            SetVerticesDirty();
        }

        internal void SetFrame(float progress, bool goingUp)
        {
            this.progress = progress;
            direction = goingUp ? -1f : 1f;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            if (progress <= 0f || progress >= 1f || rect.width <= 0f || rect.height <= 0f) return;

            float center = .5f + (progress - .5f) * 3.9f * direction;
            Texture texture = mainTexture;
            float relativeAspect = (rect.width / rect.height) / (texture.width / (float)texture.height);
            Vector2 uvScale = new Vector2(.94f * Mathf.Min(1f, relativeAspect), .80f / Mathf.Max(1f, relativeAspect));
            float mistLead = Mathf.Sin(progress * Mathf.PI) * .23f * direction;
            AddLayer(mesh, rect, center + mistLead, uvScale, .32f, true);
            // The dense layer covers the viewport at the midpoint, when the map sprite changes.
            AddLayer(mesh, rect, center, uvScale, 1f, false);
        }

        private void AddLayer(VertexHelper mesh, Rect rect, float center, Vector2 uvScale, float opacity, bool mist)
        {
            int firstVertex = mesh.currentVertCount;
            int rows = CloudRows.Length;
            Color tint = clouds != null ? Color.white : FallbackTint;
            float drift = (progress - .5f) * (mist ? -.10f : .05f);
            float phase = mist ? 2.8f : .5f;
            for (int column = 0; column <= Columns; column++)
            {
                float x = column / (float)Columns;
                float columnCenter = center + Mathf.Sin(x * 15f + phase) * .045f + Mathf.Sin(x * 29f + phase) * .025f;
                float u = .5f + ((mist ? 1f - x : x) - .5f) * uvScale.x + drift;
                foreach (float row in CloudRows)
                {
                    float y = Mathf.Clamp01(columnCenter + row);
                    tint.a = opacity * Mathf.InverseLerp(FeatherRadius, CoreRadius, Mathf.Abs(y - columnCenter));
                    // Keep the clouds moving with the band as its edges pass outside the viewport.
                    Vector2 uv = new Vector2(u, .5f + (y - center) * uvScale.y);
                    mesh.AddVert(new Vector3(Mathf.Lerp(rect.xMin, rect.xMax, x), Mathf.Lerp(rect.yMin, rect.yMax, y)), tint, uv);
                }
            }

            for (int column = 0; column < Columns; column++)
            {
                for (int row = 0; row < rows - 1; row++)
                {
                    int lowerLeft = firstVertex + column * rows + row;
                    int lowerRight = lowerLeft + rows;
                    mesh.AddTriangle(lowerLeft, lowerLeft + 1, lowerRight + 1);
                    mesh.AddTriangle(lowerLeft, lowerRight + 1, lowerRight);
                }
            }
        }
    }
}
