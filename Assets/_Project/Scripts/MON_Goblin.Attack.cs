using System.Collections.Generic;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using UnityEngine;

public partial class MON_Goblin
{
    public enum LeapAttackPhase { None, Approach, Windup, Leaping, Recovery }

    [Header("Basic leap attack")]
    [SerializeField, Min(.1f)] private float attackRange = 4.5f;
    [SerializeField, Min(.1f)] private float launchDistance = 1.8f;
    [SerializeField, Min(.1f)] private float maximumApproachSeconds = 1f;
    [SerializeField, Min(.01f)] private float windupSeconds = .32f;
    [SerializeField, Min(.1f)] private float flightSeconds = .48f;
    [SerializeField, Min(.1f)] private float leapHeight = .65f;
    [SerializeField, Min(.1f)] private float maximumLeapDistance = 2.8f;
    [SerializeField, Min(.01f)] private float recoverySeconds = .4f;
    [SerializeField, Min(.1f)] private float leapCooldown = 1.5f;
    [SerializeField] private Vector2 strikeSize = new Vector2(1.15f, 1.05f);

    public LeapAttackPhase CurrentAttackPhase { get; private set; }
    public override float ContactDamage => CurrentAttackPhase == LeapAttackPhase.None ? base.ContactDamage : 0f;
    protected override bool ControlsAttackMovement => true;
    protected override string DeathAnimationState => "Goblin_Dead";
    protected override bool CanUseAbility() => CurrentAttackPhase != LeapAttackPhase.None || base.CanUseAbility();

    private float phaseElapsed, nextLeapTime, attackDirection, committedTargetX;
    private float leapHorizontalSpeed, previousGravity;
    private bool gravityChanged, strikeHit, swingSoundPlayed, impactSoundPlayed;
    private readonly List<Collider2D> leapIgnoredColliders = new List<Collider2D>();
    private readonly Collider2D[] groundResults = new Collider2D[8];
    private readonly Collider2D[] strikeResults = new Collider2D[16];

    private void TryBeginLeapAttack()
    {
        if (CurrentAttackPhase != LeapAttackPhase.None || Time.timeScale <= 0f
            || Time.time < nextLeapTime || isKnockback || IsSpawnContactProtected
            || !HasLivingPlayer || !CanDetectLivingPlayer() || !IsLeapGrounded()) return;
        Vector2 offset = playerEntity.transform.position - transform.position;
        if (Mathf.Abs(offset.x) > attackRange || Mathf.Abs(offset.y) > 1.4f) return;
        CurrentAttackPhase = LeapAttackPhase.Approach;
        isAttacking = true;
        phaseElapsed = 0f;
        strikeHit = false;
        swingSoundPlayed = false;
        impactSoundPlayed = false;
    }

    protected override void AttackFixedUpdate()
    {
        if (CurrentAttackPhase == LeapAttackPhase.None) return;
        if (!HasLivingPlayer || HP <= 0f) { FinishLeapAttack(true); return; }
        phaseElapsed += Time.fixedDeltaTime;
        switch (CurrentAttackPhase)
        {
            case LeapAttackPhase.Approach:
                if (!CanDetectLivingPlayer() || phaseElapsed > maximumApproachSeconds)
                { FinishLeapAttack(true); return; }
                float distance = playerEntity.transform.position.x - rb.position.x;
                attackDirection = distance >= 0f ? 1f : -1f;
                FaceLeapDirection();
                if (Mathf.Abs(distance) <= launchDistance && IsLeapGrounded()) BeginWindup();
                else if (!IsLeapGrounded(attackDirection * (capsuleCollider2D.bounds.extents.x + .15f)))
                    FinishLeapAttack(true);
                else rb.linearVelocity = new Vector2(attackDirection * Speed, rb.linearVelocity.y);
                break;
            case LeapAttackPhase.Windup:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (phaseElapsed >= windupSeconds) LaunchLeap();
                break;
            case LeapAttackPhase.Leaping:
                rb.linearVelocity = new Vector2(leapHorizontalSpeed, rb.linearVelocity.y);
                if (phaseElapsed >= flightSeconds * .65f) TryLeapStrike();
                if ((phaseElapsed >= .12f && rb.linearVelocity.y <= .1f && IsLeapGrounded())
                    || phaseElapsed >= flightSeconds + .35f) BeginRecovery();
                break;
            case LeapAttackPhase.Recovery:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (phaseElapsed >= recoverySeconds) FinishLeapAttack(true);
                break;
        }
    }

    private void BeginWindup()
    {
        CurrentAttackPhase = LeapAttackPhase.Windup;
        phaseElapsed = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        // Aim once, leaving the player time to dodge instead of homing in midair.
        committedTargetX = playerEntity.transform.position.x;
        IgnoreLeapPlayerCollisions();
        animator.Play("Goblin_Attack", 0, 0f);
        animator.Update(0f);
    }

