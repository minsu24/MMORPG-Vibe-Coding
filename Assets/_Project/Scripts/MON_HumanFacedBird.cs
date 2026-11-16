using EasternFantasy.Player;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class MON_HumanFacedBird : EnemyController
{
    [Header("Airborne claw attack")]
    [SerializeField, Min(.1f)] private float attackRange = 1.8f;
    [SerializeField, Min(.1f)] private float attackVerticalRange = 1.3f;
    [SerializeField, Min(0f)] private float attackCooldown = 1.5f;
    [SerializeField, Min(0f)] private float attackLiftHeight = .65f;
    [SerializeField, Min(.1f)] private float attackDuration = 1f;
    [SerializeField] private Vector2 clawOffset = new Vector2(.9f, .05f);
    [SerializeField] private Vector2 clawSize = new Vector2(1.5f, 1.2f);
    [Header("Ground support")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Min(.01f)] private float groundCheckDistance = .12f;
    [SerializeField, Min(.1f)] private float walkAnimationReferenceSpeed = 1.4f;

    private readonly Collider2D[] clawHits = new Collider2D[16];
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private float nextAttackAt, attackStartedAt, attackEndsAt, attackDirection, takeoffY, savedGravity;
    private bool gravityChanged, clawEmitted;
    private static readonly int WalkCycleSpeed = Animator.StringToHash("walkCycleSpeed");

    public bool IsAttacking => isAttacking;
    public override float ContactDamage => 0f;
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
        attackEndsAt = Time.time + attackDuration + .25f;
        savedGravity = rb.gravityScale;
        gravityChanged = true;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isMoving", false);
        animator.Play("HumanFacedBirdAttack", 0, 0f);
    }

    private bool IsGrounded()
    {
        if (Mathf.Abs(rb.linearVelocity.y) > .3f) return false;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayers.value & ~(_playerLayer.value | LayerMask.GetMask("Enemy", "Ignore Raycast")));
        filter.useTriggers = false;
        int count = capsuleCollider2D.Cast(Vector2.down, filter, groundHits, groundCheckDistance);
        for (int i = 0; i < count; i++)
            if (groundHits[i].normal.y > .5f) return true;
        return false;
    }

    protected override void AttackFixedUpdate()
    {
        if (!isAttacking) return;
        if (!HasLivingPlayer || !CanDealContactDamage) { FinishAttack(HP > 0f); return; }
        float phase = Mathf.Clamp01((Time.time - attackStartedAt) / attackDuration);
        float lift = phase < .35f ? Mathf.SmoothStep(0f, 1f, phase / .35f)
            : phase < .7f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (phase - .7f) / .3f);
        float desiredY = takeoffY + attackLiftHeight * lift;
        // Physics resolves ceilings, floors and walls throughout takeoff and landing.
        rb.linearVelocity = new Vector2(0f, Mathf.Clamp((desiredY - rb.position.y) / Time.fixedDeltaTime, -8f, 8f));
    }

    // Animation event on the first frame with extended attacking talons.
    public void HumanFacedBirdClawHit()
    {
        if (!isAttacking || clawEmitted || isKnockback || !CanDealContactDamage || !HasLivingPlayer) return;
        clawEmitted = true;
        Vector2 scale = new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        Vector2 center = rb.position + Vector2.Scale(new Vector2(clawOffset.x * attackDirection, clawOffset.y), scale);
        var filter = new ContactFilter2D(); filter.SetLayerMask(_playerLayer); filter.useTriggers = false;
        int count = gameObject.scene.GetPhysicsScene2D().OverlapBox(center, Vector2.Scale(clawSize, scale), 0f, filter, clawHits);
        for (int i = 0; i < count; i++)
        {
            var victim = clawHits[i].GetComponentInParent<PlayerEntity>();
            if (victim != playerEntity || victim.IsDead) continue;
            uint before = victim.HitReactionVersion;
            victim.TakeDamage(Attack_Power);
            if (!victim.IsDead && victim.HitReactionVersion != before) victim.ApplyKnockback(attackDirection);
            break;
        }
    }

    public void HumanFacedBirdAttackFinished()
    {
        if (isAttacking) FinishAttack(HP > 0f);
    }

    private void FinishAttack(bool returnToIdle)
    {
        bool wasAttacking = isAttacking;
        isAttacking = false;
        clawEmitted = true;
        if (gravityChanged && rb != null) { rb.gravityScale = savedGravity; gravityChanged = false; }
        if (!wasAttacking) return;
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
        FinishAttack(true);
        base.ApplyKnockback(directionX);
    }
    protected override void OnDefeated()
    {
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
