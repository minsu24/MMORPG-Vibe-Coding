using UnityEngine;
using EasternFantasy.Player;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class MON_Ginseng : EnemyController
{
    [SerializeField, Min(0.1f)] private float walkAnimationReferenceSpeed = 1.4f;
    private static readonly int WalkCycleSpeed = Animator.StringToHash("walkCycleSpeed");
    [Header("Root Swipe")]
    [SerializeField, Min(0.1f)] private float attackRange = 2f;
    [SerializeField, Min(0.1f)] private float attackReach = 2f;
    [SerializeField, Min(0.1f)] private float attackHeight = 1.25f;
    [SerializeField, Min(0f)] private float attackCooldown = 1.3f;
    [SerializeField, Min(0.1f)] private float attackDuration = 0.9f;
    private float nextAttackAt, attackEndsAt, attackDirection;
    private bool impactFired;
    private AudioSource footstepAudio;
    private float nextFootstepAt;
    private readonly Collider2D[] attackHits = new Collider2D[16];
    protected override string DeathAnimationState => "GinsengDefeated";

    private void LateUpdate()
    {
        UpdateFootstepAudio();
        if (animator == null || rb == null || HP <= 0f) return;
        // Slow patrols take slower steps; chasing uses the normal authored stride.
        float rate = Mathf.Abs(rb.linearVelocity.x) / walkAnimationReferenceSpeed;
        animator.SetFloat(WalkCycleSpeed, Mathf.Clamp(rate, 0.2f, 2.5f));
    }

    private void UpdateFootstepAudio()
    {
        bool walking = HP > 0f && !isAttacking && !isKnockback && Time.timeScale > 0f
            && rb != null && rb.simulated && Mathf.Abs(rb.linearVelocity.x) > .1f
            && Mathf.Abs(rb.linearVelocity.y) < .2f;
        if (!walking)
        {
            StopFootstepAudio();
            return;
        }
        if (footstepAudio != null)
            footstepAudio.volume = LocalAmbientAudio2D.GetPlayerVolumeAt(transform.position, .16f);
        if (Time.time < nextFootstepAt) return;
        if (footstepAudio == null)
        {
            var clip = Resources.Load<AudioClip>("Audio/GinsengStep");
            if (clip == null) return;
            footstepAudio = gameObject.AddComponent<AudioSource>();
            footstepAudio.playOnAwake = false;
            footstepAudio.loop = false;
            footstepAudio.spatialBlend = 0f;
            footstepAudio.volume = LocalAmbientAudio2D.GetPlayerVolumeAt(transform.position, .16f);
            footstepAudio.clip = clip;
        }
        float strideRate = Mathf.Clamp(Mathf.Abs(rb.linearVelocity.x) / walkAnimationReferenceSpeed, .2f, 2.5f);
        footstepAudio.pitch = Mathf.Clamp(strideRate, .85f, 1.2f);
        if (!footstepAudio.isPlaying) footstepAudio.Play();
        nextFootstepAt = Time.time + Mathf.Max(footstepAudio.clip.length / footstepAudio.pitch, .56f / strideRate);
    }

    private void StopFootstepAudio()
    {
        if (footstepAudio != null) footstepAudio.Stop();
        nextFootstepAt = 0f;
    }

    protected override bool CanUseAbility() => isAttacking || base.CanUseAbility();

    protected override void MonsterAbility()
    {
        if (isAttacking)
        {
            if (!HasLivingPlayer || HP <= 0f || Time.time >= attackEndsAt) FinishAttack(HP > 0f);
            return;
        }
        if (!HasLivingPlayer || isKnockback || Time.time < nextAttackAt || Mathf.Abs(rb.linearVelocity.y) > 0.3f) return;
        Vector2 delta = playerEntity.transform.position - transform.position;
        if (Mathf.Abs(delta.x) > attackRange || Mathf.Abs(delta.y - 0.7f) > 0.9f) return;
        BeginAttack(delta.x < 0f ? -1f : 1f);
    }

    private void BeginAttack(float direction)
    {
        attackDirection = direction;
        var scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;
        isAttacking = true;
        StopFootstepAudio();
        EnemyAttackAudio.PlayAt(gameObject, "Audio/GinsengAttack", .5f);
        impactFired = false;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        animator.SetBool("isMoving", false);
        animator.Play("GinsengAttack", 0, 0);
        attackEndsAt = Time.time + attackDuration;
    }

    // Animation event at the first fully extended root-arm pose. One hit per swing.
    public void GinsengAttackHit()
    {
        if (!isAttacking || impactFired || isKnockback || HP <= 0f || !HasLivingPlayer) return;
        impactFired = true;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(_playerLayer);
        filter.useTriggers = false;
        Vector2 center = (Vector2)transform.position + new Vector2(attackDirection * attackReach * 0.5f, attackHeight * 0.5f);
        int count = gameObject.scene.GetPhysicsScene2D().OverlapBox(center, new Vector2(attackReach, attackHeight), 0, filter, attackHits);
        for (int i = 0; i < count; i++)
        {
            var entity = attackHits[i].GetComponentInParent<PlayerEntity>();
            if (entity != playerEntity) continue;
            uint health = entity.HitReactionVersion;
            entity.TakeDamage(Attack_Power);
            if (!entity.IsDead && entity.HitReactionVersion != health) entity.ApplyKnockback(attackDirection);
            break;
        }
    }

    public void GinsengAttackFinished()
    {
        if (isAttacking) FinishAttack(HP > 0f);
    }

    private void FinishAttack(bool returnToIdle)
    {
        isAttacking = false;
        impactFired = true;
        nextAttackAt = Time.time + attackCooldown;
        if (returnToIdle && animator != null && animator.isActiveAndEnabled) animator.Play("GinsengIdle", 0, 0);
    }

    public override void ApplyKnockback(float directionX)
    {
        if (isAttacking) FinishAttack(true);
        base.ApplyKnockback(directionX);
    }

    protected override void OnDefeated()
    {
        StopFootstepAudio();
        EnemyDeathAudio.PlayFor(this, "Audio/GinsengDeath", .5f);
        FinishAttack(false);
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetBool("isMoving", false);
        base.OnDefeated();
    }

    protected override void OnDisable()
    {
        StopFootstepAudio();
        FinishAttack(false);
        base.OnDisable();
    }

    public override float ContactDamage => 0f;
}
