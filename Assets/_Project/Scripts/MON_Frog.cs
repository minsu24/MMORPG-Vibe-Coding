using System.Collections;
using EasternFantasy.Player;
using UnityEngine;

public sealed class MON_Frog : EnemyController
{
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");

    [Header("Tongue Attack")]
    [SerializeField, Min(0.1f)] private float attackRange = 4f;
    [SerializeField, Min(0.1f)] private float verticalTolerance = 1.5f;
    [SerializeField, Min(0f)] private float hitDelay = 0.375f;
    [SerializeField, Min(0.1f)] private float attackAnimationDuration = 0.75f;
    [SerializeField, Min(0f)] private float attackCooldown = 1.5f;


    [SerializeField] private GameObject BossMapPortal;

    private float nextAttackTime;

    protected override bool CanUseAbility()
    {
        if (player == null)
        {
            inFarAttackRange = false;
            return false;
        }

        Vector2 offset = player.transform.position - transform.position;
        bool detected = offset.sqrMagnitude <= _detectRange * _detectRange;

        inFarAttackRange = detected
            && Mathf.Abs(offset.x) <= attackRange
            && Mathf.Abs(offset.y) <= verticalTolerance;

        return detected;
    }

    protected override void MonsterAbility()
    {
        if (!inFarAttackRange || isAttacking || Time.time < nextAttackTime)
            return;

        StartCoroutine(TongueAttackRoutine());
    }

    protected override void OnDefeated()
    {
        BossMapPortal.SetActive(true);
        base.OnDefeated();
    }


    private IEnumerator TongueAttackRoutine()
    {
        isAttacking = true;
        FacePlayer();

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.ResetTrigger(AttackTrigger);
            animator.SetTrigger(AttackTrigger);
        }

        yield return new WaitForSeconds(Mathf.Min(hitDelay, attackAnimationDuration));

        TryHitPlayer();

        float remainingAnimationTime = Mathf.Max(0f, attackAnimationDuration - hitDelay);
        if (remainingAnimationTime > 0f)
            yield return new WaitForSeconds(remainingAnimationTime);

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    private void TryHitPlayer()
    {
        if (playerEntity == null || playerEntity.IsDead || playerEntity.isInvincible)
            return;

        Vector2 offset = playerEntity.transform.position - transform.position;
        if (Mathf.Abs(offset.x) > attackRange || Mathf.Abs(offset.y) > verticalTolerance)
            return;

        playerEntity.TakeDamage(Attack_Power);

        float knockbackDirection = offset.x >= 0f ? 1f : -1f;
        playerEntity.ApplyKnockback(knockbackDirection);
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 scale = transform.localScale;
        float absoluteScaleX = Mathf.Abs(scale.x);
        scale.x = player.transform.position.x >= transform.position.x
            ? -absoluteScaleX
            : absoluteScaleX;
        transform.localScale = scale;
    }

    private void OnValidate()
    {
        attackRange = Mathf.Max(0.1f, attackRange);
        verticalTolerance = Mathf.Max(0.1f, verticalTolerance);
        attackAnimationDuration = Mathf.Max(0.1f, attackAnimationDuration);
        hitDelay = Mathf.Clamp(hitDelay, 0f, attackAnimationDuration);
        attackCooldown = Mathf.Max(0f, attackCooldown);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.25f, 0.15f, 0.85f);
        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(attackRange * 2f, verticalTolerance * 2f, 0f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectRange);
    }
}
