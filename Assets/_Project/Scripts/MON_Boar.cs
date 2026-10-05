using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class MON_Boar : EnemyController
{
    public enum ChargePhase { None, Warning, Charging }

    [Header("Charge Attack")]
    [SerializeField, Min(0.1f)] private float warningDuration = 2.2f;
    [SerializeField, Min(1f)] private float chargeSpeed = 32f;
    [SerializeField, Min(1f)] private float chargeRange = 25f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 2.5f;
    [SerializeField] private Collider2D arenaBounds;

    public ChargePhase CurrentPhase { get; private set; }
    public override float ContactDamage => CurrentPhase == ChargePhase.None ? base.ContactDamage : 0f;
    protected override bool ControlsAttackMovement => true;
    protected override string DeathAnimationState => "BoarDead";

    private float nextAttackTime, phaseEnds, chargeDirection, chargeEndX;
    private bool chargeHit;
    private GiantAttackVisual warning;
    private readonly List<Collider2D> ignoredPlayerColliders = new List<Collider2D>();

    private void Start() => nextAttackTime = Time.time + attackCooldown;

    protected override bool CanUseAbility() => CurrentPhase != ChargePhase.None || base.CanUseAbility();

    protected override void MonsterAbility()
    {
        if (Time.timeScale <= 0f) return;
        if (CurrentPhase != ChargePhase.None)
        {
            if (!HasLivingPlayer || HP <= 0f) { FinishCharge(); return; }
            if (CurrentPhase == ChargePhase.Warning && Time.time >= phaseEnds)
            {
                ClearWarning();
                CurrentPhase = ChargePhase.Charging;
                animator.Play("BoarCharge", 0, 0f);
                phaseEnds = Time.time + Mathf.Abs(chargeEndX - rb.position.x) / chargeSpeed + 0.2f;
            }
            else if (Time.time >= phaseEnds) FinishCharge();
            return;
        }
        if (Time.time < nextAttackTime || isKnockback || !CanDetectLivingPlayer()
            || Mathf.Abs(rb.linearVelocity.y) > 0.3f) return;
        BeginCharge();
    }

    private void BeginCharge()
    {
        // Commit the direction once; crossing behind the boar during warning is safe.
        chargeDirection = playerEntity.transform.position.x >= rb.position.x ? 1f : -1f;
        Vector3 scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x) * chargeDirection;
        transform.localScale = scale;
        chargeEndX = rb.position.x + chargeDirection * chargeRange;
        if (arenaBounds != null)
            chargeEndX = Mathf.Clamp(chargeEndX,
                arenaBounds.bounds.min.x + capsuleCollider2D.bounds.extents.x,
                arenaBounds.bounds.max.x - capsuleCollider2D.bounds.extents.x);
        chargeHit = false;
        isAttacking = true;
        CurrentPhase = ChargePhase.Warning;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        animator.Play("BoarIdle", 0, 0f);
        foreach (Collider2D c in playerEntity.GetComponentsInChildren<Collider2D>())
        {
            if (c.enabled && !Physics2D.GetIgnoreCollision(capsuleCollider2D, c))
            {
                Physics2D.IgnoreCollision(capsuleCollider2D, c, true);
                ignoredPlayerColliders.Add(c);
            }
        }
        Bounds body = capsuleCollider2D.bounds;
        warning = GiantAttackVisual.CreateWarning(
            new Vector2((body.center.x + chargeEndX) * 0.5f, body.center.y),
            new Vector2(Mathf.Abs(chargeEndX - body.center.x) + body.size.x, body.size.y),
            chargeDirection, spriteRenderer);
        warning.gameObject.name = "Boar Charge Warning";
        phaseEnds = Time.time + warningDuration;
    }

    protected override void AttackFixedUpdate()
    {
        if (CurrentPhase != ChargePhase.Charging)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        float remaining = (chargeEndX - rb.position.x) * chargeDirection;
        if (remaining <= 0.05f) { FinishCharge(); return; }
        float step = Mathf.Min(chargeSpeed * Time.fixedDeltaTime, remaining);
        Bounds body = capsuleCollider2D.bounds;
        // Sweep the body over the entire step, as with the giant's sprint attack.
        Vector2 center = (Vector2)body.center + Vector2.right * chargeDirection * step * 0.5f;
        foreach (Collider2D c in Physics2D.OverlapBoxAll(center,
            new Vector2(body.size.x + step, body.size.y), 0f, _playerLayer))
        {
            PlayerEntity entity = c.GetComponentInParent<PlayerEntity>();
            if (entity == null || chargeHit) continue;
            chargeHit = true;
            float before = entity.HP;
            entity.TakeDamage(Attack_Power);
            if (!entity.IsDead && entity.HP < before) entity.ApplyKnockback(chargeDirection);
        }
        rb.linearVelocity = new Vector2(chargeDirection * step / Time.fixedDeltaTime, rb.linearVelocity.y);
    }

    public override void ApplyKnockback(float directionX)
    {
        if (CurrentPhase == ChargePhase.None) base.ApplyKnockback(directionX);
    }

    private void ClearWarning()
    {
        if (warning != null) Destroy(warning.gameObject);
        warning = null;
    }

    private void FinishCharge()
    {
        foreach (Collider2D c in ignoredPlayerColliders)
            if (c != null && capsuleCollider2D != null)
                Physics2D.IgnoreCollision(capsuleCollider2D, c, false);
        ignoredPlayerColliders.Clear();
        ClearWarning();
        CurrentPhase = ChargePhase.None;
        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (HP > 0f && animator != null && animator.isActiveAndEnabled)
            animator.Play("BoarIdle", 0, 0f);
    }

    protected override void OnDefeated()
    {
        FinishCharge();
        base.OnDefeated();
    }

    protected override void OnDisable()
    {
        if (CurrentPhase != ChargePhase.None) FinishCharge();
        base.OnDisable();
    }
}
