using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

public partial class MON_Ghost
{
    public enum ScareAttackPhase { None, Flying, Scaring }

    [Header("Basic scare attack")]
    [SerializeField, Min(.1f)] private float scareDistance = 1.4f;
    [SerializeField, Min(.1f)] private float maximumFlightSeconds = 3f;
    [SerializeField, Min(.1f)] private float scareCooldown = 1.5f;
    [SerializeField] private Sprite[] scareEffectFrames;
    [SerializeField] private Vector2 scareSize = new Vector2(2.4f, 1.8f);

    public ScareAttackPhase CurrentAttackPhase { get; private set; }
    public override float ContactDamage => CurrentAttackPhase == ScareAttackPhase.None ? base.ContactDamage : 0f;
    protected override bool ControlsAttackMovement => true;
    protected override string DeathAnimationState => "GhostDead";
    protected override bool CanUseAbility() => CurrentAttackPhase != ScareAttackPhase.None || base.CanUseAbility();

    private float scareElapsed, nextScareTime, scareDirection, savedGravity;
    private bool flightGravityChanged, scareEmitted;
    private GameObject scareEffect;
    private SpriteRenderer scareEffectRenderer;
    private readonly List<Collider2D> scareIgnoredColliders = new List<Collider2D>();
    private readonly Collider2D[] scareHits = new Collider2D[16];

    private void TryBeginScareAttack()
    {
        if (CurrentAttackPhase != ScareAttackPhase.None || Time.timeScale <= 0f
            || Time.time < nextScareTime || isKnockback || IsSpawnContactProtected
            || !HasLivingPlayer || !CanDetectLivingPlayer()) return;
        CurrentAttackPhase = ScareAttackPhase.Flying;
        isAttacking = true;
        scareElapsed = 0f;
        scareEmitted = false;
        savedGravity = rb.gravityScale;
        flightGravityChanged = true;
        rb.gravityScale = 0f;
        foreach (var collider in playerEntity.GetComponentsInChildren<Collider2D>())
            if (collider.enabled && !Physics2D.GetIgnoreCollision(capsuleCollider2D, collider))
            {
                Physics2D.IgnoreCollision(capsuleCollider2D, collider, true);
                scareIgnoredColliders.Add(collider);
            }
        animator.Play("Ghost_Fly", 0, 0f);
        animator.Update(0f);
    }

    protected override void AttackFixedUpdate()
    {
        if (CurrentAttackPhase == ScareAttackPhase.None) return;
        if (!HasLivingPlayer || !CanDealContactDamage) { FinishScareAttack(true); return; }
        scareElapsed += Time.fixedDeltaTime;
        if (CurrentAttackPhase == ScareAttackPhase.Flying)
        {
            if (!CanDetectLivingPlayer() || scareElapsed >= maximumFlightSeconds)
            { FinishScareAttack(true); return; }
            Vector2 offset = (Vector2)playerEntity.transform.position - rb.position;
            scareDirection = offset.x >= 0f ? 1f : -1f;
            var scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x) * scareDirection;
            transform.localScale = scale;
            if (Mathf.Abs(offset.x) <= scareDistance && Mathf.Abs(offset.y) <= .65f)
            {
                CurrentAttackPhase = ScareAttackPhase.Scaring;
                scareElapsed = 0f;
                rb.linearVelocity = Vector2.zero;
                animator.Play("Ghost_Scare", 0, 0f);
                animator.Update(0f);
            }
            else rb.linearVelocity = offset.normalized * (Speed * 1.2f);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            if (scareEffectRenderer != null && scareEffectFrames != null && scareEffectFrames.Length > 0)
            {
                int index = Mathf.FloorToInt((scareElapsed - .12f) / .09f);
                if (index >= scareEffectFrames.Length) DestroyScareEffect();
                else scareEffectRenderer.sprite = scareEffectFrames[Mathf.Max(0, index)];
            }
            if (scareElapsed >= .64f) FinishScareAttack(true);
        }
    }

    // Animation event on supplied frame 7: both the effect and damage start here.
    public void EmitScareAttack()
    {
        if (CurrentAttackPhase != ScareAttackPhase.Scaring || scareEmitted || !CanDealContactDamage) return;
        scareEmitted = true;
        EnemyAttackAudio.PlayFor(this, "Audio/GhostAttack", startSeconds: .3f);
        Vector2 center = (Vector2)capsuleCollider2D.bounds.center + Vector2.right * scareDirection * .9f;
        if (scareEffectFrames != null && scareEffectFrames.Length > 0)
        {
            scareEffect = new GameObject("Ghost Scare Effect");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(scareEffect, gameObject.scene);
            scareEffect.transform.position = center;
            scareEffectRenderer = scareEffect.AddComponent<SpriteRenderer>();
            scareEffectRenderer.sprite = scareEffectFrames[0];
            scareEffectRenderer.sharedMaterial = spriteRenderer.sharedMaterial;
            scareEffectRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            scareEffectRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            scareEffectRenderer.flipX = scareDirection < 0f;
        }
        var filter = new ContactFilter2D();
        filter.SetLayerMask(_playerLayer);
        filter.useTriggers = false;
        int count = Physics2D.OverlapBox(center, scareSize, 0f, filter, scareHits);
        for (int i = 0; i < count; i++)
        {
            var victim = scareHits[i].GetComponentInParent<PlayerEntity>();
            if (victim == null || victim.IsDead) continue;
            float before = victim.HP;
            victim.TakeDamage(Attack_Power);
            if (!victim.IsDead && victim.HP < before) victim.ApplyKnockback(scareDirection);
            break;
        }
    }

    private void DestroyScareEffect()
    {
        if (scareEffect != null) Destroy(scareEffect);
        scareEffect = null;
        scareEffectRenderer = null;
    }

    private void FinishScareAttack(bool returnToIdle)
    {
        DestroyScareEffect();
        foreach (var collider in scareIgnoredColliders)
            if (collider != null && capsuleCollider2D != null)
                Physics2D.IgnoreCollision(capsuleCollider2D, collider, false);
        scareIgnoredColliders.Clear();
        bool wasAttacking = CurrentAttackPhase != ScareAttackPhase.None;
        if (rb != null && flightGravityChanged)
        {
            rb.gravityScale = savedGravity;
            rb.linearVelocity = Vector2.zero;
        }
        flightGravityChanged = false;
        CurrentAttackPhase = ScareAttackPhase.None;
        isAttacking = false;
        if (wasAttacking) nextScareTime = Time.time + scareCooldown;
        if (returnToIdle && HP > 0f && animator != null && animator.isActiveAndEnabled)
            animator.Play("GhostIdle", 0, 0f);
    }

    public override void ApplyKnockback(float directionX)
    {
        if (CurrentAttackPhase != ScareAttackPhase.None) FinishScareAttack(true);
        base.ApplyKnockback(directionX);
    }
}
