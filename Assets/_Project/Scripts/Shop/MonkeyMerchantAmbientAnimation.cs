using UnityEngine;

namespace EasternFantasy.Shop
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class MonkeyMerchantAmbientAnimation : MonoBehaviour
    {
        public const string IdleState = "MonkeyMerchantIdle";
        public const string PipeState = "MonkeyMerchantPipe";
        public const string EnterPipeState = "MonkeyMerchantEnterPipe";
        [SerializeField] private Vector2 pipeInterval = new Vector2(8f, 14f);
        [SerializeField, Min(.1f)] private float pipeDuration = 2.4f;
        [SerializeField, Min(.1f)] private float transitionDuration = .64f;
        [SerializeField] private Sprite idleAnchorFrame;
        [Header("Smoke starts when the eighth pipe frame begins")]
        [SerializeField] private Sprite[] smokeFrames;
        [SerializeField] private Vector2 smokeLocalOffset;
        [SerializeField] private Vector2 smokeLocalSize = new Vector2(1.5f, 1.6f);
        [SerializeField] private Vector2 smokeDrift = new Vector2(.45f, .65f);
        [SerializeField, Min(.1f)] private float smokeLifetime = 1.6f;
        private Animator animator;
        private SpriteRenderer visual;
        private enum Phase { Idle, EnteringPipe, Smoking }
        private Phase phase;
        private float elapsed, nextPipeDelay;
        public bool IsSmoking { get; private set; }
        public bool IsTransitioning => phase == Phase.EnteringPipe;

        private void OnEnable()
        {
            animator = GetComponent<Animator>();
            visual = GetComponent<SpriteRenderer>();
            ReturnToIdle();
        }

        private void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (!isActiveAndEnabled || animator == null || animator.runtimeAnimatorController == null) return;
            elapsed += Mathf.Max(0f, deltaTime);
            if (phase == Phase.EnteringPipe)
            {
                if (elapsed >= transitionDuration) StartSmoking();
            }
            else if (phase == Phase.Smoking)
            {
                // The final sheet frame already contains painted smoke. Hand off at its
                // start, rather than displaying it and replacing it with another cloud later.
                if (elapsed >= pipeDuration * 7f / 8f)
                {
                    ReleaseSmoke();
                    ReturnToIdle();
                }
            }
            else if (elapsed >= nextPipeDelay && (idleAnchorFrame == null || visual == null || visual.sprite == idleAnchorFrame))
            {
                // Wait for the matching idle pose instead of cutting an arbitrary breathing frame.
                if (animator.HasState(0, Animator.StringToHash(EnterPipeState)))
                {
                    elapsed = 0f; phase = Phase.EnteringPipe;
                    animator.Play(EnterPipeState, 0, 0f);
                }
                else StartSmoking();
            }
        }

        private void StartSmoking()
        {
            elapsed = 0f; phase = Phase.Smoking; IsSmoking = true;
            animator.Play(PipeState, 0, 0f);
        }

        private void ReleaseSmoke()
        {
            if (smokeFrames == null || smokeFrames.Length == 0) return;
            var source = GetComponent<SpriteRenderer>();
            if (source == null) return;
            var offset = smokeLocalOffset;
            var drift = smokeDrift;
            if (source.flipX) { offset.x = -offset.x; drift.x = -drift.x; }
            if (source.flipY) { offset.y = -offset.y; drift.y = -drift.y; }
            var scale = transform.lossyScale;
            var size = new Vector2(smokeLocalSize.x * Mathf.Abs(scale.x), smokeLocalSize.y * Mathf.Abs(scale.y));
            MerchantPipeSmoke.Create(source, smokeFrames, transform.TransformPoint(offset), size,
                transform.TransformVector(drift), smokeLifetime);
        }

        private void ReturnToIdle()
        {
            elapsed = 0f; IsSmoking = false; phase = Phase.Idle;
            float minimum = Mathf.Max(.5f, pipeInterval.x);
            nextPipeDelay = Random.Range(minimum, Mathf.Max(minimum, pipeInterval.y));
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.Play(IdleState, 0, 0f);
        }
    }
}
