using UnityEngine;

namespace EasternFantasy.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class PlayerMovement2D : MonoBehaviour
    {
        [SerializeField] private PlayerMovementSettings settings;
        [SerializeField] private LayerMask groundLayers;
        private PlayerIdleAnimation playerIdleAnimation;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        public Rigidbody2D body;
        private CapsuleCollider2D bodyCollider;
        private PlayerEntity playerEntity;
        private ContactFilter2D groundFilter;
        private float moveInput;
        private bool jumpRequested;

        public bool IsGrounded { get; private set; }
        public float MoveInput => moveInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            playerEntity = GetComponent<PlayerEntity>();
            playerIdleAnimation = GetComponent<PlayerIdleAnimation>();
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

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            ClearInput();
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position = position;
            transform.SetPositionAndRotation(position, rotation);
            Physics2D.SyncTransforms();
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
                        if (playerIdleAnimation != null)
                            playerIdleAnimation.SetJumping(false);
                        break;
                    }
            }

            Vector2 velocity = body.linearVelocity;
            float moveSpeed = playerEntity != null ? playerEntity.Speed : settings.MoveSpeed;
            if(playerEntity.isKnockback == false) velocity.x = moveInput * moveSpeed;
            if (jumpRequested && IsGrounded)
            {
                velocity.y = settings.JumpSpeed;
                if (playerIdleAnimation != null)
                    playerIdleAnimation.SetJumping(true);
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
