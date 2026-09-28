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
    private LineRenderer attackWarning;
    private Material attackWarningMaterial;

    protected override bool CanUseAbility()
    {
        if (!HasLivingPlayer)
        {
            inFarAttackRange = false;
            return false;
        }

        Vector2 offset = player.transform.position - transform.position;
        bool detected = base.CanUseAbility();

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
        if (attackWarning != null)
            attackWarning.enabled = false;
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

        ShowAttackWarning();

        yield return new WaitForSeconds(Mathf.Min(hitDelay, attackAnimationDuration));

        if (attackWarning != null)
            attackWarning.enabled = false;
        if (HP <= 0f)
            yield break;

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
        if (!playerEntity.IsDead)
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

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(1f, 0.25f, 0.15f, 0.85f);
        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(attackRange * 2f, verticalTolerance * 2f, 0f));
    }

    private void ShowAttackWarning()
    {
        if (attackWarning == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
                return;
            GameObject warning = new GameObject("Tongue Attack Range", typeof(LineRenderer));
            warning.transform.SetParent(transform, false);
            attackWarning = warning.GetComponent<LineRenderer>();
            attackWarningMaterial = new Material(shader);
            attackWarning.sharedMaterial = attackWarningMaterial;
            attackWarning.useWorldSpace = true;
            attackWarning.loop = true;
            attackWarning.positionCount = 4;
            attackWarning.widthMultiplier = 0.06f;
            attackWarning.startColor = attackWarning.endColor =
                new Color(1f, 0.2f, 0.1f, 0.85f);
            attackWarning.sortingLayerID = spriteRenderer.sortingLayerID;
            attackWarning.sortingOrder = spriteRenderer.sortingOrder + 2;
        }

        Vector3 center = transform.position;
        attackWarning.SetPosition(0, center + new Vector3(-attackRange, -verticalTolerance));
        attackWarning.SetPosition(1, center + new Vector3(attackRange, -verticalTolerance));
        attackWarning.SetPosition(2, center + new Vector3(attackRange, verticalTolerance));
        attackWarning.SetPosition(3, center + new Vector3(-attackRange, verticalTolerance));
        attackWarning.enabled = true;
    }

    private void OnDestroy()
    {
        if (attackWarningMaterial != null)
            Destroy(attackWarningMaterial);
    }
}
