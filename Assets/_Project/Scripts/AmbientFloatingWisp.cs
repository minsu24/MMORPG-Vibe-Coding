using UnityEngine;

namespace EasternFantasy.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AmbientFloatingWisp : MonoBehaviour
    {
        public Sprite[] frames;
        [Min(.1f)] public float framesPerSecond = 5f;
        public Vector2 amplitude = new Vector2(.18f, .24f);
        public Vector2 speed = new Vector2(.65f, 1.1f);
        public float phase;
        [Range(0f, 1f)] public float opacity = .8f;

        private SpriteRenderer visual;
        private Vector3 restPosition, restScale;
        private Color restColor;
        private float elapsed;
        private bool initialized;

        private void OnEnable() => CaptureRestPose();

        private void CaptureRestPose()
        {
            if (initialized) return;
            visual = GetComponent<SpriteRenderer>();
            restPosition = transform.localPosition;
            restScale = transform.localScale;
            restColor = visual.color;
            initialized = true;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            SampleAtTime(elapsed);
        }

        // Deterministic sampling allows a visual preview without running gameplay.
        public void SampleAtTime(float seconds)
        {
            CaptureRestPose();
            transform.localPosition = restPosition + new Vector3(
                Mathf.Sin(seconds * speed.x + phase) * amplitude.x,
                Mathf.Sin(seconds * speed.y + phase * 1.7f) * amplitude.y, 0f);
            float pulse = Mathf.Sin(seconds * .83f + phase);
            transform.localScale = restScale * (1f + pulse * .025f);
            visual.color = new Color(restColor.r, restColor.g, restColor.b,
                restColor.a * opacity * (.93f + pulse * .07f));
            if (frames != null && frames.Length > 0)
            {
                int frame = Mathf.FloorToInt(Mathf.Repeat(seconds * framesPerSecond + phase, frames.Length));
                if (frames[frame] != null) visual.sprite = frames[frame];
            }
        }
    }
}
