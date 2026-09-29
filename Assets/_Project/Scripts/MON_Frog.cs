using System.Collections;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using UnityEngine;

public sealed class MON_Frog : EnemyController
{
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int JumpState = Animator.StringToHash("Base Layer.Jump");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int RangedAttackState = Animator.StringToHash("Base Layer.RangedAttack");

    [Header("Tongue Attack")]
    [SerializeField, Min(0.1f)] private float attackRange = 4f;
    [SerializeField, Min(0.1f)] private float verticalTolerance = 1.5f;
    [SerializeField, Min(0f)] private float hitDelay = 0.375f;
    [SerializeField, Min(0.1f)] private float attackAnimationDuration = 0.75f;
    [SerializeField, Min(0f)] private float attackCooldown = 1.5f;

    [Header("Ranged Attack")]
    [SerializeField] private FrogSpitProjectile spitProjectilePrefab;
    [SerializeField] private Vector2 mouthOffset = new Vector2(1.15f, 0.35f);
    [SerializeField, Min(0f)] private float rangedDamage = 20f;
    [SerializeField, Min(0.1f)] private float rangedProjectileSpeed = 9f;
    [SerializeField, Min(0.1f)] private float rangedTravelDistance = 14f;
    [SerializeField, Min(0.1f)] private float rangedVerticalTolerance = 4f;
    [SerializeField, Min(0f)] private float rangedWindup = 0.35f;
    [SerializeField, Min(0.1f)] private float rangedAnimationDuration = 0.75f;
    [SerializeField, Min(0f)] private float rangedCooldown = 2f;

    [Header("Jump Slam")]
    [SerializeField, Min(0.1f)] private float jumpRange = 7f;
    [SerializeField, Min(0.1f)] private float jumpVerticalTolerance = 3f;
    [SerializeField, Min(0.1f)] private float jumpHeight = 3.2f;
    [Tooltip("Time of the highest point in the Jump clip.")]
    [SerializeField, Min(0.01f)] private float jumpApexTime = 0.3f;
    [Tooltip("How long the frog waits motionless at the highest point.")]
    [SerializeField, Min(0f)] private float jumpHangTime = 0.4f;
    [Tooltip("The impact sprite in Jump.anim starts at 0.5 seconds.")]
    [SerializeField, Min(0.02f)] private float jumpLandingTime = 0.5f;
    [Tooltip("Full length of Jump.anim at 6 FPS.")]
    [SerializeField, Min(0.1f)] private float jumpAnimationDuration = 1.1666666f;
    [SerializeField, Min(0f)] private float jumpCooldown = 5f;
    [SerializeField, Min(0.1f)] private float slamWidth = 3.8f;
    [SerializeField, Min(0.1f)] private float slamVerticalTolerance = 2.5f;
    [SerializeField, Min(1f)] private float slamDamageMultiplier = 2.5f;
    [SerializeField, Min(0.02f)] private float warningHeight = 0.32f;
    [SerializeField, Range(0.05f, 0.8f)] private float warningOpacity = 0.32f;

    [SerializeField] private GameObject BossMapPortal;

    private float nextAttackTime;
    private float nextJumpTime;
    private bool jumpCooldownStarted;
    private bool inTongueAttackRange;
    private LineRenderer attackWarning;
    private Material attackWarningMaterial;
    private SpriteRenderer jumpWarning;
    private Sprite jumpWarningSprite;
    private Vector2 jumpLandingPosition;
    private float jumpWarningY;
    private float animatorSpeedBeforeJump = 1f;

    protected override bool CanUseAbility()
    {
        if (!HasLivingPlayer)
        {
            inFarAttackRange = false;
            inTongueAttackRange = false;
            return false;
        }

        Vector2 offset = player.transform.position - transform.position;
        bool detected = base.CanUseAbility();
        inTongueAttackRange = detected
            && Mathf.Abs(offset.x) <= attackRange
            && Mathf.Abs(offset.y) <= verticalTolerance;
        inFarAttackRange = detected && Mathf.Abs(offset.y) <= rangedVerticalTolerance;
        return detected;
    }

