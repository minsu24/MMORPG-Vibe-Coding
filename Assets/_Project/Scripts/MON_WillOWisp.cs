using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed partial class MON_WillOWisp : EnemyController
{
    [Header("Three flame volley")]
    [SerializeField] private WillOWispProjectile projectilePrefab;
    [SerializeField, Min(.1f)] private float attackRange = 6f;
    [SerializeField, Min(0f)] private float attackCooldown = 2f;
    [SerializeField, Min(.1f)] private float attackDuration = 1.333333f;
    [SerializeField, Min(.1f)] private float deathDuration = 1.533333f;
    [SerializeField, Min(.1f)] private float destinationSpacing = 1.25f;
    [SerializeField, Min(.1f)] private float flightDuration = 1.1f;
    [SerializeField, Min(.1f)] private float arcHeight = 1.6f;
    [SerializeField] private Vector2 mouthOffset = new Vector2(.48f, -.4f);

    private static readonly int Attacking = Animator.StringToHash("isAttacking");
    private float nextAttackAt, attackEndsAt, attackDirection;
    private Vector2 aimedPosition;
    private bool volleyEmitted;
    private bool provoked;

    public bool IsAttacking => isAttacking;
    public bool IsProvoked => provoked;
    public override float ContactDamage => 0f;
    protected override float MinimumDeathAnimationDuration => deathDuration;
    protected override bool CanUseAbility() => HP > 0f && (isAttacking || (provoked && HasLivingPlayer));

    protected override void OnDamageReceived(float damage)
    {
        if (HP <= 0f || provoked) return;
        provoked = true;
        nextAttackAt = Time.time;
        StopBlink();
    }

    protected override void MonsterAbility()
    {
        if (isAttacking)
        {
            if (!HasLivingPlayer || isKnockback || Time.time >= attackEndsAt)
                FinishWispAttack();
            return;
        }
        if (!provoked || !HasLivingPlayer || isKnockback || !CanDealContactDamage
            || Time.timeScale <= 0f || Time.time < nextAttackAt || projectilePrefab == null) return;
        var targetCollider = playerEntity.GetComponent<Collider2D>();
        Vector2 aim = targetCollider != null ? (Vector2)targetCollider.bounds.center : (Vector2)playerEntity.transform.position;
        if ((aim - rb.position).sqrMagnitude > attackRange * attackRange) return;

        aimedPosition = aim; // A volley aims once; moving after the windup can evade it.
        attackDirection = aim.x >= rb.position.x ? 1f : -1f;
        var scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x) * attackDirection;
        transform.localScale = scale;
        isAttacking = true;
        StopBlink();
        volleyEmitted = false;
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isMoving", false);
        animator.SetBool(Attacking, true);
        // The animation event normally finishes the attack; this handles interrupted playback.
        attackEndsAt = Time.time + attackDuration + .25f;
    }

    // Animation event: first open-mouth frame after the two inflated-cheek poses.
    public void EmitWispVolley()
    {
        if (!provoked || !isAttacking || volleyEmitted || isKnockback || !CanDealContactDamage
            || !HasLivingPlayer || projectilePrefab == null) return;
        volleyEmitted = true;
        EnemyAttackAudio.PlayFor(this, "Audio/WispAttack", .45f);
        Vector2 mouth = (Vector2)transform.position + new Vector2(
            mouthOffset.x * attackDirection * Mathf.Abs(transform.lossyScale.x),
            mouthOffset.y * Mathf.Abs(transform.lossyScale.y));
        for (int i = 0; i < 3; i++)
        {
            Vector2 destination = aimedPosition + Vector2.right * ((i - 1) * destinationSpacing);
            var projectile = Instantiate(projectilePrefab, mouth, Quaternion.identity);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(projectile.gameObject, gameObject.scene);
            projectile.Launch(destination, flightDuration + (i - 1) * .12f,
                arcHeight + i * .4f, Attack_Power, this);
        }
    }

    public void FinishWispAttack()
    {
        if (isAttacking) nextAttackAt = Time.time + attackCooldown;
        isAttacking = false;
        volleyEmitted = true;
        if (animator != null) animator.SetBool(Attacking, false);
    }

    public override void ApplyKnockback(float directionX)
    {
        StopBlink();
        FinishWispAttack();
        base.ApplyKnockback(directionX);
        // A hovering enemy has no gravity to cancel an upward knockback impulse.
        if (rb != null) rb.linearVelocity = LimitTerrainVelocity(new Vector2(rb.linearVelocity.x, 0f));
    }

    protected override void OnDefeated()
    {
        StopBlink();
        FinishWispAttack();
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isMoving", false);
        base.OnDefeated();
    }

    // Animation event on the frame where the face dissolves into scattered flame.
    public void PlayWispDisappearanceSound()
    {
        EnemyDeathAudio.PlayFor(this, "Audio/WispDeath", .45f);
    }

    protected override void OnDisable()
    {
        StopBlink();
        FinishWispAttack();
        base.OnDisable();
    }
}
