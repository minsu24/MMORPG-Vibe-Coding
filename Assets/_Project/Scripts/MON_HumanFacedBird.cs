using EasternFantasy.Player;
using EasternFantasy.Quest;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class MON_HumanFacedBird : EnemyController
{
    [Header("Rise and dive attack")]
    [SerializeField, Min(.1f)] private float attackRange = 4.5f;
    [SerializeField, Min(.1f)] private float attackVerticalRange = 2.5f;
    [SerializeField, Min(0f)] private float attackCooldown = 1.5f;
    [SerializeField, Min(0f)] private float attackLiftHeight = 2f;
    [Tooltip("Time spent rising and preparing the dive.")]
    [SerializeField, Min(.1f)] private float attackDuration = .7f;
    [SerializeField, Min(.1f)] private float diveSpeed = 13f;
    [SerializeField, Min(.1f)] private float diveDuration = .45f;
    [SerializeField, Min(0f)] private float recoveryDuration = .55f;
    [SerializeField, Min(.1f)] private float landingTimeout = 2f;
    [SerializeField] private Vector2 clawOffset = new Vector2(.9f, .05f);
    [SerializeField] private Vector2 clawSize = new Vector2(1.5f, 1.2f);
    [Header("Ground support")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Min(.01f)] private float groundCheckDistance = .12f;
    [SerializeField, Min(.1f)] private float walkAnimationReferenceSpeed = 1.4f;
    [SerializeField] private QuestDefinition questDefinition;

    private RaycastHit2D[] clawHits = new RaycastHit2D[16];
    private RaycastHit2D[] terrainHits = new RaycastHit2D[16];
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private enum AttackPhase { Rising, Diving, Landing, Recovering }
    private AttackPhase attackPhase;
    private Vector2 diveDirection;
    private float nextAttackAt, attackStartedAt, attackEndsAt, attackDirection, takeoffY, savedGravity;
    private float phaseStartedAt, savedAnimatorSpeed;
    private bool gravityChanged, clawEmitted;
    private static readonly int WalkCycleSpeed = Animator.StringToHash("walkCycleSpeed");

    public bool IsAttacking => isAttacking;
    public override float ContactDamage => 0f;
    public QuestDefinition Quest => questDefinition;
    protected override string DeathAnimationState => "HumanFacedBirdDead";
    protected override float MinimumDeathAnimationDuration => 1.1f;
    protected override bool ControlsAttackMovement => true;
    protected override bool CanUseAbility() => isAttacking || base.CanUseAbility();

    protected override void MonsterAbility()
    {
        if (isAttacking)
        {
            if (!HasLivingPlayer || !CanDealContactDamage || isKnockback || Time.time >= attackEndsAt)
                FinishAttack(HP > 0f);
            return;
        }
        if (!HasLivingPlayer || isKnockback || !CanDealContactDamage || Time.timeScale <= 0f
            || Time.time < nextAttackAt || !IsGrounded()) return;
        Vector2 delta = playerEntity.transform.position - transform.position;
        if (Mathf.Abs(delta.x) > attackRange || Mathf.Abs(delta.y) > attackVerticalRange) return;

        attackDirection = delta.x >= 0f ? 1f : -1f;
        var scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x) * attackDirection; // Clips normalize their source art to face left.
        transform.localScale = scale;
        isAttacking = true;
        clawEmitted = false;
        takeoffY = rb.position.y;
        attackStartedAt = Time.time;
        phaseStartedAt = Time.time;
        attackPhase = AttackPhase.Rising;
        attackEndsAt = Time.time + attackDuration + diveDuration + landingTimeout + recoveryDuration + .5f;
        savedGravity = rb.gravityScale;
        gravityChanged = true;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isMoving", false);
        savedAnimatorSpeed = animator.speed;
        animator.speed = .45f / attackDuration;
        animator.Play("HumanFacedBirdAttack", 0, 0f);
    }

    private ContactFilter2D TerrainFilter()
    {
        var filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayers.value & ~(_playerLayer.value | LayerMask.GetMask("Enemy", "Ignore Raycast")));
        filter.useTriggers = false;
        return filter;
    }

    private bool IsGrounded()
    {
        if (Mathf.Abs(rb.linearVelocity.y) > .3f) return false;
        int count = capsuleCollider2D.Cast(Vector2.down, TerrainFilter(), groundHits, groundCheckDistance);
        for (int i = 0; i < count; i++)
            if (groundHits[i].normal.y > .5f) return true;
        return false;
    }

    protected override void AttackFixedUpdate()
    {
        if (!isAttacking) return;
        if (!HasLivingPlayer || !CanDealContactDamage || isKnockback) { FinishAttack(HP > 0f); return; }
        float age = Time.time - phaseStartedAt;
        switch (attackPhase)
        {
            case AttackPhase.Rising:
                if (age >= attackDuration) { BeginDive(); return; }
                float lift = Mathf.SmoothStep(0f, 1f, (Time.time - attackStartedAt) / (attackDuration * .75f));
                Vector2 rise = Vector2.up * Mathf.Clamp(takeoffY + attackLiftHeight * lift - rb.position.y,
                    -8f * Time.fixedDeltaTime, 8f * Time.fixedDeltaTime);
                rb.linearVelocity = TerrainSafeStep(rise, out _) / Time.fixedDeltaTime;
                break;
            case AttackPhase.Diving:
                Vector2 desiredStep = diveDirection * (diveSpeed * Time.fixedDeltaTime);
                Vector2 step = TerrainSafeStep(desiredStep, out bool blocked);
                HumanFacedBirdClawSweep(step);
                rb.linearVelocity = step / Time.fixedDeltaTime;
                if (blocked || age >= diveDuration) BeginLanding();
                break;
            case AttackPhase.Landing:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (IsGrounded())
                {
                    attackPhase = AttackPhase.Recovering;
                    phaseStartedAt = Time.time;
                    rb.linearVelocity = Vector2.zero;
                    animator.speed = savedAnimatorSpeed;
                    animator.Play("HumanFacedBirdIdle", 0, 0f);
                }
                else if (age >= landingTimeout) FinishAttack(true);
                break;
            case AttackPhase.Recovering:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (age >= recoveryDuration) FinishAttack(true);
                break;
        }
    }

    private void BeginDive()
    {
        var targetCollider = playerEntity.GetComponent<Collider2D>();
        Vector2 aim = targetCollider != null ? (Vector2)targetCollider.bounds.center : (Vector2)playerEntity.transform.position;
        Vector2 delta = aim - (Vector2)capsuleCollider2D.bounds.center;
        diveDirection = delta.sqrMagnitude > .001f ? delta.normalized : new Vector2(attackDirection, -.5f).normalized;
        attackDirection = diveDirection.x >= 0f ? 1f : -1f;
        var scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x) * attackDirection;
        transform.localScale = scale;
        attackPhase = AttackPhase.Diving;
        phaseStartedAt = Time.time;
        animator.Play("HumanFacedBirdAttack", 0, .5f);
        animator.speed = 0f;
        EnemyAttackAudio.PlayFor(this, "Audio/HumanFacedBirdDive", .35f);
    }

    private void BeginLanding()
    {
        attackPhase = AttackPhase.Landing;
        phaseStartedAt = Time.time;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = savedGravity;
        animator.Play("HumanFacedBirdAttack", 0, .875f);
    }

    private Vector2 TerrainSafeStep(Vector2 step, out bool blocked)
    {
        blocked = false;
        float distance = step.magnitude;
        if (distance <= .0001f) return Vector2.zero;
        int count;
        do
        {
            count = capsuleCollider2D.Cast(step / distance, TerrainFilter(), terrainHits, distance + .02f);
            if (count == terrainHits.Length) System.Array.Resize(ref terrainHits, terrainHits.Length * 2);
            else break;
        } while (true);
        float allowed = distance;
        for (int i = 0; i < count; i++)
        {
            var hit = terrainHits[i];
            if (hit.collider == null || Vector2.Dot(hit.normal, step / distance) >= -.01f) continue;
            allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - .02f));
        }
        blocked = allowed < distance;
        return step / distance * allowed;
    }

    // Animation event on the first frame with extended attacking talons.
    public void HumanFacedBirdClawHit()
    {
        HumanFacedBirdClawSweep(Vector2.zero);
    }

    private void HumanFacedBirdClawSweep(Vector2 step)
    {
        if (attackPhase != AttackPhase.Diving) return;
        if (!isAttacking || clawEmitted || isKnockback || !CanDealContactDamage || !HasLivingPlayer) return;
        Vector2 scale = new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        Vector2 center = rb.position + Vector2.Scale(new Vector2(clawOffset.x * attackDirection, clawOffset.y), scale);
        var filter = new ContactFilter2D(); filter.SetLayerMask(_playerLayer); filter.useTriggers = false;
        int count;
        do
        {
            count = gameObject.scene.GetPhysicsScene2D().BoxCast(center, Vector2.Scale(clawSize, scale), 0f,
                step.sqrMagnitude > .0001f ? step.normalized : Vector2.down, step.magnitude, filter, clawHits);
            if (count == clawHits.Length) System.Array.Resize(ref clawHits, clawHits.Length * 2);
            else break;
        } while (true);
        for (int i = 0; i < count; i++)
        {
            var victim = clawHits[i].collider.GetComponentInParent<PlayerEntity>();
            if (victim != playerEntity || victim.IsDead) continue;
            Vector2 sightline = (Vector2)clawHits[i].collider.bounds.center - (Vector2)capsuleCollider2D.bounds.center;
            if (gameObject.scene.GetPhysicsScene2D().Raycast(capsuleCollider2D.bounds.center, sightline.normalized,
                sightline.magnitude, TerrainFilter(), terrainHits) > 0) continue;
            clawEmitted = true;
            uint before = victim.HitReactionVersion;
            victim.TakeDamage(Attack_Power);
            if (!victim.IsDead && victim.HitReactionVersion != before) victim.ApplyKnockback(attackDirection);
            break;
        }
    }

    public void HumanFacedBirdAttackFinished()
    {
        // Legacy animation events cannot end the physics-driven dive before it lands.
    }

    private void FinishAttack(bool returnToIdle)
    {
        bool wasAttacking = isAttacking;
        isAttacking = false;
        clawEmitted = true;
        if (gravityChanged && rb != null) { rb.gravityScale = savedGravity; gravityChanged = false; }
        if (!wasAttacking) return;
        if (animator != null) animator.speed = savedAnimatorSpeed;
        nextAttackAt = Time.time + attackCooldown;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (returnToIdle && animator != null && animator.isActiveAndEnabled) animator.Play("HumanFacedBirdIdle", 0, 0f);
    }

    private void LateUpdate()
    {
        if (animator == null || rb == null || HP <= 0f) return;
        animator.SetFloat(WalkCycleSpeed, Mathf.Clamp(Mathf.Abs(rb.linearVelocity.x) / walkAnimationReferenceSpeed, .2f, 2.5f));
        if (isAttacking) animator.SetBool("isMoving", false);
    }

    public override void ApplyKnockback(float directionX)
    {
        if (!isAttacking)
            base.ApplyKnockback(directionX);
    }
    protected override void OnDefeated()
    {
        if (questDefinition != null) QuestManager.Instance?.AddProgress(questDefinition);
        FinishAttack(false);
        if (animator != null) animator.SetBool("isMoving", false);
        base.OnDefeated();
    }
    protected override void OnDisable()
    {
        FinishAttack(false);
        base.OnDisable();
    }
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        float direction = Application.isPlaying && isAttacking ? attackDirection : transform.lossyScale.x > 0 ? -1f : 1f;
        var scale = new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube((Vector2)transform.position + Vector2.Scale(new Vector2(clawOffset.x * direction, clawOffset.y), scale), Vector2.Scale(clawSize, scale));
    }
}
