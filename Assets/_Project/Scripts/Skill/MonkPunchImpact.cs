using UnityEngine;

namespace EasternFantasy.Skill
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MonkPunchImpact : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(1f)] private float framesPerSecond = 25f;
        private SpriteRenderer visual;
        private float elapsed;
        private void Awake() => visual = GetComponent<SpriteRenderer>();
        private void Update()
        {
            elapsed += Time.deltaTime;
            int frame = Mathf.FloorToInt(elapsed * framesPerSecond);
            if (frames == null || frame >= frames.Length)
            {
                Destroy(gameObject);
                return;
            }
            visual.sprite = frames[frame];
        }
    }
}