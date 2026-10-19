using UnityEngine;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement2D), typeof(CapsuleCollider2D))]
    public sealed class MonkJumpAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private LayerMask groundLayers;
        private static readonly int Jumping = Animator.StringToHash("isJumping");
        private static readonly int Falling = Animator.StringToHash("Base Layer.MonkFall");
        private static readonly int Rising = Animator.StringToHash("Base Layer.MonkJump");
        private static readonly int Landing = Animator.StringToHash("Base Layer.MonkLand");
        private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];
        private CapsuleCollider2D bodyCollider;
        private Rigidbody2D body;
        private PlayerMovement2D movement;
        private PlayerEntity entity;
        private PlayerIdleAnimation animationDriver;
        private ContactFilter2D groundFilter;
        private bool wasAirborne;

        private void Awake()
        {
            bodyCollider = GetComponent<CapsuleCollider2D>();
            body = GetComponent<Rigidbody2D>();
            movement = GetComponent<PlayerMovement2D>();
            entity = GetComponent<PlayerEntity>();
            animationDriver = GetComponent<PlayerIdleAnimation>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            groundFilter.SetLayerMask(groundLayers);
            groundFilter.useTriggers = false;
        }

        private void Update()
        {
            if (animator == null || entity == null || entity.IsDead) { wasAirborne = false; return; }
            bool grounded = false;
            if (body.linearVelocity.y <= .1f)
            {
                int count = bodyCollider.GetContacts(groundFilter, contacts);
                for (int i = 0; i < count; i++)
                    if (contacts[i].normal.y >= .65f) { grounded = true; break; }
            }
            // Movement uses a short ground cast for jumping; animation waits for real contact.
            animator.SetBool(Jumping, !grounded);
            if (!grounded) {
                wasAirborne = true;
                UpdateAirbornePose();
            }
            else CompleteLanding();
        }

        private bool CanDrivePose => !entity.isKnockback && !movement.IsDashing && !movement.IsMovementLocked
            && (animationDriver == null || !animationDriver.IsAttacking);

        private void UpdateAirbornePose()
        {
            if (!CanDrivePose) return;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (body.linearVelocity.y < 0f) {
                // Start the falling pose immediately at the apex, including walking off a ledge.
                if (state.IsName("MonkJump") || state.IsName("MonkIdle")
                    || state.IsName("MonkMove") || state.IsName("MonkLand"))
                    animator.Play(Falling, 0, 0f);
            }
            else if (body.linearVelocity.y > 0f && state.IsName("MonkFall"))
                animator.Play(Rising, 0, 0f);
        }

        private void OnCollisionEnter2D(Collision2D collision) => CheckLanding(collision);
        private void OnCollisionStay2D(Collision2D collision) => CheckLanding(collision);

        private void CheckLanding(Collision2D collision)
        {
            if (!wasAirborne || body.linearVelocity.y > .1f
                || (groundLayers.value & (1 << collision.gameObject.layer)) == 0) return;
            for (int i = 0; i < collision.contactCount; i++)
                if (collision.GetContact(i).normal.y >= .65f) { CompleteLanding(); break; }
        }

        private void CompleteLanding()
        {
            if (!wasAirborne) return;
            wasAirborne = false;
            if (animator == null || entity == null || entity.IsDead) return;
            animator.SetBool(Jumping, false);
            if (!CanDrivePose) return;
            animator.Play(Landing, 0, 0f);
        }

        private void OnDisable() => wasAirborne = false;
    }
}
