using EasternFantasy.Inventory;
using UnityEngine;
using EasternFantasy.Skill;

namespace EasternFantasy.Player
{
    // No Entity or collider: the companion has no health, cannot be attacked and does not block movement.
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class PetCompanionActor : MonoBehaviour
    {
        [Header("Companion Catalog")]
        [SerializeField] private string companionId = "goblin";
        [SerializeField] private string displayName = "도깨비";
        [SerializeField, TextArea] private string description = "장난을 좋아하지만 의리가 깊은\n작은 도깨비. 인연을 맺은 자를\n스스로의 친구로 여긴다.";
        [SerializeField] private ItemDefinition summonItem;
        [SerializeField] private Sprite[] previewFrames;
        [SerializeField, Min(.01f)] private float previewFrameRate = 5f;

        [Header("Active Skill")]
        [SerializeField] private SkillDefinition activeSkill;
        [SerializeField] private float skillDamageMultiplier = 5f;
        [SerializeField] private float growDuration = .72f, skillStrikeDuration = .72f, shrinkDuration = .72f;
        [SerializeField] private float skillHitTime = .5f;
        private enum SkillPhase { None, Approaching, Growing, Striking, Shrinking }
        private SkillPhase skillPhase;
        private EnemyController skillTarget;
        private float skillElapsed, skillDamage;
        private bool skillHitApplied;
        private PlayerSkillSystem skillSystem;
        public SkillDefinition ActiveSkill => activeSkill;
        public bool IsUsingActiveSkill => skillPhase != SkillPhase.None;

        [Header("Combat Cycle")]
        [SerializeField, Min(.01f)] private float attackInterval = 4f;
        [SerializeField, Min(.01f)] private float attackDuration = .8f;
        [SerializeField, Min(0f)] private float attackHitTime = .6f;
        [SerializeField, Min(.01f)] private float approachSpeed = 10f;
        [SerializeField, Min(.01f)] private float returnSpeed = 12f;
        [SerializeField, Min(.01f)] private float attackRange = 1.1f;
        [SerializeField, Min(0f)] private float attackStandOff = .65f;
        [SerializeField, Min(.01f)] private float followStartDistance = .5f;
        [SerializeField, Min(.01f)] private float arrivalDistance = .1f;
        [SerializeField, Min(.01f)] private float followSmoothTime = .06f;
        [SerializeField] private Vector2 followOffset = new Vector2(1.1f, 0f);
        [SerializeField, Min(.01f)] private float referenceVisualHeight = 1.2514286f;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerEntity owner;

        private PetCompanionProgress progress;
        private PlayerIdleAnimation ownerAnimation;
        private PlayerMovement2D movement;
        private float ownerGroundOffset = -.5f;
        private Vector3 followVelocity;
        private bool walking;
        private EnemyController target, attackTarget, approachTarget;
        private float approachSide = -1f, attackFacing = 1f;
        private PetCompanionCombatCycle cycle;
        private bool subscribed;
        private float facing = 1f;
        private string animationState;

        public string CompanionId => companionId;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemDefinition SummonItem => summonItem;
        public Sprite[] PreviewFrames => previewFrames;
        public float PreviewFrameRate => previewFrameRate;
        public PetCompanionCombatState CombatState => cycle != null ? cycle.State : PetCompanionCombatState.Waiting;
        public EnemyController SelectedTarget => target;
        private float WorldHeight => referenceVisualHeight * Mathf.Abs(transform.lossyScale.y);

        private void Awake()
        {
            if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        public void Initialize(PlayerEntity player, PetCompanionProgress state)
        {
            Unsubscribe();
            owner = player;
            progress = state;
            skillSystem = player.GetComponent<PlayerSkillSystem>();
            if (skillSystem != null)
            {
                skillSystem.SetCompanionSkillUnlocked(activeSkill, state.IsSummoned);
                skillSystem.BindCompanion(this);
            }
            skillPhase = SkillPhase.None;
            ownerAnimation = player.GetComponent<PlayerIdleAnimation>();
            movement = player.GetComponent<PlayerMovement2D>();
            CacheOwnerGroundOffset();
            followVelocity = Vector3.zero;
            cycle = new PetCompanionCombatCycle(attackInterval, attackDuration, attackHitTime);
            target = attackTarget = null;
            var playerSprite = player.GetComponentInChildren<SpriteRenderer>();
            if (playerSprite != null && visual != null)
            {
                visual.sortingLayerID = playerSprite.sortingLayerID;
                visual.sortingOrder = playerSprite.sortingOrder + 1;
            }
            transform.position = OwnerPosition();
            Subscribe();
            SelectTarget(player.LastAttackTarget);
            PlayAnimation("Companion_Idle");
        }

        public void BindProgress(PetCompanionProgress state) => progress = state;

        private void Subscribe()
        {
            if (owner == null || subscribed) return;
            owner.AttackTargetHit += SelectTarget;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (owner != null && subscribed) owner.AttackTargetHit -= SelectTarget;
            subscribed = false;
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();
        private void OnDestroy()
        {
            Unsubscribe();
            if (skillSystem != null) skillSystem.UnbindCompanion(this);
        }

        private void SelectTarget(EnemyController enemy)
        {
            // The persistent player lives in DontDestroyOnLoad; enemies live in this companion's gameplay scene.
            if (owner == null || !owner.IsInCombat || !IsLiving(enemy)
                || enemy.gameObject.scene != gameObject.scene) return;
            target = enemy;
        }

        public string GetActiveSkillUnavailableReason()
        {
            if (!isActiveAndEnabled || progress == null || !progress.IsEquipped) return "도깨비를 장착해 주세요";
            if (owner == null || owner.IsDead || Time.timeScale <= 0f) return "지금 사용할 수 없습니다";
            if (IsUsingActiveSkill) return "도깨비가 기술을 시전 중입니다";
            var enemy = owner.LastAttackTarget;
            if (!owner.IsInCombat || !IsLiving(enemy) || enemy.gameObject.scene != gameObject.scene)
                return "전투 중인 대상이 필요합니다";
            return string.Empty;
        }

        public bool TryUseActiveSkill(SkillDefinition skill)
        {
            if (skill == null || skill != activeSkill || !string.IsNullOrEmpty(GetActiveSkillUnavailableReason())) return false;
            skillTarget = owner.LastAttackTarget;
            target = skillTarget;
            approachSide = transform.position.x <= skillTarget.transform.position.x ? -1f : 1f;
            attackFacing = -approachSide;
            skillDamage = progress.Attack * skillDamageMultiplier;
            skillHitApplied = false;
            skillElapsed = 0f;
            followVelocity = Vector3.zero;
            cycle.CancelAttack();
            skillPhase = SkillPhase.Approaching;
            return true;
        }

        private void AdvanceActiveSkill()
        {
            cycle.Tick(Time.deltaTime, false, false, false);
            bool valid = owner.IsInCombat && IsLiving(skillTarget) && skillTarget.gameObject.scene == gameObject.scene;
            if (!valid && skillPhase != SkillPhase.Shrinking
                && !(skillPhase == SkillPhase.Striking && skillHitApplied))
            {
                if (skillPhase == SkillPhase.Approaching) { FinishActiveSkill(); return; }
                // Keep a partially grown body continuous when cancelling before impact.
                float grown = skillPhase == SkillPhase.Growing ? Mathf.Clamp01(skillElapsed / growDuration) : 1f;
                skillPhase = SkillPhase.Shrinking;
                skillElapsed = (1f - grown) * shrinkDuration;
                PlayAnimation("Companion_Shrink");
                if (animator != null) animator.Play("Companion_Shrink", 0, 1f - grown);
                return;
            }
            if (skillPhase == SkillPhase.Approaching)
            {
                MoveTo(TargetPosition(skillTarget), approachSpeed);
                PlayAnimation("Companion_Walk");
                if (Vector2.Distance(transform.position, TargetPosition(skillTarget)) > arrivalDistance) return;
                skillPhase = SkillPhase.Growing; skillElapsed = 0f;
                visual.flipX = attackFacing < 0f;
                PlayAnimation("Companion_Grow");
                return;
            }
            if (valid && skillPhase != SkillPhase.Shrinking) MoveTo(TargetPosition(skillTarget), approachSpeed, false);
            visual.flipX = attackFacing < 0f;
            skillElapsed += Time.deltaTime;
            if (skillPhase == SkillPhase.Growing && skillElapsed >= growDuration)
            {
                skillPhase = SkillPhase.Striking; skillElapsed = 0f;
                PlayAnimation("Companion_SkillStrike");
            }
            else if (skillPhase == SkillPhase.Striking)
            {
                if (!skillHitApplied && skillElapsed >= skillHitTime)
                {
                    skillHitApplied = true;
                    if (valid && InAttackRange(skillTarget)) skillTarget.TakeDamage(skillDamage);
                }
                if (skillElapsed >= skillStrikeDuration)
                {
                    skillPhase = SkillPhase.Shrinking; skillElapsed = 0f;
                    PlayAnimation("Companion_Shrink");
                }
            }
            else if (skillPhase == SkillPhase.Shrinking && skillElapsed >= shrinkDuration) FinishActiveSkill();
        }

        private void FinishActiveSkill()
        {
            skillPhase = SkillPhase.None;
            skillTarget = null;
            cycle.CancelAttack();
            // A skill completes with a return, before ordinary attacks resume.
            returningAfterSkill = true;
            PlayAnimation("Companion_Walk");
        }
        private bool returningAfterSkill;

        private static bool IsLiving(EnemyController enemy) => enemy != null && enemy.isActiveAndEnabled && enemy.HP > 0f;

        private Vector3 OwnerPosition()
        {
            if (ownerAnimation != null && Mathf.Abs(ownerAnimation.FacingDirectionX) > .01f)
                facing = Mathf.Sign(ownerAnimation.FacingDirectionX);
            else if (movement != null && Mathf.Abs(movement.MoveInput) > .01f)
                facing = Mathf.Sign(movement.MoveInput);
            // Use the interpolated player position on both axes, with a stable foot offset.
            float ground = owner.transform.position.y + ownerGroundOffset;
            return new Vector3(owner.transform.position.x - facing * followOffset.x,
                ground + WorldHeight * .5f + followOffset.y, owner.transform.position.z);
        }

        private Vector3 TargetPosition(EnemyController enemy)
        {
            var collider = enemy.GetComponent<Collider2D>();
            float ground = collider != null ? collider.bounds.min.y : enemy.transform.position.y - WorldHeight * .5f;
            return new Vector3(enemy.transform.position.x + approachSide * attackStandOff,
                ground + WorldHeight * .5f, transform.position.z);
        }

        private bool InAttackRange(EnemyController enemy)
        {
            if (!IsLiving(enemy)) return false;
            var collider = enemy.GetComponent<Collider2D>();
            Vector2 point = collider != null ? collider.ClosestPoint(transform.position) : (Vector2)enemy.transform.position;
            return Vector2.Distance(transform.position, point) <= attackRange;
        }

        private void CacheOwnerGroundOffset()
        {
            var collider = owner.GetComponent<Collider2D>();
            ownerGroundOffset = collider != null ? collider.bounds.min.y - owner.transform.position.y : -.5f;
        }

        private void LateUpdate()
        {
            if (owner == null) { Destroy(gameObject); return; }
            if (visual == null || progress == null) return;
            visual.enabled = progress.IsEquipped && !owner.IsDead;
            if (!visual.enabled)
            {
                target = attackTarget = null;
                cycle = null;
                skillPhase = SkillPhase.None;
                returningAfterSkill = false;
                return;
            }
            if (Time.deltaTime <= 0f) return;
            if (cycle == null)
            {
                ownerAnimation = owner.GetComponent<PlayerIdleAnimation>();
                movement = owner.GetComponent<PlayerMovement2D>();
                CacheOwnerGroundOffset();
                followVelocity = Vector3.zero;
                cycle = new PetCompanionCombatCycle(attackInterval, attackDuration, attackHitTime);
                Subscribe();
            }
            if (IsUsingActiveSkill) { AdvanceActiveSkill(); return; }
            if (returningAfterSkill)
            {
                var homePosition = OwnerPosition();
                MoveTo(homePosition, returnSpeed);
                PlayAnimation("Companion_Walk");
                if (Vector2.Distance(transform.position, homePosition) <= arrivalDistance) returningAfterSkill = false;
                return;
            }
            if (!IsLiving(target) || target.gameObject.scene != gameObject.scene) target = null;
            if (!owner.IsInCombat || owner.LastAttackTarget == null)
            {
                target = attackTarget = approachTarget = null;
                cycle.CancelAttack();
            }
            Vector3 home = OwnerPosition();
            if (target != null && cycle.State != PetCompanionCombatState.Attacking
                && (cycle.State == PetCompanionCombatState.Waiting || approachTarget != target))
            {
                // Fix the side for this approach instead of switching destinations when the enemy crosses us.
                approachSide = transform.position.x <= target.transform.position.x ? -1f : 1f;
                approachTarget = target;
            }
            var previousState = cycle.State;
            var activeTarget = previousState == PetCompanionCombatState.Attacking ? attackTarget : target;
            // Evaluate arrival and impact using this frame's position, not the previous frame's.
            // Otherwise a moving destination can stay outside the small arrival tolerance forever.
            Vector3 previousPosition = transform.position;
            bool movedBeforeTransition = false;
            if (previousState == PetCompanionCombatState.Waiting)
            {
                transform.position = Vector3.SmoothDamp(transform.position, home, ref followVelocity,
                    followSmoothTime, returnSpeed, Time.deltaTime);
                movedBeforeTransition = true;
            }
            else followVelocity = Vector3.zero;
            if (previousState == PetCompanionCombatState.Approaching && IsLiving(target))
            {
                MoveTo(TargetPosition(target), approachSpeed);
                movedBeforeTransition = true;
            }
            else if (previousState == PetCompanionCombatState.Returning)
            {
                MoveTo(home, returnSpeed);
                movedBeforeTransition = true;
            }
            else if (previousState == PetCompanionCombatState.Attacking && IsLiving(attackTarget))
            {
                MoveTo(TargetPosition(attackTarget), approachSpeed, false);
                movedBeforeTransition = true;
            }
            bool nearTarget = IsLiving(activeTarget)
                && Vector2.Distance(transform.position, TargetPosition(activeTarget)) <= arrivalDistance;
            float homeDistance = Vector2.Distance(transform.position, home);
            // Normal following remains continuous. Only combat returns require exact arrival.
            bool readyToAttack = previousState != PetCompanionCombatState.Waiting || homeDistance <= followStartDistance;
            bool hit = cycle.Tick(Time.deltaTime, owner.IsInCombat && IsLiving(activeTarget) && readyToAttack, nearTarget,
                previousState == PetCompanionCombatState.Waiting || homeDistance <= arrivalDistance);
            if (previousState != PetCompanionCombatState.Attacking && cycle.State == PetCompanionCombatState.Attacking)
            {
                attackTarget = target;
                attackFacing = -approachSide;
            }
            if (hit && IsLiving(attackTarget) && InAttackRange(attackTarget))
                attackTarget.TakeDamage(progress.Attack);

            if (cycle.State == PetCompanionCombatState.Approaching && IsLiving(target))
            {
                if (!movedBeforeTransition || previousState == PetCompanionCombatState.Waiting) MoveTo(TargetPosition(target), approachSpeed);
                PlayAnimation("Companion_Walk");
            }
            else if (cycle.State == PetCompanionCombatState.Returning)
            {
                if (!movedBeforeTransition) MoveTo(home, returnSpeed);
                PlayAnimation("Companion_Walk");
            }
            else if (cycle.State == PetCompanionCombatState.Attacking)
            {
                // Follow a moving target through the wind-up without changing the locked strike direction.
                if (!movedBeforeTransition && IsLiving(attackTarget)) MoveTo(TargetPosition(attackTarget), approachSpeed, false);
                visual.flipX = attackFacing < 0f;
                PlayAnimation("Companion_Attack");
            }
            else
            {
                float speed = Vector3.Distance(transform.position, previousPosition) / Time.deltaTime;
                if (speed > .2f) walking = true;
                else if (speed < .05f) walking = false;
                float horizontalStep = transform.position.x - previousPosition.x;
                visual.flipX = walking && Mathf.Abs(horizontalStep) > .001f ? horizontalStep < 0f : facing < 0f;
                PlayAnimation(walking ? "Companion_Walk" : "Companion_Idle");
            }
        }

        private void MoveTo(Vector3 destination, float speed, bool updateFacing = true)
        {
            if (updateFacing && Mathf.Abs(destination.x - transform.position.x) > .05f)
                visual.flipX = destination.x < transform.position.x;
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        }

        private void PlayAnimation(string state)
        {
            if (animator == null || animationState == state) return;
            animator.Play(state, 0, 0f);
            animationState = state;
        }
    }
}

