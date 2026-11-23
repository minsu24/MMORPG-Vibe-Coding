using UnityEngine;

public sealed class MON_WildCat : EnemyController
{
    private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
    private static readonly int LandingState = Animator.StringToHash("Base Layer.Landing");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");

    [Header("Pounce")]
    [SerializeField, Min(.1f)] private float attackRange = 5.5f;
    [SerializeField, Min(.1f)] private float attackHalfHeight = 1f;
    [SerializeField, Min(.1f)] private float pounceSpeed = 14f;
    [SerializeField, Min(.1f)] private float jumpSpeed = 5.3f;
    [SerializeField, Min(0f)] private float attackCooldown = 1.3f;

    private enum AttackPhase { None, Crouch, Airborne, Landing, Rest }

    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private ContactFilter2D groundFilter;
    private AttackPhase phase;
    private float pounceDirection;
    private float resumeTime;
    private float nextAttackTime;
    private bool hitPlayer;

    // The pounce owns the hit during flight. Keep landing contact from hitting the same player again.
    public override float ContactDamage =>
        phase == AttackPhase.Airborne || (isAttacking && hitPlayer) ? 0f : base.ContactDamage;
    protected override bool ControlsAttackMovement => true;
    protected override string DeathAnimationState => "Base Layer.Dead";

    protected override void Awake()
    {
        base.Awake();
        groundFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer)
            & ~(_playerLayer.value | LayerMask.GetMask("Enemy")));
        groundFilter.useTriggers = false;
    }

    protected override void MonsterAbility()
    {
        if (isAttacking || isKnockback || Time.time < nextAttackTime)
            return;

        Vector2 offset = playerEntity.transform.position - transform.position;
        if (Mathf.Abs(offset.x) > attackRange || Mathf.Abs(offset.y) > attackHalfHeight || !HasGround())
            return;

        pounceDirection = offset.x >= 0f ? 1f : -1f;
        Vector3 scale = transform.localScale;
        scale.x = -pounceDirection * Mathf.Abs(scale.x);
        transform.localScale = scale;

        hitPlayer = false;
        phase = AttackPhase.Crouch;
        isAttacking = true;
        StopHorizontalMovement();
        animator.Play(AttackState, 0, 0f);
    }

    // The first airborne sprite launches the body.
    public void WildCatLaunch()
    {
        if (phase != AttackPhase.Crouch || HP <= 0f)
            return;

        if (isKnockback || !HasGround())
        {
            ReturnToIdle();
            return;
        }

        phase = AttackPhase.Airborne;
        rb.linearVelocity = new Vector2(pounceDirection * pounceSpeed, jumpSpeed);
    }

    protected override void AttackFixedUpdate()
    {
        if (phase == AttackPhase.Airborne)
        {
            if (rb.linearVelocity.y > .01f || !HasGround())
                return;

            phase = AttackPhase.Landing;
            animator.Play(LandingState, 0, 0f);
        }

        StopHorizontalMovement();
        if (phase == AttackPhase.Rest && Time.time >= resumeTime)
        {
            phase = AttackPhase.None;
            isAttacking = false;
        }
    }

    public void WildCatLandingFinished()
    {
        if (phase == AttackPhase.Landing && HP > 0f)
            ReturnToIdle();
    }

    private void ReturnToIdle()
    {
        phase = AttackPhase.Rest;
        resumeTime = Time.time + .25f;
        nextAttackTime = Time.time + attackCooldown;
        animator.SetBool("isMoving", false);
        animator.Play(IdleState, 0, 0f);
    }

    private bool HasGround()
    {
        int count = capsuleCollider2D.Cast(Vector2.down, groundFilter, groundHits, .045f);
        for (int i = 0; i < count; i++)
        {
            if (groundHits[i].normal.y > .55f
                && groundHits[i].collider.bounds.max.y <= capsuleCollider2D.bounds.min.y + .06f)
                return true;
        }
        return false;
    }

    private void StopHorizontalMovement()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision) => TryPounceHit(collision.collider);
    private void OnCollisionStay2D(Collision2D collision) => TryPounceHit(collision.collider);

    private void TryPounceHit(Collider2D other)
    {
        if (phase != AttackPhase.Airborne || hitPlayer || isKnockback
            || !CanDealContactDamage || !HasLivingPlayer || playerEntity.IsDamageImmune
            || other.GetComponentInParent<EasternFantasy.Player.PlayerEntity>() != playerEntity)
            return;

        hitPlayer = true;
        uint previousHit = playerEntity.HitReactionVersion;
        playerEntity.TakeDamage(Attack_Power);
        if (!playerEntity.IsDead && playerEntity.HitReactionVersion != previousHit)
            playerEntity.ApplyKnockback(pounceDirection);
    }

    protected override void OnDisable()
    {
        phase = AttackPhase.None;
        isAttacking = false;
        base.OnDisable();
    }
}
