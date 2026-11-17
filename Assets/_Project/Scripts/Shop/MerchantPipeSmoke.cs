using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Shop
{
    public sealed class MerchantPipeSmoke : MonoBehaviour
    {
        private SpriteRenderer visual;
        private SpriteRenderer nextVisual;
        private Sprite[] frames;
        private Vector3 origin, velocity;
        private Color initialColor;
        private float age, lifetime;
        public bool IsFinished { get; private set; }

        public static MerchantPipeSmoke Create(SpriteRenderer source, Sprite[] frames, Vector3 position,
            Vector2 size, Vector3 velocity, float lifetime)
        {
            var go = new GameObject("Monkey Merchant Detached Pipe Smoke");
            SceneManager.MoveGameObjectToScene(go, source.gameObject.scene);
            go.transform.SetPositionAndRotation(position, source.transform.rotation);
            var smoke = go.AddComponent<MerchantPipeSmoke>();
            smoke.frames = frames; smoke.origin = position; smoke.velocity = velocity;
            smoke.lifetime = Mathf.Max(.1f, lifetime);
            smoke.initialColor = source.color;
            smoke.visual = go.AddComponent<SpriteRenderer>();
            smoke.visual.sharedMaterial = source.sharedMaterial;
            smoke.visual.sortingLayerID = source.sortingLayerID;
            smoke.visual.sortingOrder = source.sortingOrder + 1;
            smoke.visual.flipX = source.flipX ^ (source.transform.lossyScale.x < 0f);
            smoke.visual.flipY = source.flipY ^ (source.transform.lossyScale.y < 0f);
            var next = new GameObject("Smoke frame blend");
            next.transform.SetParent(go.transform, false);
            smoke.nextVisual = next.AddComponent<SpriteRenderer>();
            smoke.nextVisual.sharedMaterial = source.sharedMaterial;
            smoke.nextVisual.sortingLayerID = source.sortingLayerID;
            smoke.nextVisual.sortingOrder = smoke.visual.sortingOrder + 1;
            smoke.nextVisual.flipX = smoke.visual.flipX; smoke.nextVisual.flipY = smoke.visual.flipY;
            // One constant scale preserves the source animation's gradual thinning.
            go.transform.localScale = new Vector3(size.x / frames[0].bounds.size.x, size.y / frames[0].bounds.size.y, 1f);
            smoke.Draw();
            return smoke;
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
            transform.position = origin + velocity * age;
            float progress = age / lifetime * frames.Length;
            int frame = Mathf.Clamp(Mathf.FloorToInt(progress), 0, frames.Length - 1);
            int next = Mathf.Min(frame + 1, frames.Length - 1);
            float blend = next == frame ? 0f : progress - frame;
            visual.sprite = frames[frame];
            nextVisual.sprite = frames[next];
            var color = initialColor;
            color.a *= Mathf.Clamp01((lifetime - age) / (lifetime * .45f));
            var nextColor = color; nextColor.a *= blend; color.a *= 1f - blend;
            visual.color = color;
            nextVisual.color = nextColor;
        }
    }
}
