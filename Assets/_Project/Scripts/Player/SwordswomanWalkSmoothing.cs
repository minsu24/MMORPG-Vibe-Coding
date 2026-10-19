using UnityEngine;

namespace EasternFantasy.Player
{
    // Sprite animation is discrete. Briefly fade the outgoing walk pose over the
    // incoming pose to soften frame changes without speeding up the walk cycle.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    [DefaultExecutionOrder(100)]
    public sealed class SwordswomanWalkSmoothing : MonoBehaviour
    {
        [SerializeField] private string walkStateName = "SwordWalk";
        [SerializeField, Range(0f, 0.1f), Tooltip("Seconds to soften a walk-frame change. Set to zero to disable.")]
        private float blendDuration = 0.045f;

        private Animator animator;
        private SpriteRenderer current;
        private SpriteRenderer outgoing;
        private PlayerMovement2D movement;
        private PlayerIdleAnimation animationController;
        private PlayerEntity entity;
        private Sprite previousSprite;
        private int walkStateHash;
        private float elapsed;
        private float previousFacing;
        private bool wasWalking;
        private Color outgoingColor;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            current = GetComponent<SpriteRenderer>();
            movement = GetComponentInParent<PlayerMovement2D>();
            animationController = GetComponentInParent<PlayerIdleAnimation>();
            entity = GetComponentInParent<PlayerEntity>();
            walkStateHash = Animator.StringToHash(walkStateName);
        }

        private void LateUpdate()
        {
            bool walking = animator.isInitialized && animator.enabled && current.enabled
                && !current.forceRenderingOff && current.sprite != null
                && movement != null && movement.IsGrounded
                && !movement.IsMovementLocked && !movement.IsDashing
                && animationController != null && !animationController.IsAttacking
                && entity != null && !entity.IsDead && !entity.isKnockback
                && animator.GetCurrentAnimatorStateInfo(0).shortNameHash == walkStateHash;
            float facing = Mathf.Sign(transform.localScale.x);
            if (!walking || blendDuration <= 0f || (wasWalking && facing != previousFacing))
            {
                ClearBlend();
                previousSprite = current.sprite;
                previousFacing = facing;
                wasWalking = walking;
                return;
            }

            if (wasWalking && previousSprite != null && current.sprite != previousSprite)
            {
                BeginBlend(previousSprite);
            }
            else if (outgoing != null && outgoing.enabled)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / blendDuration);
                // Keep the incoming renderer fully opaque; overlapping pixels
                // stay solid instead of becoming darker halfway through a fade.
                Color color = outgoingColor;
                color.a *= 1f - Mathf.SmoothStep(0f, 1f, progress);
                outgoing.color = color;
                if (progress >= 1f) outgoing.enabled = false;
            }

            previousSprite = current.sprite;
            previousFacing = facing;
            wasWalking = true;
        }

        private void BeginBlend(Sprite sprite)
        {
            if (outgoing == null)
            {
                var pose = new GameObject("Walk Pose Transition");
                pose.layer = gameObject.layer;
                pose.transform.SetParent(transform, false);
                outgoing = pose.AddComponent<SpriteRenderer>();
            }
            outgoing.sprite = sprite;
            outgoing.sharedMaterials = current.sharedMaterials;
            outgoing.sortingLayerID = current.sortingLayerID;
            outgoing.sortingOrder = current.sortingOrder + 1;
            outgoing.flipX = current.flipX;
            outgoing.flipY = current.flipY;
            outgoing.maskInteraction = current.maskInteraction;
            outgoing.spriteSortPoint = current.spriteSortPoint;
            outgoingColor = current.color;
            outgoing.color = outgoingColor;
            outgoing.enabled = true;
            elapsed = 0f;
        }

        private void ClearBlend()
        {
            if (outgoing != null) outgoing.enabled = false;
            elapsed = 0f;
        }

        private void OnDisable()
        {
            ClearBlend();
            previousSprite = null;
            wasWalking = false;
        }

        private void OnDestroy()
        {
            if (outgoing != null) Destroy(outgoing.gameObject);
        }
    }
}
