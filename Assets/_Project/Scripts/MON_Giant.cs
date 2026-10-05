using Unity.Cinemachine;
using UnityEngine;
using EasternFantasy.Player;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer), typeof(CinemachineImpulseSource))]
public class MON_Giant : EnemyController
{
    private const int FootstepChannel = 2;

    [Header("Footstep Camera Shake")]
    [SerializeField, Min(0f)] private float footstepShakeStrength = 0.06f;
    [SerializeField, Min(0.01f)] private float footstepShakeDuration = 0.18f;
    [Tooltip("The shake fades out as the main camera gets farther from the giant.")]
    [SerializeField, Min(0.1f)] private float footstepShakeRange = 35f;

    private CinemachineImpulseSource footstepImpulse;
    private CinemachineImpulseListener impulseListener;
    private float nextFootstepTime;

    public enum AttackPattern { None, Smash, Throw, SprintWarning, Sprint }
    [Header("Attack Patterns")]
    [SerializeField, Min(0f)] private float normalAttackDamage = 20f;
    [SerializeField, Min(0f)] private float sprintDamage = 35f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 2f;
    [SerializeField, Range(0.05f, 1f)] private float smashMapFraction = 1f / 3f;
    [SerializeField, Min(0.1f)] private float smashHeight = 3f;
    [SerializeField, Min(0.1f)] private float sprintWarningDuration = 2.2f;
    [SerializeField, Min(1f)] private float sprintSpeed = 32f;
    [SerializeField, Min(1f)] private float sprintRange = 25f;
    [SerializeField, Min(1f)] private float rockSpeed = 22f;
    [SerializeField] private Vector2 rockHandOffset = new Vector2(2.3f, 2.2f);
    [SerializeField] private Collider2D arenaBounds;
    [SerializeField] private Sprite[] dustFrames;
    [SerializeField] private Sprite[] rockFrames;
    [SerializeField, Min(0f)] private float smashShakeStrength = 0.85f;
    [SerializeField, Min(0.01f)] private float smashShakeDuration = 0.3f;

    public AttackPattern CurrentAttack { get; private set; }
    public override float ContactDamage => CurrentAttack == AttackPattern.None ? base.ContactDamage : 0f;
    protected override bool ControlsAttackMovement => true;
    protected override string DeathAnimationState => "GiantDefeated";
    private float nextAttackTime, phaseEnds, attackDirection, sprintEndX;
    private int nextPattern;
    private bool impactFired, sprintHit;
    private GiantAttackVisual warning;
    private readonly List<Collider2D> ignoredPlayerColliders = new List<Collider2D>();

    private void Start()
    {
        footstepImpulse = GetComponent<CinemachineImpulseSource>();
        footstepImpulse.ImpulseDefinition.ImpulseChannel = FootstepChannel;
        footstepImpulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        footstepImpulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        footstepImpulse.ImpulseDefinition.ImpulseDuration = footstepShakeDuration;
        nextAttackTime = Time.time + attackCooldown;
    }

    // Called by the two foot-contact events in GiantWalk.
    public void GiantFootstep()
    {
        if (!Application.isPlaying || Time.timeScale <= 0f || HP <= 0f || isKnockback
            || rb == null || Mathf.Abs(rb.linearVelocity.x) < 0.05f
            || Mathf.Abs(rb.linearVelocity.y) > 0.3f || Time.time < nextFootstepTime)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null || footstepImpulse == null)
            return;

        float distance = Vector2.Distance(mainCamera.transform.position, transform.position);
        float strength = footstepShakeStrength * Mathf.Clamp01(1f - distance / footstepShakeRange);
        if (strength <= 0f)
            return;

        if (impulseListener == null)
        {
            CinemachineCamera virtualCamera = FindFirstObjectByType<CinemachineCamera>();
            if (virtualCamera == null)
                return;

            impulseListener = virtualCamera.GetComponent<CinemachineImpulseListener>();
            if (impulseListener == null)
            {
                impulseListener = virtualCamera.gameObject.AddComponent<CinemachineImpulseListener>();
                impulseListener.Gain = 1f;
                impulseListener.Use2DDistance = true;
            }
            impulseListener.ChannelMask |= FootstepChannel;
        }

