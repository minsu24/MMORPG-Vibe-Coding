using EasternFantasy.Player;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed partial class MON_SkullMonk : PersistentBossController
{
    [SerializeField, Min(.1f)] private float attackRange = 3.8f;
    [SerializeField, Min(.1f)] private float attackVerticalRange = 2f;
    [SerializeField, Min(.1f)] private float attackDownwardRange = 3.5f;
    [SerializeField, Min(0f)] private float attackCooldown = 1.8f;
    [SerializeField] private Sprite[] thrustEffectFrames;
    [SerializeField] private Vector2 thrustOrigin = new Vector2(2.7f, .45f);
    [SerializeField, Min(0f)] private float thrustTravel = .95f;
    [SerializeField] private Vector2 hitOffset = new Vector2(3.5f, -.85f);
    [SerializeField] private Vector2 hitSize = new Vector2(5.2f, 4.4f);
    private readonly Collider2D[] hits = new Collider2D[16];
    private float nextAttackAt, endsAt, attackDirection;
    private bool emitted;
    private GameObject effect;
    private SpriteRenderer effectRenderer;
    private float effectStarted;
    private bool stepPlanted;
    public bool IsAttacking => isAttacking;
    public override float ContactDamage => 0f;
    protected override bool CanUseAbility() => IsCombatReady && (isAttacking || CanStartGiantStaffPattern() || CanStartPrayerPattern() || base.CanUseAbility());
    protected override float LocomotionSpeedMultiplier
    {
        get
        {
            if (animator == null || isAttacking || isKnockback) return 1f;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            return state.IsName("SkullMonkWalk") ? EvaluateStaggerStride(state.normalizedTime) : 1f;
        }
    }

    // Slow dragging step, then a quick catch-up step; average multiplier is approximately one.
    // Reading the walk state's phase keeps real movement locked to the asymmetric sprite timing.
    public static float EvaluateStaggerStride(float normalizedTime)
    {
        float phase = Mathf.Repeat(normalizedTime, 1f);
        return phase < .6f
            ? .65f - .15f * Mathf.Sin(Mathf.PI * phase / .6f)
            : .65f + 1.6f * Mathf.Sin(Mathf.PI * (phase - .6f) / .4f);
    }

    protected override void MonsterAbility()
    {
        if (!IsCombatReady) return;
        if (isAttacking)
        {
            if (!HasLivingPlayer || !CanDealContactDamage || isKnockback || Time.time >= endsAt)
                FinishAttack();
            else if (IsGiantStaffAttacking) TickGiantStaffPattern(Time.deltaTime);
            else if (IsPrayerAttacking) TickPrayerPattern(Time.deltaTime);
            else if (IsDashing) TickDash(Time.deltaTime);
            else if (IsRingAttacking) TickRingPattern(Time.deltaTime);
            return;
        }
        if (!HasLivingPlayer || !CanDealContactDamage || isKnockback || Time.time < nextAttackAt) return;
        bool waveReady = CanStartWavePattern();
        bool ringReady = CanStartRingPattern();
        if (CanStartGiantStaffPattern() && (!waveReady || nextGiantStaffAt <= nextWaveAt)
            && (!ringReady || nextGiantStaffAt <= nextRingAt)
            && (!CanStartPrayerPattern() || nextGiantStaffAt <= nextPrayerAt)) { StartGiantStaffPattern(); return; }
        if (CanStartPrayerPattern() && (!waveReady || nextPrayerAt <= nextWaveAt)
            && (!ringReady || nextPrayerAt <= nextRingAt)) { StartPrayerPattern(); return; }
        if (ringReady && (!waveReady || nextRingAt <= nextWaveAt)) { StartRingPattern(); return; }
        if (waveReady) { StartWavePattern(); return; }
        Vector2 delta = playerEntity.transform.position - transform.position;
        if (delta.y > attackVerticalRange || delta.y < -attackDownwardRange) return;
        if (CanStartDash(delta))
        {
            StartMeleeApproach();
            return;
        }
        if (Mathf.Abs(delta.x) <= attackRange) StartMeleeAttack();
    }

    private void StartMeleeAttack()
    {
        Vector2 delta = playerEntity.transform.position - transform.position;
        attackDirection = delta.x >= 0 ? 1f : -1f;
        var scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x) * attackDirection;
        transform.localScale = scale;
        isAttacking = true; emitted = false; stepPlanted = false;
        endsAt = Time.time + 1.5f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        animator.SetBool("isMoving", false);
        animator.Play("SkullMonkAttack", 0, 0f);
    }

    public void SkullMonkPlantStep()
    {
        if (!isAttacking || stepPlanted || isKnockback || !CanDealContactDamage) return;
        stepPlanted = true;
        GiantAudioCue.Play(gameObject, "Audio/GiantFootstepSource", .12f);
        GiantAudioCue.Play(gameObject, "Audio/GoblinSwing", .38f);
    }

    // Fired exactly once at the fully extended staff frame, after the 60 ms drive.
    public void SkullMonkThrust()
    {
        if (!isAttacking || emitted || isKnockback || !CanDealContactDamage || !HasLivingPlayer) return;
        emitted = true;
        ClearEffect();
        if (thrustEffectFrames != null && thrustEffectFrames.Length > 0)
        {
            effect = new GameObject("SkullMonk Thrust Effect");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(effect, gameObject.scene);
            effect.transform.SetParent(transform, false);
            // Actor scale faces left; effect source points right.
            effect.transform.localPosition = new Vector3(-thrustOrigin.x, thrustOrigin.y, 0f);
            effect.transform.localScale = new Vector3(-1f, 1f, 1f);
            var visual = effect.AddComponent<SpriteRenderer>();
            effectRenderer = visual;
            visual.sharedMaterial = spriteRenderer.sharedMaterial;
            visual.sortingLayerID = spriteRenderer.sortingLayerID;
            visual.sortingOrder = spriteRenderer.sortingOrder + 1;
            visual.color = new Color(1f, 1f, 1f, .8f);
            visual.sprite = thrustEffectFrames[0];
            effectStarted = Time.time;
            UpdateThrustEffect(0f);
        }
        Vector2 scale = new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        Vector2 center = (Vector2)transform.position + Vector2.Scale(new Vector2(hitOffset.x * attackDirection, hitOffset.y), scale);
        var filter = new ContactFilter2D(); filter.SetLayerMask(_playerLayer); filter.useTriggers = false;
        int count = gameObject.scene.GetPhysicsScene2D().OverlapBox(center, Vector2.Scale(hitSize, scale), 0f, filter, hits);
        for (int i = 0; i < count; i++)
        {
            var victim = hits[i].GetComponentInParent<PlayerEntity>();
            if (victim != playerEntity || victim.IsDead) continue;
            uint before = victim.HitReactionVersion;
            victim.TakeDamage(Attack_Power);
            if (victim.HitReactionVersion != before)
            {
                // Shared feedback owns the 40ms hit stop and small camera impulse, and respects menus.
                CombatImpactFeedback.Play(victim.transform.position, false, null, 0f);
                if (!victim.IsDead) victim.ApplyKnockback(attackDirection);
            }
            break;
        }
    }

    public void SkullMonkAttackFinished() { if (isAttacking) FinishAttack(); }
    private void FinishAttack()
    {
        bool active = isAttacking;
        EndDash();
        EndPrayerPattern();
        EndWavePattern();
        EndRingPattern();
        EndGiantStaffPattern();
        isAttacking = false; emitted = true;
        ClearEffect();
        if (!active) return;
        nextAttackAt = Time.time + attackCooldown;
        if (HP > 0 && animator != null && animator.isActiveAndEnabled) animator.Play("SkullMonkIdle", 0, 0f);
    }
    private void LateUpdate()
    {
        // All supplied monk art faces right; shared enemy locomotion expects left-facing art.
        if (spriteRenderer != null) spriteRenderer.flipX = true;
        UpdateThrustEffect(Time.time - effectStarted);
        UpdateStaffRings();
    }
    // Shared by runtime and the editor preview; the burst opens immediately, then leaves a fading tail.
    public static void EvaluateThrustBurst(float age, out int frame, out float travel, out Vector2 size, out Color color)
    {
        frame = age < .025f ? 0 : age < .07f ? 2 : age < .12f ? 3 : 1;
        float t = Mathf.Clamp01(age / .12f);
        travel = 1f - Mathf.Pow(1f - t, 3f);
        size = new Vector2(1.05f, Mathf.Lerp(.55f, 1f, Mathf.Clamp01(age / .045f)));
        float brightness = Mathf.Lerp(1.5f, 1f, Mathf.Clamp01(age / .07f));
        float alpha = .95f * (1f - Mathf.Clamp01((age - .065f) / .155f));
        color = new Color(brightness, brightness * .97f, brightness * .9f, alpha);
    }
    private void UpdateThrustEffect(float age)
    {
        if (effect == null || effectRenderer == null) return;
        if (age >= .22f) { ClearEffect(); return; }
        EvaluateThrustBurst(age, out int frame, out float travel, out Vector2 size, out Color color);
        effect.transform.localPosition = new Vector3(-(thrustOrigin.x + thrustTravel * travel), thrustOrigin.y, 0f);
        effect.transform.localScale = new Vector3(-size.x, size.y, 1f);
        effectRenderer.sprite = thrustEffectFrames[Mathf.Min(frame, thrustEffectFrames.Length - 1)];
        effectRenderer.color = color;
    }
    private void ClearEffect()
    {
        if (effect != null) { effect.SetActive(false); Destroy(effect); }
        effect = null; effectRenderer = null;
    }
    public override void ApplyKnockback(float directionX)
    {
        if (!IsCombatReady) return;
        if (IsWaveAttacking || IsRingAttacking || IsPrayerAttacking || IsGiantStaffAttacking) return;
        FinishAttack(); base.ApplyKnockback(directionX);
    }
    protected override void OnDefeated() { FinishAttack(); CancelGroundWaves(); CancelStaffRings(); base.OnDefeated(); }
    protected override void OnDisable() { UnbindIntroDialogue(); FinishAttack(); CancelGroundWaves(); CancelStaffRings(); base.OnDisable(); }
}
