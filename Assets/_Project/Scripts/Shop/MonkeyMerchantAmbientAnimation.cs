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
        public const string ReturnFromPipeState = "MonkeyMerchantReturnFromPipe";
        public const float EnterPipeDuration = 1.2f;
        public const float PipeDuration = 2.1f;
        public const float ReturnFromPipeDuration = 1.18f;
        [SerializeField] private Vector2 pipeInterval = new Vector2(8f, 14f);
        [SerializeField, Min(.1f)] private float pipeDuration = PipeDuration;
        [SerializeField, Min(.1f)] private float transitionDuration = EnterPipeDuration;
        [SerializeField] private Sprite idleAnchorFrame;
        [Header("Smoke released on the exhale frame")]
        [SerializeField] private Sprite smokeSprite;
        [SerializeField] private Sprite[] smokeFrames;
        [SerializeField] private Sprite smokeExhaleFrame;
        [SerializeField] private Vector2 smokeMouthLocalOffset;
        [SerializeField] private Vector2 smokeLocalOffset;
        [SerializeField] private Vector2 smokeLocalSize = new Vector2(1.5f, 1.6f);
        [SerializeField] private Vector2 smokeDrift = new Vector2(.63f, .66f);
        [SerializeField, Min(.1f)] private float smokeLifetime = 2.35f;
        private Animator animator;
        private SpriteRenderer visual;
        private enum Phase { Idle, EnteringPipe, Smoking, ReturningToIdle }
        private Phase phase;
        private float elapsed, nextPipeDelay;
        private bool smokeReleased;
        public bool IsSmoking { get; private set; }
        public bool IsTransitioning => phase == Phase.EnteringPipe || phase == Phase.ReturningToIdle;

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
                if (!smokeReleased && (elapsed >= pipeDuration ||
                    (smokeExhaleFrame != null && visual != null && visual.sprite == smokeExhaleFrame)))
                {
                    ReleaseSmoke();
                    smokeReleased = true;
                }
                if (elapsed >= pipeDuration)
                {
                    if (animator.HasState(0, Animator.StringToHash(ReturnFromPipeState)))
                    {
                        elapsed = 0f;
                        IsSmoking = false;
                        phase = Phase.ReturningToIdle;
                        animator.Play(ReturnFromPipeState, 0, 0f);
                    }
                    else ReturnToIdle();
                }
            }
            else if (phase == Phase.ReturningToIdle)
            {
                if (elapsed >= ReturnFromPipeDuration) ReturnToIdle();
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
            smokeReleased = false;
            animator.Play(PipeState, 0, 0f);
        }

        private void ReleaseSmoke()
        {
            var frames = smokeFrames;
            if (frames == null || frames.Length == 0)
            {
                if (smokeSprite == null) return;
                frames = new[] { smokeSprite };
            }
            var source = visual;
            if (source == null) return;
            var flip = new Vector2(source.flipX ? -1f : 1f, source.flipY ? -1f : 1f);
            var offset = Vector2.Scale(smokeLocalOffset, flip);
            var drift = transform.TransformVector(smokeDrift);
            var scale = transform.lossyScale;
            var size = new Vector2(smokeLocalSize.x * Mathf.Abs(scale.x), smokeLocalSize.y * Mathf.Abs(scale.y));
            float cloudDelay = 0f;
            if (smokeMouthLocalOffset != Vector2.zero)
            {
                const float wispScale = .28f;
                // Keep the tail at the lips when the plume is scaled down.
                var wispOffset = Vector2.Scale(Vector2.Lerp(smokeMouthLocalOffset, smokeLocalOffset, wispScale), flip);
                var wisp = MerchantPipeSmoke.Create(source, frames, transform.TransformPoint(wispOffset),
                    size * wispScale, drift * .55f, .70f, opacity: .65f);
                wisp.gameObject.name = "Monkey Merchant Mouth Smoke";
                cloudDelay = .14f;
            }
            MerchantPipeSmoke.Create(source, frames, transform.TransformPoint(offset), size, drift, smokeLifetime, delay: cloudDelay);
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
