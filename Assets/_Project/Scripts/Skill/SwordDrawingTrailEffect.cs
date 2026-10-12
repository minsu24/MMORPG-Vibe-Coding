using UnityEngine;

namespace EasternFantasy.Skill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SwordDrawingTrailEffect : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(0.01f)] private float duration = 0.32f;
        private SpriteRenderer spriteRenderer;
        private float elapsed;
        private float length;
        private float height;
        private float direction = 1f;

        private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();
        public void Play(float pathLength, float pathHeight, float facing, SpriteRenderer character)
        {
            length = Mathf.Max(0.01f, pathLength);
            height = Mathf.Max(0.01f, pathHeight);
            direction = facing < 0f ? -1f : 1f;
            elapsed = 0f;
            if (character != null)
            {
                spriteRenderer.sortingLayerID = character.sortingLayerID;
                spriteRenderer.sortingOrder = character.sortingOrder + 2;
            }
            UpdateFrame();
        }
        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration) { Destroy(gameObject); return; }
            UpdateFrame();
        }
        private void UpdateFrame()
        {
            if (frames == null || frames.Length == 0) return;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(.01f, duration));
            int index = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(progress * frames.Length));
            Sprite sprite = frames[index];
            if (sprite == null) return;
            spriteRenderer.sprite = sprite;
            transform.localScale = new Vector3(direction * length / sprite.bounds.size.x,
                height / sprite.bounds.size.y, 1f);
            spriteRenderer.color = new Color(1f, 1f, 1f, 1f - Mathf.InverseLerp(.65f, 1f, progress));
        }
    }
}
