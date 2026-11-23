using UnityEngine;

namespace EasternFantasy.Quest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TravelerIdleAnimation : MonoBehaviour
    {
        [SerializeField] private Sprite[] standing, tiptoe, oneFoot;
        [SerializeField, Min(.05f)] private float frameDuration = .17f;
        [SerializeField] private Vector2 variationInterval = new Vector2(5f, 9f);
        private SpriteRenderer visual;
        private Sprite[] current;
        private float elapsed, untilVariation;
        private bool nextTiptoe;

        private void OnEnable()
        {
            visual = GetComponent<SpriteRenderer>();
            nextTiptoe = true;
            StartStanding();
        }

        private void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (visual == null || current == null || current.Length == 0 || deltaTime <= 0f) return;
            elapsed += deltaTime;
            float duration = current.Length * frameDuration;
            if (current == standing)
            {
                untilVariation -= deltaTime;
                if (elapsed >= duration)
                {
                    elapsed %= duration;
                    if (untilVariation <= 0f)
                    {
                        var variation = nextTiptoe ? tiptoe : oneFoot;
                        nextTiptoe = !nextTiptoe;
                        if (variation != null && variation.Length > 0) current = variation;
                        elapsed = 0f;
                    }
                }
            }
            else if (elapsed >= duration) StartStanding();

            visual.sprite = current[Mathf.Min((int)(elapsed / frameDuration), current.Length - 1)];
        }

        private void StartStanding()
        {
            current = standing;
            elapsed = 0f;
            untilVariation = Random.Range(variationInterval.x, variationInterval.y);
            if (current != null && current.Length > 0) visual.sprite = current[0];
        }
    }
}
