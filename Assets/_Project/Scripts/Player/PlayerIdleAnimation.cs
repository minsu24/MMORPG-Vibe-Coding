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

        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float movementThreshold = 0.05f;
        [SerializeField] private string attackStateName = "PlayerAttack";
        [SerializeField] private string throwStateName = "PlayerThrowTails";
        [SerializeField] private string idleStateName = "PlayerIdle";
        [SerializeField] private string deathStateName = "PlayerDead";
        [Tooltip("Enable when the source sprites face right at positive X scale.")]
        [SerializeField] private bool spriteFacesRight;

        private PlayerMovement2D movement;
        private PlayerEntity playerEntity;
        private bool wasMoving;
        private Coroutine attackLockRoutine;
        private int attackStateHash;
        private int throwStateHash;
        private bool hasMovingParameter;
        private bool hasJumpingParameter;
        private bool hasAttackParameter;

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

            attackStateHash = Animator.StringToHash(attackStateName);
            throwStateHash = Animator.StringToHash(throwStateName);
            CacheAnimatorParameters();

            wasMoving = movement != null
                && Mathf.Abs(movement.MoveInput) > movementThreshold;
            if (hasMovingParameter)
                animator.SetBool(IsMovingParameter, wasMoving);
            bool positiveScale = animator.transform.localScale.x >= 0f;
            FacingDirectionX = positiveScale == spriteFacesRight ? 1f : -1f;
        }

        private void LateUpdate()
        {
            if (playerEntity != null && playerEntity.IsDead)
                return;
            if (playerEntity != null && playerEntity.isKnockback)
                return;

            float horizontalInput = movement != null ? movement.MoveInput : 0f;
            bool isMoving = Mathf.Abs(horizontalInput) > movementThreshold;
            if (isMoving) ApplyFacingDirection(horizontalInput);
            if (isMoving == wasMoving) return;

            wasMoving = isMoving;
            if (hasMovingParameter)
                animator.SetBool(IsMovingParameter, isMoving);
        }

        public bool TryRequestAttack()
        {
            if (!isActiveAndEnabled || IsAttacking || !hasAttackParameter)
                return false;

            IsAttacking = true;
            animator.ResetTrigger(AttackParameter);
            animator.SetTrigger(AttackParameter);
            attackLockRoutine = StartCoroutine(WaitForAttackAnimation(attackStateHash));
            return true;
        }

        public bool TryRequestThrow()
        {
            if (!isActiveAndEnabled || IsAttacking)
                return false;

            string statePath = "Base Layer." + throwStateName;
            if (!animator.HasState(0, Animator.StringToHash(statePath)))
            {
                Debug.LogWarning($"Throw animation state '{statePath}' was not found.", animator);
                return false;
            }

            IsAttacking = true;
            if (hasAttackParameter)
                animator.ResetTrigger(AttackParameter);
            animator.Play(statePath, 0, 0f);
            attackLockRoutine = StartCoroutine(WaitForAttackAnimation(throwStateHash));
            return true;
        }

        public void SetJumping(bool isJumping)
        {
            if (animator != null && hasJumpingParameter)
                animator.SetBool(IsJumpingParameter, isJumping);
        }

        public void PlayDeath()
        {
            if (attackLockRoutine != null)
                StopCoroutine(attackLockRoutine);
            attackLockRoutine = null;
            IsAttacking = false;
            if (hasAttackParameter)
                animator.ResetTrigger(AttackParameter);
            SetJumping(false);
            if (hasMovingParameter)
                animator.SetBool(IsMovingParameter, false);
            string statePath = "Base Layer." + deathStateName;
            if (animator.HasState(0, Animator.StringToHash(statePath)))
            {
                animator.Play(statePath, 0, 0f);
                animator.Update(0f);
            }
            else
                Debug.LogWarning($"Death animation state '{statePath}' was not found.", animator);
        }

        public void PlayIdle()
        {
            wasMoving = false;
            if (hasMovingParameter)
                animator.SetBool(IsMovingParameter, false);
            SetJumping(false);
            animator.Play("Base Layer." + idleStateName, 0, 0f);
        }

        private IEnumerator WaitForAttackAnimation(int stateHash)
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

                bool currentIsAttack = current.shortNameHash == stateHash;
                bool nextIsAttack = transitioning && next.shortNameHash == stateHash;

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

            if (animator != null && animator.isInitialized && hasAttackParameter)
                animator.ResetTrigger(AttackParameter);

            attackLockRoutine = null;
            IsAttacking = false;
        }

        private void CacheAnimatorParameters()
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == IsMovingParameter
                    && parameter.type == AnimatorControllerParameterType.Bool)
                    hasMovingParameter = true;
                else if (parameter.nameHash == IsJumpingParameter
                    && parameter.type == AnimatorControllerParameterType.Bool)
                    hasJumpingParameter = true;
                else if (parameter.nameHash == AttackParameter
                    && parameter.type == AnimatorControllerParameterType.Trigger)
                    hasAttackParameter = true;
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(attackStateName))
                attackStateName = "PlayerAttack";
            if (string.IsNullOrWhiteSpace(throwStateName))
                throwStateName = "PlayerThrowTails";
            if (string.IsNullOrWhiteSpace(deathStateName))
                deathStateName = "PlayerDead";
        }

        private void ApplyFacingDirection(float horizontalInput)
        {
            Transform visual = animator.transform;
            Vector3 scale = visual.localScale;
            float magnitude = Mathf.Abs(scale.x);
            bool faceRight = horizontalInput > 0f;
            bool usePositiveScale = faceRight == spriteFacesRight;
            scale.x = usePositiveScale ? magnitude : -magnitude;
            visual.localScale = scale;
            FacingDirectionX = faceRight ? 1f :- 1f;
        }
    }
}