        nextFootstepTime = Time.time + 0.12f;
        footstepImpulse.ImpulseDefinition.ImpulseDuration = footstepShakeDuration;
        footstepImpulse.GenerateImpulseAtPositionWithVelocity(transform.position, Vector3.down * strength);
    }

    protected override void MonsterAbility()
    {
        if (CurrentAttack != AttackPattern.None)
        {
            if (!HasLivingPlayer || HP <= 0f)
            {
                FinishAttack();
                return;
            }
            if (CurrentAttack == AttackPattern.SprintWarning && Time.time >= phaseEnds)
            {
                if (warning != null) Destroy(warning.gameObject);
                CurrentAttack = AttackPattern.Sprint;
                animator.Play("GiantSprint", 0, 0f);
                phaseEnds = Time.time + Mathf.Abs(sprintEndX - rb.position.x) / sprintSpeed + 0.2f;
            }
            else if (Time.time >= phaseEnds)
                FinishAttack();
            return;
        }
        if (Time.time < nextAttackTime || !CanDetectLivingPlayer() || isKnockback
            || Mathf.Abs(rb.linearVelocity.y) > 0.3f)
            return;
        int pattern = nextPattern;
        // Skip an offscreen smash without stalling the other attack patterns.
        if (pattern == 0 && !IsInPlayerCamera()) pattern = 1;
        BeginAttack(pattern);
        nextPattern = (pattern + 1) % 3;
    }

    protected override bool CanUseAbility() => CurrentAttack != AttackPattern.None || base.CanUseAbility();

    private void BeginAttack(int pattern)
    {
        if (pattern == 0 && !IsInPlayerCamera()) return;
        attackDirection = playerEntity.transform.position.x >= transform.position.x ? 1f : -1f;
        // Use the same facing convention as EnemyController movement.
        // The giant's SpriteRenderer already has Flip X enabled in the scene.
        Vector3 scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x) * attackDirection;
        transform.localScale = scale;
        isAttacking = true;
        impactFired = sprintHit = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        animator.SetBool("isMoving", false);
        if (pattern == 0)
        {
            CurrentAttack = AttackPattern.Smash;
            IgnorePlayerBodyCollisions();
            PlayAttack("GiantSmash");
        }
        else if (pattern == 1)
        {
            CurrentAttack = AttackPattern.Throw;
            PlayAttack("GiantThrow");
        }
        else
        {
            CurrentAttack = AttackPattern.SprintWarning;
            // Keep the pair ignored through the charge: the swept attack supplies damage
            // and the player's body must not stop the giant's movement.
            IgnorePlayerBodyCollisions();
            animator.Play("GiantIdel", 0, 0f);
            float end = rb.position.x + attackDirection * sprintRange;
            if (arenaBounds != null)
                end = Mathf.Clamp(end, arenaBounds.bounds.min.x + capsuleCollider2D.bounds.extents.x,
                    arenaBounds.bounds.max.x - capsuleCollider2D.bounds.extents.x);
            sprintEndX = end;
            Bounds body = capsuleCollider2D.bounds;
            warning = GiantAttackVisual.CreateWarning(new Vector2((body.center.x + end) * 0.5f, body.center.y),
                new Vector2(Mathf.Abs(end - body.center.x) + body.size.x, body.size.y), attackDirection, spriteRenderer);
            phaseEnds = Time.time + sprintWarningDuration;
        }
    }

    private bool IsInPlayerCamera()
    {
        Camera camera = Camera.main;
        if (camera == null || (camera.cullingMask & (1 << gameObject.layer)) == 0) return false;
        Vector3 viewport = camera.WorldToViewportPoint(spriteRenderer.bounds.center);
        return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f
            && viewport.y >= 0f && viewport.y <= 1f;
    }

    private void IgnorePlayerBodyCollisions()
    {
        foreach (Collider2D c in playerEntity.GetComponentsInChildren<Collider2D>())
        {
            if (!Physics2D.GetIgnoreCollision(capsuleCollider2D, c))
            {
                Physics2D.IgnoreCollision(capsuleCollider2D, c, true);
                ignoredPlayerColliders.Add(c);
            }
        }
    }

    public override void ApplyKnockback(float directionX)
    {
        // Attacks keep their animation and committed direction when the giant is hit.
        if (CurrentAttack != AttackPattern.None) return;
        base.ApplyKnockback(directionX);
    }

    private void PlayAttack(string state)
    {
        animator.Play(state, 0, 0f);
        float duration = 2f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name == state) duration = clip.length;
        phaseEnds = Time.time + duration + 0.05f;
    }

    // Animation event on the first ground-contact frame.
    public void GiantSmashImpact()
    {
        if (CurrentAttack != AttackPattern.Smash || impactFired || HP <= 0f || !HasLivingPlayer) return;
        impactFired = true;
        float range = (arenaBounds != null ? arenaBounds.bounds.size.x : 60f) * smashMapFraction;
        float ground = capsuleCollider2D.bounds.min.y;
        Vector2 center = new Vector2(transform.position.x + attackDirection * range * 0.5f, ground + smashHeight * 0.5f);
        HashSet<PlayerEntity> hit = new HashSet<PlayerEntity>();
        foreach (Collider2D c in Physics2D.OverlapBoxAll(center, new Vector2(range, smashHeight), 0f, _playerLayer))
        {
            PlayerEntity entity = c.GetComponentInParent<PlayerEntity>();
            if (entity != null && hit.Add(entity)) DamagePlayer(entity, normalAttackDamage);
        }
        for (float distance = 1.5f; distance < range; distance += 3f)
        {
            Vector2 point = new Vector2(transform.position.x + attackDirection * distance, ground);
            GiantAttackVisual.CreateDust(point, dustFrames, spriteRenderer, distance * 0.006f);
        }
        EnsureImpulseListener();
        footstepImpulse.ImpulseDefinition.ImpulseDuration = smashShakeDuration;
        footstepImpulse.GenerateImpulseAtPositionWithVelocity(transform.position,
            new Vector3(0.35f * attackDirection, -1f, 0f).normalized * smashShakeStrength);
    }

    // Animation event on the frame where the rock leaves the hands.
    public void GiantThrowRelease()
    {
        if (CurrentAttack != AttackPattern.Throw || impactFired || HP <= 0f || !HasLivingPlayer) return;
        impactFired = true;
        Vector2 origin = (Vector2)transform.position + new Vector2(rockHandOffset.x * attackDirection, rockHandOffset.y);
        Vector2 aim = (Vector2)playerEntity.transform.position - origin;
        if (aim.x * attackDirection < 0.1f) aim = new Vector2(attackDirection, 0f);
        GiantRockProjectile.Create(origin, aim.normalized * rockSpeed, normalAttackDamage,
            rockFrames, spriteRenderer, _playerLayer, gameObject);
    }

    protected override void AttackFixedUpdate()
    {
        if (CurrentAttack != AttackPattern.Sprint)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        float remaining = (sprintEndX - rb.position.x) * attackDirection;
        if (remaining <= 0.05f) { FinishAttack(); return; }
        float step = Mathf.Min(sprintSpeed * Time.fixedDeltaTime, remaining);
        Bounds body = capsuleCollider2D.bounds;
        // Sweep the whole body so a fast charge cannot skip a player between physics frames.
        Vector2 center = (Vector2)body.center + Vector2.right * attackDirection * step * 0.5f;
        foreach (Collider2D c in Physics2D.OverlapBoxAll(center,
            new Vector2(body.size.x + step, body.size.y), 0f, _playerLayer))
        {
            PlayerEntity entity = c.GetComponentInParent<PlayerEntity>();
            if (entity != null && !sprintHit)
            {
                sprintHit = true;
                DamagePlayer(entity, sprintDamage);
            }
        }
        rb.linearVelocity = new Vector2(attackDirection * step / Time.fixedDeltaTime, rb.linearVelocity.y);
    }

    private void DamagePlayer(PlayerEntity entity, float damage)
    {
        float before = entity.HP;
        entity.TakeDamage(damage);
        if (!entity.IsDead && entity.HP < before) entity.ApplyKnockback(attackDirection);
    }

    private void EnsureImpulseListener()
    {
        if (impulseListener == null)
        {
            CinemachineCamera camera = FindFirstObjectByType<CinemachineCamera>();
            if (camera == null) return;
            impulseListener = camera.GetComponent<CinemachineImpulseListener>();
            if (impulseListener == null) impulseListener = camera.gameObject.AddComponent<CinemachineImpulseListener>();
            impulseListener.Use2DDistance = true;
        }
        impulseListener.ChannelMask |= FootstepChannel;
    }

    private void FinishAttack()
    {
        foreach (Collider2D c in ignoredPlayerColliders)
            if (c != null && capsuleCollider2D != null) Physics2D.IgnoreCollision(capsuleCollider2D, c, false);
        ignoredPlayerColliders.Clear();
        if (warning != null) Destroy(warning.gameObject);
        CurrentAttack = AttackPattern.None;
        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (HP > 0f && animator != null && animator.isActiveAndEnabled)
            animator.Play("GiantIdel", 0, 0f);
    }

    protected override void OnDefeated()
    {
        FinishAttack();
        base.OnDefeated();
    }

    protected override void OnDisable()
    {
        if (CurrentAttack != AttackPattern.None) FinishAttack();
        base.OnDisable();
    }
}
