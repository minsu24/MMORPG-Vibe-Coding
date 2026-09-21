using System.Collections;
using UnityEngine;

namespace EasternFantasy.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerIdleAnimation : MonoBehaviour
    {
        private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
        private static readonly int IsJumpingParameter = Animator.StringToHash("isJumping");
        private static readonly int AttackParameter = Animator.StringToHash("Attack");
        private static readonly int AttackState = Animator.StringToHash("PlayerAttack");

        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float movementThreshold = 0.05f;

        private PlayerMovement2D movement;
        private PlayerEntity playerEntity;
        private bool wasMoving;
        private Coroutine attackLockRoutine;

        public float FacingDirectionX { get; private set; }
        public bool IsAttacking { get; private set; }

        private void Awake()
        {
            movement = GetComponent<PlayerMovement2D>();
            playerEntity = GetComponent<PlayerEntity>();
            if (animator == null)
            {
                Debug.LogError("PlayerIdleAnimation requires an Animator.", this);
                enabled = false;
                return;
            }

            wasMoving = movement != null
                && Mathf.Abs(movement.MoveInput) > movementThreshold;
            animator.SetBool(IsMovingParameter, wasMoving);
            FacingDirectionX = animator.transform.localScale.x < 0f ? 1f : -1f;
        }

        private void LateUpdate()
        {
            if (playerEntity != null && playerEntity.isKnockback)
                return;

            float horizontalInput = movement != null ? movement.MoveInput : 0f;
            bool isMoving = Mathf.Abs(horizontalInput) > movementThreshold;
            if (isMoving) ApplyFacingDirection(horizontalInput);
            if (isMoving == wasMoving) return;

            wasMoving = isMoving;
            animator.SetBool(IsMovingParameter, isMoving);
        }

        public bool TryRequestAttack()
        {
            if (!isActiveAndEnabled || IsAttacking)
                return false;

            IsAttacking = true;
            animator.ResetTrigger(AttackParameter);
            animator.SetTrigger(AttackParameter);
            attackLockRoutine = StartCoroutine(WaitForAttackAnimation());
            return true;
        }

        public void SetJumping(bool isJumping)
        {
            if (animator != null)
                animator.SetBool(IsJumpingParameter, isJumping);
        }

        private IEnumerator WaitForAttackAnimation()
        {
            bool enteredAttackState = false;

            while (isActiveAndEnabled)
            {
                yield return null;

                AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
                bool transitioning = animator.IsInTransition(0);
                AnimatorStateInfo next = transitioning
                    ? animator.GetNextAnimatorStateInfo(0)
                    : default;

                bool currentIsAttack = current.shortNameHash == AttackState;
                bool nextIsAttack = transitioning && next.shortNameHash == AttackState;

                if (!enteredAttackState)
                {
                    enteredAttackState = currentIsAttack || nextIsAttack;
                    continue;
                }

                if (!currentIsAttack && !nextIsAttack)
                    break;
            }

            IsAttacking = false;
            attackLockRoutine = null;
        }

        private void OnDisable()
        {
            if (attackLockRoutine != null)
                StopCoroutine(attackLockRoutine);

            if (animator != null)
                animator.ResetTrigger(AttackParameter);

            attackLockRoutine = null;
            IsAttacking = false;
        }

        private void ApplyFacingDirection(float horizontalInput)
        {
            Transform visual = animator.transform;
            Vector3 scale = visual.localScale;
            float magnitude = Mathf.Abs(scale.x);
            scale.x = horizontalInput > 0f ? -magnitude : magnitude;
            visual.localScale = scale;
            FacingDirectionX = horizontalInput > 0f ? 1f : -1f;
        }
    }
}
