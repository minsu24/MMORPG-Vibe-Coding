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
        [Tooltip("Enable when walking sprites face the opposite direction to idle sprites.")]
        [SerializeField] private bool movingSpritesFaceOppositeDirection;

        private PlayerMovement2D movement;
        private PlayerEntity playerEntity;
        private PlayerInputReader inputReader;
        private bool wasMoving;
        private Coroutine attackLockRoutine;
        private int attackStateHash;
        private int throwStateHash;
        private bool hasMovingParameter;
        private bool hasJumpingParameter;
        private bool hasAttackParameter;
        private bool lockAttackFacing;
        private bool skillSpeedOverridden;
        private float speedBeforeSkill;
        private float basicAttackClipLength;
        private float basicAttackChainAt = float.PositiveInfinity;
        private bool wasKnockedBack;

        public float FacingDirectionX { get; private set; }
        public bool IsAttacking { get; private set; }
        public bool IsBasicAttackActive { get; private set; }
        public int BasicAttackSequence { get; private set; }
        public bool CanChainBasicAttack => IsBasicAttackActive && Time.time >= basicAttackChainAt;
        public bool CanPerformCombatAction => isActiveAndEnabled
            && (playerEntity == null || (!playerEntity.IsDead && !playerEntity.isKnockback))
            && (movement == null || (!movement.IsMovementLocked && !movement.IsDashing))
            && (inputReader == null || inputReader.CanPerformCombatAction);

        private void Awake()
        {
            movement = GetComponent<PlayerMovement2D>();
            playerEntity = GetComponent<PlayerEntity>();
            inputReader = GetComponent<PlayerInputReader>();
            if (animator == null)
            {
                Debug.LogError("PlayerIdleAnimation requires an Animator.", this);
                enabled = false;
                return;
            }

            attackStateHash = Animator.StringToHash(attackStateName);
            throwStateHash = Animator.StringToHash(throwStateName);
            CacheAnimatorParameters();
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == attackStateName) basicAttackClipLength = clip.length;

            wasMoving = movement != null
                && Mathf.Abs(movement.MoveInput) > movementThreshold;
            if (hasMovingParameter)
                animator.SetBool(IsMovingParameter, wasMoving);
            bool positiveScale = animator.transform.localScale.x >= 0f;
            FacingDirectionX = positiveScale == spriteFacesRight ? 1f : -1f;
        }

        private void LateUpdate()
        {
            if (IsBasicAttackActive && !CanPerformCombatAction) CancelBasicAttack();
            if (playerEntity != null && playerEntity.IsDead)
                return;
            if (playerEntity != null && playerEntity.isKnockback)
            {
                if (!wasKnockedBack) FinishSkillAnimation();
                wasKnockedBack = true;
                return;
            }
            if (wasKnockedBack)
            {
                wasKnockedBack = false;
                if (!IsAttacking && movement != null && movement.IsGrounded) PlayIdle();
            }

            float horizontalInput = movement != null && !movement.IsMovementLocked && !movement.IsDashing
                ? movement.MoveInput : 0f;
            bool isMoving = Mathf.Abs(horizontalInput) > movementThreshold;
            if (!lockAttackFacing)
                ApplyFacingDirection(isMoving ? horizontalInput : FacingDirectionX, isMoving && !IsAttacking);
            if (isMoving == wasMoving) return;

            wasMoving = isMoving;
            if (hasMovingParameter)
                animator.SetBool(IsMovingParameter, isMoving);
        }

        public bool TryRequestAttack(bool lockFacing = false)
        {
            if (!CanPerformCombatAction || Time.timeScale <= 0f || IsAttacking || !hasAttackParameter)
                return false;

            IsAttacking = true;
            IsBasicAttackActive = true;
            BasicAttackSequence++;
            basicAttackChainAt = float.PositiveInfinity;
            lockAttackFacing = lockFacing;
            RefreshAttackFacing();
            animator.ResetTrigger(AttackParameter);
            animator.SetTrigger(AttackParameter);
            attackLockRoutine = StartCoroutine(WaitForAttackAnimation(attackStateHash));
            return true;
        }

        public void RefreshAttackFacing()
        {
            if (lockAttackFacing && !IsBasicAttackActive) return;
            float input = movement != null ? movement.MoveInput : 0f;
            ApplyFacingDirection(Mathf.Abs(input) > movementThreshold ? input : FacingDirectionX, false);
        }

        public void CancelBasicAttack()
        {
            if (!IsBasicAttackActive) return;
            BasicAttackSequence++;
            FinishSkillAnimation();
        }

        public bool HasReachedBasicAttackHit(float clipTime)
        {
            if (!IsBasicAttackActive || basicAttackClipLength <= 0f) return false;
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            return current.shortNameHash == attackStateHash
                && current.normalizedTime >= clipTime / basicAttackClipLength;
        }

        public void MarkBasicAttackHit(float recoveryDuration)
        {
            lockAttackFacing = true;
            // Preserve a short follow-through before a skill can replace the basic attack.
            basicAttackChainAt = Time.time + Mathf.Min(0.1f, recoveryDuration * 0.35f);
        }

        public bool TryRequestThrow()
        {
            if (!CanPerformCombatAction || Time.timeScale <= 0f || (IsAttacking && !CanChainBasicAttack))
                return false;

            string statePath = "Base Layer." + throwStateName;
            if (!animator.HasState(0, Animator.StringToHash(statePath)))
            {
                Debug.LogWarning($"Throw animation state '{statePath}' was not found.", animator);
                return false;
            }

            CancelBasicAttack();
            RefreshAttackFacing();
            IsAttacking = true;
            if (hasAttackParameter)
                animator.ResetTrigger(AttackParameter);
            animator.Play(statePath, 0, 0f);
            attackLockRoutine = StartCoroutine(WaitForAttackAnimation(throwStateHash));
            return true;
        }

        public bool TryRequestSkillAnimation(string stateName, float playbackDuration = 0f)
        {
            if (!CanPerformCombatAction || Time.timeScale <= 0f
                || (IsAttacking && !CanChainBasicAttack) || animator == null)
                return false;
            string statePath = "Base Layer." + stateName;
            if (!animator.HasState(0, Animator.StringToHash(statePath)))
                return false;

            CancelBasicAttack();
            RefreshAttackFacing();
            IsAttacking = true;
            lockAttackFacing = true;
            if (hasAttackParameter) animator.ResetTrigger(AttackParameter);
            animator.Play(statePath, 0, 0f);
            if (playbackDuration > 0f)
            {
                foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                    if (clip.name == stateName)
                    {
                        speedBeforeSkill = animator.speed;
                        skillSpeedOverridden = true;
                        animator.speed = clip.length / playbackDuration;
                        break;
                    }
            }
            attackLockRoutine = StartCoroutine(WaitForAttackAnimation(Animator.StringToHash(stateName)));
            return true;
        }

        public void FinishSkillAnimation()
        {
            if (animator != null && hasAttackParameter) animator.ResetTrigger(AttackParameter);
            if (attackLockRoutine != null) StopCoroutine(attackLockRoutine);
            attackLockRoutine = null;
            IsAttacking = false;
            IsBasicAttackActive = false;
            lockAttackFacing = false;
            RestoreSkillAnimationSpeed();
            if (playerEntity == null || !playerEntity.IsDead) PlayIdle();
        }

        private void RestoreSkillAnimationSpeed()
        {
            if (!skillSpeedOverridden) return;
            if (animator != null) animator.speed = speedBeforeSkill;
            skillSpeedOverridden = false;
        }

        public void SetJumping(bool isJumping)
        {
            if (animator != null && hasJumpingParameter)
                animator.SetBool(IsJumpingParameter, isJumping);
        }

        public void PlayDeath()
        {
            RestoreSkillAnimationSpeed();
            if (attackLockRoutine != null)
                StopCoroutine(attackLockRoutine);
            attackLockRoutine = null;
            IsAttacking = false;
            IsBasicAttackActive = false;
            lockAttackFacing = false;
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
            float entryDeadline = Time.time + 0.5f;

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
                    if (!enteredAttackState && Time.time >= entryDeadline)
                    {
                        if (hasAttackParameter) animator.ResetTrigger(AttackParameter);
                        BasicAttackSequence++;
                        break;
                    }
                    continue;
                }

                // A non-looping attack can finish without an Animator exit transition.
                // Release gameplay locks even when the controller holds its final pose.
                if (currentIsAttack && !current.loop && current.normalizedTime >= 1f)
                    break;

                if (!currentIsAttack && !nextIsAttack)
                    break;
            }

            IsAttacking = false;
            IsBasicAttackActive = false;
            lockAttackFacing = false;
            attackLockRoutine = null;
            RestoreSkillAnimationSpeed();
        }

        private void OnDisable()
        {
            RestoreSkillAnimationSpeed();
            if (attackLockRoutine != null)
                StopCoroutine(attackLockRoutine);

            if (animator != null && animator.isInitialized && hasAttackParameter)
                animator.ResetTrigger(AttackParameter);

            attackLockRoutine = null;
            IsAttacking = false;
            IsBasicAttackActive = false;
            lockAttackFacing = false;
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

        private void ApplyFacingDirection(float horizontalInput, bool isMoving)
        {
            Transform visual = animator.transform;
            Vector3 scale = visual.localScale;
            float magnitude = Mathf.Abs(scale.x);
            bool faceRight = horizontalInput > 0f;
            bool sourceFacesRight = spriteFacesRight ^ (isMoving && movingSpritesFaceOppositeDirection);
            bool usePositiveScale = faceRight == sourceFacesRight;
            scale.x = usePositiveScale ? magnitude : -magnitude;
            visual.localScale = scale;
            FacingDirectionX = faceRight ? 1f :- 1f;
        }
    }
}
