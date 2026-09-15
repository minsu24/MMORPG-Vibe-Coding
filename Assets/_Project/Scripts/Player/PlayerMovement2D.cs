using UnityEngine;

namespace EasternFantasy.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class PlayerMovement2D : MonoBehaviour
    {
        [SerializeField] private PlayerMovementSettings settings;
        [SerializeField] private LayerMask groundLayers;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private Rigidbody2D body;
        private CapsuleCollider2D bodyCollider;
        private ContactFilter2D groundFilter;
        private float moveInput;
        private bool jumpRequested;

        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            if (settings == null)
            {
                Debug.LogError("PlayerMovement2D requires movement settings.", this);
                enabled = false;
                return;
            }
            body.gravityScale = settings.GravityScale;
            body.freezeRotation = true;
            groundFilter.SetLayerMask(groundLayers);
            groundFilter.useTriggers = false;
        }

        public void SetMoveInput(float value) => moveInput = Mathf.Clamp(value, -1f, 1f);
        public void RequestJump() => jumpRequested = true;

        public void ClearInput()
        {
            moveInput = 0f;
            jumpRequested = false;
        }

        private void FixedUpdate()
        {
            IsGrounded = false;
            // A wall contact must not grant a jump; only upward-facing surfaces count.
            if (body.linearVelocity.y <= 0.1f)
            {
                int count = bodyCollider.Cast(Vector2.down, groundFilter, groundHits,
                    settings.GroundCheckDistance);
                for (int i = 0; i < count; i++)
                    if (groundHits[i].normal.y >= 0.65f)
                    {
                        IsGrounded = true;
                        break;
                    }
            }

            Vector2 velocity = body.linearVelocity;
            velocity.x = moveInput * settings.MoveSpeed;
            if (jumpRequested && IsGrounded)
            {
                velocity.y = settings.JumpSpeed;
                IsGrounded = false;
            }
            // Consume airborne presses too, preventing an unintended jump on landing.
            jumpRequested = false;
            body.linearVelocity = velocity;
        }

        private void OnDisable()
        {
            ClearInput();
            if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }
    }
}
