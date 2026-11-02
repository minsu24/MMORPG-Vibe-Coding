using UnityEngine;

public partial class MON_Spider
{
    [Header("Ranged basic attack")]
    [SerializeField, Min(.5f)] private float spitAttackRange = 5f;
    [SerializeField, Min(.1f)] private float spitCooldown = 1.5f;
    [SerializeField, Min(.1f)] private float projectileSpeed = 7f;
    [SerializeField] private SpiderSpitProjectile spitProjectilePrefab;

    public bool IsSpitting { get; private set; }
    protected override bool ControlsAttackMovement => true;
    protected override string DeathAnimationState => "SpiderDead";
    protected override bool CanUseAbility() => isAttacking || base.CanUseAbility();
    private float spitElapsed, nextSpitTime, spitDirection;
    private bool projectileEmitted;
    private Vector2 aimedPosition;

    private bool WithinSpitRange() => HasLivingPlayer &&
        ((Vector2)playerEntity.transform.position - rb.position).sqrMagnitude <= spitAttackRange * spitAttackRange;

    private void TryBeginSpitAttack()
    {
        if (isAttacking || Time.timeScale <= 0f || isKnockback || !CanDealContactDamage
            || !CanDetectLivingPlayer() || !WithinSpitRange() || spitProjectilePrefab == null) return;
        // Hold position during the cooldown instead of walking into the target.
        isAttacking = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        if (Time.time >= nextSpitTime) BeginSpitting();
    }

    private void BeginSpitting()
    {
        IsSpitting = true;
        projectileEmitted = false;
        spitElapsed = 0f;
        aimedPosition = playerEntity.GetComponent<Collider2D>().bounds.center;
        spitDirection = aimedPosition.x >= rb.position.x ? 1f : -1f;
        var scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x) * spitDirection;
        transform.localScale = scale;
        animator.Play("Spider_Attack", 0, 0f);
        animator.Update(0f);
    }

    protected override void AttackFixedUpdate()
    {
        if (!HasLivingPlayer || !CanDealContactDamage || !CanDetectLivingPlayer())
        { FinishSpitAttack(true); return; }
        if (!IsSpitting)
        {
            if (!WithinSpitRange()) { FinishSpitAttack(true); return; }
            if (Time.time >= nextSpitTime) BeginSpitting();
        }
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        if (!IsSpitting) return;
        spitElapsed += Time.fixedDeltaTime;
        if (spitElapsed >= .88f)
        {
            IsSpitting = false;
            nextSpitTime = Time.time + spitCooldown;
            animator.Play("SpiderIdle", 0, 0f);
        }
    }

    // Frame 5 is the fully opened mouth in the supplied eight-frame sheet.
    public void EmitSpiderProjectile()
    {
        if (!IsSpitting || projectileEmitted || !CanDealContactDamage || !HasLivingPlayer
            || !WithinSpitRange() || spitProjectilePrefab == null) return;
        projectileEmitted = true;
        Bounds bounds = capsuleCollider2D.bounds;
        Vector2 mouth = new Vector2(bounds.center.x + spitDirection * (bounds.extents.x + .08f),
            bounds.center.y + bounds.size.y * .08f);
        var projectile = Instantiate(spitProjectilePrefab, mouth, Quaternion.identity);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(projectile.gameObject, gameObject.scene);
        projectile.Launch(aimedPosition - mouth, projectileSpeed, spitAttackRange + 2f, Attack_Power, this);
        EnemyAttackAudio.PlayFor(this, "Audio/SpiderAttack", .27f);
    }

    private void FinishSpitAttack(bool returnToIdle)
    {
        bool wasSpitting = IsSpitting;
        isAttacking = false;
        IsSpitting = false;
        if (wasSpitting) nextSpitTime = Time.time + spitCooldown;
        if (rb != null && wasSpitting) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        if (returnToIdle && HP > 0 && animator != null && animator.isActiveAndEnabled)
            animator.Play("SpiderIdle", 0, 0f);
    }

    public override void ApplyKnockback(float directionX)
    {
        if (isAttacking) FinishSpitAttack(true);
        base.ApplyKnockback(directionX);
    }
}