    protected override void MonsterAbility()
    {
        if (isAttacking)
            return;

        if (!jumpCooldownStarted)
        {
            nextJumpTime = Time.time + jumpCooldown;
            jumpCooldownStarted = true;
        }

        Vector2 offset = player.transform.position - transform.position;
        if (inTongueAttackRange)
        {
            if (Time.time >= nextAttackTime)
                StartCoroutine(TongueAttackRoutine());
            return;
        }

        bool canJump = inFarAttackRange
            && Time.time >= nextJumpTime
            && Mathf.Abs(offset.x) <= jumpRange
            && Mathf.Abs(offset.y) <= jumpVerticalTolerance;

        if (canJump)
        {
            StartCoroutine(JumpSlamRoutine());
            return;
        }

        if (inFarAttackRange && Time.time >= nextAttackTime)
            StartCoroutine(RangedAttackRoutine());
    }

    protected override void OnDefeated()
    {
        HideWarnings();
        RestoreAnimatorSpeed();
        if (BossMapPortal != null)
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

    private IEnumerator RangedAttackRoutine()
    {
        isAttacking = true;
        FacePlayer();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        if (animator != null)
            animator.Play(RangedAttackState, 0, 0f);

        yield return new WaitForSeconds(Mathf.Min(rangedWindup, rangedAnimationDuration));
        if (HP <= 0f)
            yield break;

        FireSpitProjectile();

        float recoveryTime = Mathf.Max(0f, rangedAnimationDuration - rangedWindup);
        if (recoveryTime > 0f)
            yield return new WaitForSeconds(recoveryTime);
        if (HP <= 0f)
            yield break;

        if (animator != null)
            animator.CrossFade(IdleState, 0.05f);
        nextAttackTime = Time.time + rangedCooldown;
        isAttacking = false;
    }

    private void FireSpitProjectile()
    {
        if (spitProjectilePrefab == null || playerEntity == null || playerEntity.IsDead)
            return;

        float directionX = playerEntity.transform.position.x >= transform.position.x ? 1f : -1f;
        Vector2 spawnPosition = (Vector2)transform.position
            + new Vector2(mouthOffset.x * directionX, mouthOffset.y);
        Vector2 targetPosition = (Vector2)playerEntity.transform.position + Vector2.up * 0.2f;
        Vector2 direction = (targetPosition - spawnPosition).normalized;

        FrogSpitProjectile projectile = Instantiate(
            spitProjectilePrefab, spawnPosition, Quaternion.identity);
        projectile.Launch(direction, rangedProjectileSpeed,
            rangedTravelDistance, rangedDamage, this);
    }

    private IEnumerator JumpSlamRoutine()
    {
        isAttacking = true;
        FacePlayer();

        Vector2 start = rb != null ? rb.position : (Vector2)transform.position;
        jumpLandingPosition = FindJumpLanding(start, player.transform.position.x, out jumpWarningY);
        ShowJumpWarning();

        bool colliderWasTrigger = capsuleCollider2D != null && capsuleCollider2D.isTrigger;
        if (capsuleCollider2D != null)
            capsuleCollider2D.isTrigger = true;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        if (animator != null)
        {
            animatorSpeedBeforeJump = animator.speed;
            animator.speed = 1f;
            animator.Play(JumpState, 0, 0f);
        }

        float riseElapsed = 0f;
        while (riseElapsed < jumpApexTime && HP > 0f)
        {
            riseElapsed += Time.deltaTime;
            float rise = Mathf.Clamp01(riseElapsed / jumpApexTime);
            float travel = Mathf.Clamp01(riseElapsed / jumpLandingTime);
            float x = Mathf.SmoothStep(start.x, jumpLandingPosition.x, travel);
            float y = Mathf.Lerp(start.y, jumpLandingPosition.y, travel)
                + jumpHeight * Mathf.SmoothStep(0f, 1f, rise);
            SetJumpPosition(new Vector2(x, y));
            yield return null;
        }

        if (HP <= 0f)
            yield break;

        float apexTravel = Mathf.Clamp01(jumpApexTime / jumpLandingTime);
        Vector2 apexPosition = new Vector2(
            Mathf.SmoothStep(start.x, jumpLandingPosition.x, apexTravel),
            Mathf.Lerp(start.y, jumpLandingPosition.y, apexTravel) + jumpHeight);
        SetJumpPosition(apexPosition);

        if (animator != null)
            animator.speed = 0f;

        float hangElapsed = 0f;
        while (hangElapsed < jumpHangTime && HP > 0f)
        {
            hangElapsed += Time.deltaTime;
            SetJumpPosition(apexPosition);
            yield return null;
        }

        if (HP <= 0f)
            yield break;

        if (animator != null)
            animator.speed = 1f;

        float descentDuration = Mathf.Max(0.01f, jumpLandingTime - jumpApexTime);
        float fallElapsed = 0f;
        while (fallElapsed < descentDuration && HP > 0f)
        {
            fallElapsed += Time.deltaTime;
            float fall = Mathf.Clamp01(fallElapsed / descentDuration);
            float clipTime = jumpApexTime + fallElapsed;
            float travel = Mathf.Clamp01(clipTime / jumpLandingTime);
            float x = Mathf.SmoothStep(start.x, jumpLandingPosition.x, travel);
            float y = Mathf.Lerp(start.y, jumpLandingPosition.y, travel)
                + jumpHeight * (1f - fall * fall);
            SetJumpPosition(new Vector2(x, y));
            yield return null;
        }

        if (HP <= 0f)
            yield break;

        // Jump.anim changes to its impact sprite at exactly 0.5 seconds.
        SetJumpPosition(jumpLandingPosition);
        if (jumpWarning != null)
            jumpWarning.enabled = false;
        TrySlamPlayer();

        if (capsuleCollider2D != null)
            capsuleCollider2D.isTrigger = colliderWasTrigger;

        float recoveryTime = Mathf.Max(0f, jumpAnimationDuration - jumpLandingTime);
        if (recoveryTime > 0f)
            yield return new WaitForSeconds(recoveryTime);
        if (HP <= 0f)
            yield break;

        if (animator != null)
            animator.CrossFade(IdleState, 0.05f);
        RestoreAnimatorSpeed();
        nextJumpTime = Time.time + jumpCooldown;
        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    private Vector2 FindJumpLanding(Vector2 start, float desiredX, out float floorY)
    {
        Collider2D floor = DropPlacement2D.FindFloor(gameObject.scene, start);
        if (floor == null)
        {
            floorY = start.y - 1f;
            return new Vector2(desiredX, start.y);
        }

        Bounds bounds = floor.bounds;
        float halfBodyWidth = capsuleCollider2D != null ? capsuleCollider2D.bounds.extents.x : 0.5f;
        float targetX = Mathf.Clamp(desiredX,
            bounds.min.x + halfBodyWidth,
            bounds.max.x - halfBodyWidth);
        floorY = bounds.max.y + 0.03f;
        return new Vector2(targetX, start.y);
    }

    private void SetJumpPosition(Vector2 position)
    {
        if (rb != null)
            rb.position = position;
        else
            transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    private void TrySlamPlayer()
    {
        if (playerEntity == null || playerEntity.IsDead || playerEntity.isInvincible)
            return;

        Vector2 offset = playerEntity.transform.position - (Vector3)jumpLandingPosition;
        if (Mathf.Abs(offset.x) > slamWidth * 0.5f || Mathf.Abs(offset.y) > slamVerticalTolerance)
            return;

        playerEntity.TakeDamage(Attack_Power * slamDamageMultiplier);
        float knockbackDirection = Mathf.Approximately(offset.x, 0f) ? 1f : Mathf.Sign(offset.x);
        if (!playerEntity.IsDead)
            playerEntity.ApplyKnockback(knockbackDirection);
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
        scale.x = player.transform.position.x >= transform.position.x ? -absoluteScaleX : absoluteScaleX;
        transform.localScale = scale;
    }

    private void OnValidate()
    {
        attackRange = Mathf.Max(0.1f, attackRange);
        verticalTolerance = Mathf.Max(0.1f, verticalTolerance);
        attackAnimationDuration = Mathf.Max(0.1f, attackAnimationDuration);
        hitDelay = Mathf.Clamp(hitDelay, 0f, attackAnimationDuration);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        rangedDamage = Mathf.Max(0f, rangedDamage);
        rangedProjectileSpeed = Mathf.Max(0.1f, rangedProjectileSpeed);
        rangedTravelDistance = Mathf.Max(0.1f, rangedTravelDistance);
        rangedVerticalTolerance = Mathf.Max(0.1f, rangedVerticalTolerance);
        rangedAnimationDuration = Mathf.Max(0.1f, rangedAnimationDuration);
        rangedWindup = Mathf.Clamp(rangedWindup, 0f, rangedAnimationDuration);
        rangedCooldown = Mathf.Max(0f, rangedCooldown);
        jumpRange = Mathf.Max(0.1f, jumpRange);
        jumpVerticalTolerance = Mathf.Max(0.1f, jumpVerticalTolerance);
        jumpHeight = Mathf.Max(0.1f, jumpHeight);
        jumpHangTime = Mathf.Max(0f, jumpHangTime);
        jumpLandingTime = Mathf.Max(0.02f, jumpLandingTime);
        jumpApexTime = Mathf.Clamp(jumpApexTime, 0.01f, jumpLandingTime - 0.01f);
        jumpAnimationDuration = Mathf.Max(jumpLandingTime, jumpAnimationDuration);
        jumpCooldown = Mathf.Max(0f, jumpCooldown);
        slamWidth = Mathf.Max(0.1f, slamWidth);
        slamVerticalTolerance = Mathf.Max(0.1f, slamVerticalTolerance);
        slamDamageMultiplier = Mathf.Max(1f, slamDamageMultiplier);
        warningHeight = Mathf.Max(0.02f, warningHeight);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(1f, 0.25f, 0.15f, 0.85f);
        Gizmos.DrawWireCube(transform.position,
            new Vector3(attackRange * 2f, verticalTolerance * 2f, 0f));
        Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
        Gizmos.DrawWireCube(transform.position,
            new Vector3(slamWidth, slamVerticalTolerance * 2f, 0f));
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
            attackWarning.startColor = attackWarning.endColor = new Color(1f, 0.2f, 0.1f, 0.85f);
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

    private void ShowJumpWarning()
    {
        if (jumpWarning == null)
        {
            jumpWarningSprite = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            GameObject warning = new GameObject("Jump Slam Range", typeof(SpriteRenderer));
            jumpWarning = warning.GetComponent<SpriteRenderer>();
            jumpWarning.sprite = jumpWarningSprite;
            jumpWarning.sortingLayerID = spriteRenderer.sortingLayerID;
            jumpWarning.sortingOrder = spriteRenderer.sortingOrder + 1;
        }

        jumpWarning.transform.position = new Vector3(jumpLandingPosition.x, jumpWarningY, transform.position.z);
        jumpWarning.transform.localScale = new Vector3(slamWidth, warningHeight, 1f);
        jumpWarning.color = new Color(1f, 0.05f, 0.02f, warningOpacity);
        jumpWarning.enabled = true;
    }

    private void HideWarnings()
    {
        if (attackWarning != null)
            attackWarning.enabled = false;
        if (jumpWarning != null)
            jumpWarning.enabled = false;
    }

    private void RestoreAnimatorSpeed()
    {
        if (animator != null)
            animator.speed = animatorSpeedBeforeJump;
    }

    private void OnDestroy()
    {
        if (attackWarningMaterial != null)
            Destroy(attackWarningMaterial);
        if (jumpWarningSprite != null)
            Destroy(jumpWarningSprite);
        if (jumpWarning != null)
            Destroy(jumpWarning.gameObject);
    }
}
