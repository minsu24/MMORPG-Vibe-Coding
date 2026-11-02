using System.Collections.Generic;
using UnityEngine;

namespace EasternFantasy.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class PlayerMovement2D : MonoBehaviour
    {
        [SerializeField] private PlayerMovementSettings settings;
        [SerializeField] private LayerMask groundLayers;
        [Header("Jump Audio")]
        [SerializeField] private AudioClip jumpSound;
        [SerializeField, Range(0f, 1f)] private float jumpVolume = 0.5f;
        [SerializeField, Range(0.5f, 1.5f)] private float jumpPitch = 1f;
        private AudioSource jumpAudio;
        private PlayerIdleAnimation playerIdleAnimation;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private RaycastHit2D[] dashHits = new RaycastHit2D[16];
        public Rigidbody2D body;
        private CapsuleCollider2D bodyCollider;
        private PlayerEntity playerEntity;
        private EasternFantasy.Inventory.PlayerInventory inventory;
        private ContactFilter2D groundFilter;
        private float moveInput;
        private bool jumpRequested;
        private bool dropThroughRequested;
        private float jumpExpiresAt = float.NegativeInfinity;
        private float lastGroundedAt = float.NegativeInfinity;
        private readonly List<Collider2D> ignoredPlatforms = new List<Collider2D>();
        private readonly List<Collider2D> ignoredPassThroughEnemies = new List<Collider2D>();
        private int groundHitCount;
        private float supportDistance;

        private RigidbodyConstraints2D constraintsBeforeLock;
        public bool IsMovementLocked { get; private set; }
        public bool IsDashing { get; private set; }
        public int PassThroughObstacleMask => Physics2D.GetLayerCollisionMask(gameObject.layer) & ~dashEnemyMask;
        private float dashDirection;
        private float dashDistance;
        private float dashDuration;
        private float dashElapsed;
        private float gravityBeforeDash;
        private float verticalVelocityBeforeDash;
        private CollisionDetectionMode2D collisionModeBeforeDash;
        private LayerMask excludedLayersBeforeDash;
        private int dashEnemyMask;

        public bool IsGrounded { get; private set; }
        public float MoveInput => moveInput;
        public bool IsBasicAttackMovementLocked => IsGrounded && playerIdleAnimation != null && playerIdleAnimation.IsBasicAttackActive;
        public bool IsGroundedAttackMovementLocked => IsGrounded && playerIdleAnimation != null
            && playerIdleAnimation.IsStationaryAttackActive;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            playerEntity = GetComponent<PlayerEntity>();
            inventory = GetComponent<EasternFantasy.Inventory.PlayerInventory>();
            playerIdleAnimation = GetComponent<PlayerIdleAnimation>();
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            dashEnemyMask = enemyLayer >= 0 ? 1 << enemyLayer : 0;
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
            if (jumpSound != null)
            {
                var child = new GameObject("Jump Audio");
                child.transform.SetParent(transform, false);
                jumpAudio = child.AddComponent<AudioSource>();
                jumpAudio.enabled = true;
                jumpAudio.playOnAwake = false;
                jumpAudio.loop = false;
                jumpAudio.spatialBlend = 0f;
            }
        }

        public void SetMoveInput(float value) => moveInput = Mathf.Clamp(value, -1f, 1f);
        public void RequestJump(bool dropThrough = false)
        {
            if (IsMovementLocked || IsDashing || IsGroundedAttackMovementLocked || (playerEntity != null
                && (playerEntity.IsDead || playerEntity.isKnockback))) return;
            jumpRequested = true;
            dropThroughRequested = dropThrough;
            jumpExpiresAt = Time.time + settings.JumpBufferDuration;
        }

        public void StopMovementForGroundedAttack()
        {
            if (!IsGrounded) return;
            jumpRequested = false;
            dropThroughRequested = false;
            jumpExpiresAt = float.NegativeInfinity;
            // Only grounded attacks stop movement; jump attacks retain full air control.
            if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        private static bool IsOneWayPlatform(Collider2D collider)
        {
            if (collider == null || !collider.usedByEffector) return false;
            PlatformEffector2D effector = collider.GetComponent<PlatformEffector2D>();
            return effector != null && effector.isActiveAndEnabled && effector.useOneWay;
        }

        private bool IsGroundHit(RaycastHit2D hit)
        {
            if (hit.collider == null || hit.normal.y < 0.65f
                || ignoredPlatforms.Contains(hit.collider)
                || Physics2D.GetIgnoreCollision(bodyCollider, hit.collider)) return false;
            // Casts can still report a one-way platform while passing it from below.
            return !IsOneWayPlatform(hit.collider)
                || bodyCollider.bounds.min.y >= hit.collider.bounds.max.y - settings.GroundCheckDistance;
        }

        private bool TryDropThrough()
        {
            // Every surface actually supporting the player must allow dropping.
            // This also handles the seam between adjacent platform colliders.
            for (int i = 0; i < groundHitCount; i++)
            {
                RaycastHit2D hit = groundHits[i];
                if (!IsGroundHit(hit) || hit.distance > supportDistance + 0.01f) continue;
                if (!IsOneWayPlatform(hit.collider)) return false;
            }
            bool dropped = false;
            for (int i = 0; i < groundHitCount; i++)
            {
                RaycastHit2D hit = groundHits[i];
                if (!IsGroundHit(hit) || hit.distance > supportDistance + 0.01f) continue;
                ignoredPlatforms.Add(hit.collider);
                Physics2D.IgnoreCollision(bodyCollider, hit.collider, true);
                dropped = true;
            }
            return dropped;
        }

        private void RestorePlatformCollisions(bool restoreAll = false)
        {
            for (int i = ignoredPlatforms.Count - 1; i >= 0; i--)
            {
                Collider2D platform = ignoredPlatforms[i];
                // Restore after the whole player is below the top surface, or has
                // moved sideways off the platform. A timer can expire inside it.
                if (!restoreAll && platform != null && bodyCollider != null
                    && bodyCollider.bounds.max.y >= platform.bounds.max.y - 0.02f
                    && bodyCollider.bounds.max.x >= platform.bounds.min.x
                    && bodyCollider.bounds.min.x <= platform.bounds.max.x) continue;
                if (platform != null && bodyCollider != null)
                    Physics2D.IgnoreCollision(bodyCollider, platform, false);
                ignoredPlatforms.RemoveAt(i);
            }
        }

        public bool CanDash => isActiveAndEnabled && body != null && bodyCollider != null
            && !IsMovementLocked && !IsDashing && playerEntity != null
            && !playerEntity.IsDead && !playerEntity.isKnockback;

        public bool BeginDash(float direction, float distance, float duration)
        {
            if (!CanDash || distance <= 0f || duration <= 0f) return false;
            dashDirection = direction < 0f ? -1f : 1f;
            dashDistance = distance;
            dashDuration = duration;
            dashElapsed = 0f;
            gravityBeforeDash = body.gravityScale;
            verticalVelocityBeforeDash = body.linearVelocity.y;
            collisionModeBeforeDash = body.collisionDetectionMode;
            excludedLayersBeforeDash = bodyCollider.excludeLayers;
            // Ignore enemies only for this player's body, keeping terrain solid.
            bodyCollider.excludeLayers = excludedLayersBeforeDash.value | dashEnemyMask;
            PrepareEnemyPassThrough();
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearVelocity = Vector2.zero;
            jumpRequested = false;
            dropThroughRequested = false;
            IsDashing = true;
            lastGroundedAt = float.NegativeInfinity;
            return true;
        }

        public void EndDash()
        {
            if (!IsDashing) return;
            IsDashing = false;
            if (bodyCollider != null) bodyCollider.excludeLayers = excludedLayersBeforeDash;
            RestoreEnemyPassThrough();
            if (body == null) return;
            body.gravityScale = gravityBeforeDash;
            body.collisionDetectionMode = collisionModeBeforeDash;
            body.linearVelocity = new Vector2(0f, verticalVelocityBeforeDash);
        }

        private void UpdateDash()
        {
            jumpRequested = false;
            if (dashElapsed >= dashDuration || playerEntity.IsDead)
            {
                EndDash();
                return;
            }
            float nextElapsed = Mathf.Min(dashElapsed + Time.fixedDeltaTime, dashDuration);
            float from = dashElapsed / dashDuration;
            float to = nextElapsed / dashDuration;
            // Integrate a fast start and a heavy stop; total travel equals dashDistance.
            float step = dashDistance * ((2f * to - to * to) - (2f * from - from * from));
            Vector2 direction = new Vector2(dashDirection, 0f);
            ContactFilter2D filter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = true,
                layerMask = PassThroughObstacleMask
            };
            int count;
            while (true)
            {
                count = bodyCollider.Cast(direction, filter, dashHits, step + 0.02f);
                if (count < dashHits.Length) break;
                System.Array.Resize(ref dashHits, dashHits.Length * 2);
            }
            for (int i = 0; i < count; i++)
            {
                if (dashHits[i].collider == null || dashHits[i].collider.GetComponentInParent<EnemyController>() != null) continue;
                if (Vector2.Dot(dashHits[i].normal, direction) >= -0.1f) continue;
                step = Mathf.Min(step, Mathf.Max(0f, dashHits[i].distance - 0.02f));
            }
            body.linearVelocity = direction * (step / Time.fixedDeltaTime);
            dashElapsed = nextElapsed;
        }

        public void SetMovementLocked(bool locked)
        {
            if (IsMovementLocked == locked) return;
            IsMovementLocked = locked;
            if (locked) EndDash();
            jumpRequested = false;
            dropThroughRequested = false;
            lastGroundedAt = float.NegativeInfinity;
            if (body == null) return;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            if (locked)
            {
                constraintsBeforeLock = body.constraints;
                body.constraints = RigidbodyConstraints2D.FreezeAll;
            }
            else body.constraints = constraintsBeforeLock;
        }

        public void PrepareEnemyPassThrough()
        {
            foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                foreach (var collider in enemy.GetComponentsInChildren<Collider2D>())
                {
                    if (!collider.enabled || collider.isTrigger || Physics2D.GetIgnoreCollision(bodyCollider, collider)) continue;
                    Physics2D.IgnoreCollision(bodyCollider, collider, true);
                    ignoredPassThroughEnemies.Add(collider);
                }
        }

        private void RestoreEnemyPassThrough(bool force = false)
        {
            for (int i = ignoredPassThroughEnemies.Count - 1; i >= 0; i--)
            {
                var collider = ignoredPassThroughEnemies[i];
                if (collider == null) { ignoredPassThroughEnemies.RemoveAt(i); continue; }
                // Do not re-enable a collision while ending inside an enemy; restore once clear.
                if (!force && bodyCollider != null && collider.enabled && collider.gameObject.activeInHierarchy
                    && bodyCollider.Distance(collider).distance <= .02f) continue;
                if (bodyCollider != null) Physics2D.IgnoreCollision(bodyCollider, collider, false);
                ignoredPassThroughEnemies.RemoveAt(i);
            }
        }

        public void ClearInput()
        {
            moveInput = 0f;
            jumpRequested = false;
            dropThroughRequested = false;
            jumpExpiresAt = float.NegativeInfinity;
            lastGroundedAt = float.NegativeInfinity;
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            Vector3 previousPosition = transform.position;
            EndDash();
            ClearInput();
            RestorePlatformCollisions(true);
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position = position;
            transform.SetPositionAndRotation(position, rotation);
            Physics2D.SyncTransforms();
            if ((transform.position - previousPosition).sqrMagnitude > .000001f)
                CameraFollowDetail.ResetAfterTeleport(transform);
        }

        private void FixedUpdate()
        {
            RestorePlatformCollisions();
            if (!IsDashing) RestoreEnemyPassThrough();
            if (IsDashing)
            {
                UpdateDash();
                return;
            }
            if (IsMovementLocked)
            {
                jumpRequested = false;
                body.linearVelocity = Vector2.zero;
                return;
            }
            IsGrounded = false;
            groundHitCount = 0;
            supportDistance = float.PositiveInfinity;
            // A wall contact must not grant a jump; only upward-facing surfaces count.
            if (body.linearVelocity.y <= 0.1f)
            {
                groundHitCount = bodyCollider.Cast(Vector2.down, groundFilter, groundHits,
                    settings.GroundCheckDistance);
                for (int i = 0; i < groundHitCount; i++)
                    if (IsGroundHit(groundHits[i]))
                    {
                        IsGrounded = true;
                        supportDistance = Mathf.Min(supportDistance, groundHits[i].distance);
                        if (playerIdleAnimation != null)
                            playerIdleAnimation.SetJumping(false);
                    }
            }

            Vector2 velocity = body.linearVelocity;
            float moveSpeed = playerEntity != null ? playerEntity.Speed : settings.MoveSpeed;
            bool canMove = playerEntity == null || (!playerEntity.IsDead && !playerEntity.isKnockback);
            if (canMove) velocity.x = IsGroundedAttackMovementLocked ? 0f : moveInput * moveSpeed;
            else
            {
                ClearInput();
                body.linearVelocity = velocity;
                return;
            }
            if (IsGroundedAttackMovementLocked)
            {
                jumpRequested = false;
                dropThroughRequested = false;
            }
            if (IsGrounded) lastGroundedAt = Time.time;
            if (jumpRequested && Time.time > jumpExpiresAt)
            {
                jumpRequested = false;
                dropThroughRequested = false;
            }
            bool canJump = IsGrounded || (!dropThroughRequested
                && Time.time - lastGroundedAt <= settings.CoyoteDuration);
            if (jumpRequested && canJump)
            {
                bool dropping = dropThroughRequested && TryDropThrough();
                float heightBonus = inventory != null ? inventory.EquippedJumpHeightBonus : 0f;
                velocity.y = dropping ? -settings.DropThroughSpeed
                    : settings.JumpSpeed * Mathf.Sqrt(1f + heightBonus);
                if (!dropping && jumpAudio != null && jumpSound != null)
                {
                    jumpAudio.enabled = true;
                    jumpAudio.pitch = jumpPitch;
                    jumpAudio.PlayOneShot(jumpSound, jumpVolume);
                }
                if (playerIdleAnimation != null)
                    playerIdleAnimation.SetJumping(true);
                IsGrounded = false;
                lastGroundedAt = float.NegativeInfinity;
                jumpRequested = false;
                dropThroughRequested = false;
            }
            body.linearVelocity = velocity;
        }

        private void OnDisable()
        {
            RestoreEnemyPassThrough(true);
            EndDash();
            RestorePlatformCollisions(true);
            SetMovementLocked(false);
            ClearInput();
            if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }
    }
}