    private void LaunchLeap()
    {
        float targetX = Mathf.Clamp(committedTargetX, rb.position.x - maximumLeapDistance,
            rb.position.x + maximumLeapDistance);
        Collider2D support = DropPlacement2D.FindSupport(gameObject.scene, rb.position);
        if (support != null && support.bounds.size.x > capsuleCollider2D.bounds.size.x)
        {
            float margin = capsuleCollider2D.bounds.extents.x + .05f;
            targetX = Mathf.Clamp(targetX, support.bounds.min.x + margin, support.bounds.max.x - margin);
        }
        leapHorizontalSpeed = (targetX - rb.position.x) / flightSeconds;
        previousGravity = rb.gravityScale;
        gravityChanged = true;
        float gravity = 8f * leapHeight / (flightSeconds * flightSeconds);
        rb.gravityScale = gravity / Mathf.Max(.01f, Mathf.Abs(Physics2D.gravity.y));
        rb.linearVelocity = new Vector2(leapHorizontalSpeed, gravity * flightSeconds * .5f);
        CurrentAttackPhase = LeapAttackPhase.Leaping;
        phaseElapsed = 0f;
    }

    private void BeginRecovery()
    {
        TryLeapStrike();
        CurrentAttackPhase = LeapAttackPhase.Recovery;
        phaseElapsed = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (gravityChanged) { rb.gravityScale = previousGravity; gravityChanged = false; }
        // Land on the impact drawing even if world geometry shortened the flight.
        animator.Play("Goblin_Attack", 0, .8f / 1.2f);
        animator.Update(0f);
        PlayGoblinAttackImpactSound();
    }

    // Frame 6: the airborne club swing. Re-entry to the impact pose must not replay it.
    public void PlayGoblinSwingSound()
    {
        if (CurrentAttackPhase != LeapAttackPhase.Leaping || swingSoundPlayed || !CanDealContactDamage) return;
        swingSoundPlayed = true;
        EnemyAttackAudio.PlayFor(this, "Audio/GoblinSwing");
    }

    private void PlayGoblinAttackImpactSound()
    {
        if (CurrentAttackPhase != LeapAttackPhase.Recovery || impactSoundPlayed || !CanDealContactDamage) return;
        impactSoundPlayed = true;
        EnemyAttackAudio.PlayFor(this, "Audio/GoblinAttackImpact", .21f);
    }

    private void TryLeapStrike()
    {
        if (strikeHit || !CanDealContactDamage) return;
        Bounds bounds = capsuleCollider2D.bounds;
        Vector2 center = (Vector2)bounds.center + Vector2.right * attackDirection * .3f;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(_playerLayer);
        filter.useTriggers = false;
        int count = Physics2D.OverlapBox(center, strikeSize, 0f, filter, strikeResults);
        for (int i = 0; i < count; i++)
        {
            PlayerEntity victim = strikeResults[i].GetComponentInParent<PlayerEntity>();
            if (victim == null || victim.IsDead) continue;
            strikeHit = true;
            float before = victim.HP;
            victim.TakeDamage(Attack_Power);
            if (!victim.IsDead && victim.HP < before) victim.ApplyKnockback(attackDirection);
            break;
        }
    }

    private bool IsLeapGrounded(float ahead = 0f)
    {
        if (capsuleCollider2D == null || rb == null || rb.linearVelocity.y > .15f) return false;
        Bounds bounds = capsuleCollider2D.bounds;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(~(_playerLayer.value | LayerMask.GetMask("Enemy")));
        filter.useTriggers = false;
        return Physics2D.OverlapBox(new Vector2(bounds.center.x + ahead, bounds.min.y - .015f),
            new Vector2(bounds.size.x * .65f, .09f), 0f, filter, groundResults) > 0;
    }

    private void FaceLeapDirection()
    {
        Vector3 scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x) * attackDirection;
        transform.localScale = scale;
    }

    private void IgnoreLeapPlayerCollisions()
    {
        foreach (Collider2D collider in playerEntity.GetComponentsInChildren<Collider2D>())
            if (collider.enabled && !Physics2D.GetIgnoreCollision(capsuleCollider2D, collider))
            {
                Physics2D.IgnoreCollision(capsuleCollider2D, collider, true);
                leapIgnoredColliders.Add(collider);
            }
    }

    private void FinishLeapAttack(bool returnToIdle)
    {
        foreach (Collider2D collider in leapIgnoredColliders)
            if (collider != null && capsuleCollider2D != null)
                Physics2D.IgnoreCollision(capsuleCollider2D, collider, false);
        leapIgnoredColliders.Clear();
        if (rb != null)
        {
            if (gravityChanged) rb.gravityScale = previousGravity;
            if (CurrentAttackPhase != LeapAttackPhase.None)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        gravityChanged = false;
        bool wasAttacking = CurrentAttackPhase != LeapAttackPhase.None;
        CurrentAttackPhase = LeapAttackPhase.None;
        isAttacking = false;
        if (wasAttacking) nextLeapTime = Time.time + leapCooldown;
        if (returnToIdle && HP > 0f && animator != null && animator.isActiveAndEnabled)
            animator.Play("Goblin_Idle", 0, 0f);
    }

    public override void ApplyKnockback(float directionX)
    {
        if (CurrentAttackPhase != LeapAttackPhase.None) FinishLeapAttack(true);
        base.ApplyKnockback(directionX);
    }
}
