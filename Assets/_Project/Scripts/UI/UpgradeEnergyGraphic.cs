using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    /// <summary>Texture-free UI energy, driven by the upgrade's unscaled timeline.</summary>
    public sealed class UpgradeEnergyGraphic : MaskableGraphic
    {
        private float elapsed, impactTime, endTime;
        private static readonly Color Gold = new Color(1f, .73f, .25f);
        private static readonly Color PaleGold = new Color(1f, .94f, .7f);

        public void DrawAt(float seconds, float impact, float end)
        {
            elapsed = seconds;
            impactTime = Mathf.Max(.01f, impact);
            endTime = Mathf.Max(impactTime + .01f, end);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (impactTime <= 0f || elapsed >= endTime) return;
            if (elapsed < impactTime) DrawGather(mesh);
            else DrawBurst(mesh);
        }

        private void DrawGather(VertexHelper mesh)
        {
            float progress = Mathf.Clamp01(elapsed / impactTime);
            float fade = Mathf.Clamp01(progress * 6f);
            float charge = progress * progress;
            Disc(mesh, Vector2.zero, Mathf.Lerp(80f, 48f, charge), Gold, (.06f + charge * .35f) * fade);
            Ring(mesh, Mathf.Lerp(138f, 29f, charge), 2f + charge * 2f, Gold, fade * (.2f + charge * .5f));
            Ring(mesh, Mathf.Lerp(106f, 24f, charge), 1.3f, PaleGold, fade * .3f);
            for (int i = 0; i < 28; i++)
            {
                float travel = Mathf.Repeat(progress * (1.4f + (i % 3) * .15f) + i * .618034f, 1f);
                float radius = Mathf.Lerp(150f, 8f, travel * travel);
                float angle = i * 2.39996f + elapsed * 1.4f + travel * 1.5f;
                Vector2 point = Polar(angle, radius);
                float alpha = Mathf.Sin(travel * Mathf.PI) * fade;
                Disc(mesh, point, 6f + charge * 3f, Gold, alpha * .6f);
                Disc(mesh, point, 2f + charge, PaleGold, alpha);
                Vector2 previous = Polar(angle - .1f, radius + 12f);
                Streak(mesh, previous, point, 1.3f, Gold, alpha * .65f);
            }
            Disc(mesh, Vector2.zero, 18f + charge * 16f, PaleGold, charge * .8f);
            Disc(mesh, Vector2.zero, 5f + charge * 12f, Color.white, charge);
        }

        private void DrawBurst(VertexHelper mesh)
        {
            float seconds = elapsed - impactTime;
            float progress = Mathf.Clamp01(seconds / (endTime - impactTime));
            float expansion = 1f - Mathf.Pow(1f - progress, 3f);
            float fade = Mathf.Pow(1f - progress, 2f);
            Disc(mesh, Vector2.zero, 95f + expansion * 85f, Gold, fade * .45f);
            Disc(mesh, Vector2.zero, 30f + expansion * 65f, Color.white, Mathf.Clamp01(1f - seconds / .36f) * .95f);
            Ring(mesh, 30f + expansion * 190f, 3f + fade * 6f, PaleGold, fade * .9f);
            Ring(mesh, 22f + expansion * 155f, 2f, Gold, fade * .65f);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6f + .15f;
                Vector2 from = Polar(angle, 18f + expansion * 25f);
                Vector2 to = Polar(angle, 65f + expansion * (i % 2 == 0 ? 145f : 100f));
                Streak(mesh, from, to, fade * (i % 2 == 0 ? 6f : 3f), PaleGold, fade * .65f);
            }
            for (int i = 0; i < 36; i++)
            {
                float angle = i * 2.39996f;
                float radius = 18f + expansion * (105f + i % 5 * 23f);
                Vector2 point = Polar(angle, radius) + Vector2.down * (seconds * seconds * 15f);
                Disc(mesh, point, 5f, Gold, fade * .8f);
                Disc(mesh, point, 1.8f, PaleGold, fade);
                Streak(mesh, point - Polar(angle, 7f + fade * 9f), point, 1f, Gold, fade * .8f);
            }
        }

        private static Vector2 Polar(float radians, float radius) => new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;

        private static void Disc(VertexHelper mesh, Vector2 center, float radius, Color tint, float alpha)
        {
            const int segments = 32;
            int start = mesh.currentVertCount;
            tint.a = alpha;
            mesh.AddVert(center, tint, Vector2.zero);
            tint.a = 0f;
            for (int i = 0; i <= segments; i++) mesh.AddVert(center + Polar(i * Mathf.PI * 2f / segments, radius), tint, Vector2.zero);
            for (int i = 0; i < segments; i++) mesh.AddTriangle(start, start + i + 1, start + i + 2);
        }

        private static void Ring(VertexHelper mesh, float radius, float width, Color tint, float alpha)
        {
            const int segments = 80;
            int start = mesh.currentVertCount;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                tint.a = 0f; mesh.AddVert(Polar(angle, radius - width), tint, Vector2.zero);
                tint.a = alpha; mesh.AddVert(Polar(angle, radius), tint, Vector2.zero);
                tint.a = 0f; mesh.AddVert(Polar(angle, radius + width), tint, Vector2.zero);
                if (i == 0) continue;
                int index = start + i * 3;
                mesh.AddTriangle(index - 3, index - 2, index); mesh.AddTriangle(index, index - 2, index + 1);
                mesh.AddTriangle(index - 2, index - 1, index + 1); mesh.AddTriangle(index + 1, index - 1, index + 2);
            }
        }

        private static void Streak(VertexHelper mesh, Vector2 from, Vector2 to, float width, Color tint, float alpha)
        {
            Vector2 axis = (to - from).normalized;
            Vector2 normal = new Vector2(-axis.y, axis.x) * width;
            int start = mesh.currentVertCount;
            tint.a = 0f; mesh.AddVert(from - normal, tint, Vector2.zero); mesh.AddVert(from + normal, tint, Vector2.zero);
            tint.a = alpha; mesh.AddVert(to, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
        }
    }
}
