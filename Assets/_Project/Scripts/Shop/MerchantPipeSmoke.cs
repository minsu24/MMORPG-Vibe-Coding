using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Shop
{
    public sealed class MerchantPipeSmoke : MonoBehaviour
    {
        private SpriteRenderer visual, blendVisual;
        private Sprite[] frames;
        private Vector3 origin, velocity;
        private Color initialColor;
        private float age, lifetime;
        public bool IsFinished { get; private set; }

        public static MerchantPipeSmoke Create(SpriteRenderer source, Sprite[] frames, Vector3 position,
            Vector2 size, Vector3 velocity, float lifetime, float delay = 0f, float opacity = 1f)
        {
            var go = new GameObject("Monkey Merchant Detached Pipe Smoke");
            SceneManager.MoveGameObjectToScene(go, source.gameObject.scene);
            go.transform.SetPositionAndRotation(position, source.transform.rotation);
            var smoke = go.AddComponent<MerchantPipeSmoke>();
            smoke.frames = frames;
            smoke.origin = position; smoke.velocity = velocity;
            smoke.age = -Mathf.Max(0f, delay);
            smoke.lifetime = Mathf.Max(.1f, lifetime);
            var color = source.color;
            color.a *= opacity;
            smoke.initialColor = color;
            smoke.visual = AddRenderer(go, source);
            if (frames.Length > 1)
            {
                var blend = new GameObject("Next Smoke Frame");
                blend.transform.SetParent(go.transform, false);
                smoke.blendVisual = AddRenderer(blend, source);
            }
            var bounds = frames[0].bounds.size;
            go.transform.localScale = new Vector3(size.x / bounds.x, size.y / bounds.y, 1f);
            smoke.Draw();
            return smoke;
        }

        private static SpriteRenderer AddRenderer(GameObject target, SpriteRenderer source)
        {
            var renderer = target.AddComponent<SpriteRenderer>();
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder + 1;
            renderer.flipX = source.flipX ^ (source.transform.lossyScale.x < 0f);
            renderer.flipY = source.flipY ^ (source.transform.lossyScale.y < 0f);
            return renderer;
        }

        private void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (IsFinished) return;
            age += Mathf.Max(0f, deltaTime);
            if (age >= lifetime)
            {
                IsFinished = true; gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
                return;
            }
            Draw();
        }

        private void Draw()
        {
            float visibleAge = Mathf.Max(0f, age);
            transform.position = origin + velocity * visibleAge;
            float appear = Mathf.SmoothStep(0f, 1f, visibleAge / Mathf.Min(.18f, lifetime * .22f));
            float dissipate = Mathf.SmoothStep(0f, 1f, (lifetime - age) / (lifetime * .28f));
            var color = initialColor;
            color.a *= appear * dissipate;
            float playback = visibleAge / lifetime * frames.Length;
            int index = Mathf.Min((int)playback, frames.Length - 1);
            visual.sprite = frames[index];
            if (blendVisual != null && index + 1 < frames.Length)
            {
                // Overlap consecutive drawings briefly while keeping both on the same drifting origin.
                float mix = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.6f, 1f, playback - index));
                var nextColor = color;
                nextColor.a *= mix;
                blendVisual.sprite = frames[index + 1];
                blendVisual.color = nextColor;
                color.a *= 1f - mix;
            }
            else if (blendVisual != null) blendVisual.sprite = null;
            visual.color = color;
        }
    }
}
