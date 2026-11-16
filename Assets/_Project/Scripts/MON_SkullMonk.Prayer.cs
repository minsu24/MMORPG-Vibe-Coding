using System.Collections.Generic;
using UnityEngine;

public sealed partial class MON_SkullMonk
{
    [Header("Central prayer eruptions")]
    [SerializeField] private Sprite[] prayerExplosionFrames;
    [SerializeField] private Sprite[] prayerDissolveFrames;
    [SerializeField] private Collider2D prayerLeftWall, prayerRightWall;
    [SerializeField, Min(5f)] private float prayerCooldown = 20f;
    [SerializeField, Min(.5f)] private float prayerWarningSeconds = .95f;
    [SerializeField, Min(8f)] private float prayerJumpHeight = 16f;
    [SerializeField, Min(8f)] private float prayerExplosionHeight = 18f;
    [SerializeField, Min(.1f)] private float prayerDamageMultiplier = 1.5f;
    private enum PrayerStage { None, Jumping, Preparing, Channeling, Recovering }
    private PrayerStage prayerStage;
    private float nextPrayerAt, prayerAge, prayerGravity;
    private Vector2 prayerJumpStart, prayerLanding;
    private bool prayerLanded;
    public const float PrayerTakeoffTime = .12f;
    public const float PrayerTransferTime = .34f;
    public const float PrayerDropTime = .40f;
    public const float PrayerLandTime = .56f;
    public const float PrayerSettleTime = 1.12f;
    private readonly List<SkullMonkPrayerBlast> prayerBlasts = new List<SkullMonkPrayerBlast>();
    private readonly List<Collider2D> prayerIgnoredColliders = new List<Collider2D>();
    public bool IsPrayerAttacking => prayerStage != PrayerStage.None;
    public int PrayerExplosionCount { get; private set; }
    public Vector2 PrayerArenaCenter => new Vector2((PrayerMinX + PrayerMaxX) * .5f, waveFloor.bounds.max.y);
    private float PrayerMinX => prayerLeftWall != null ? prayerLeftWall.bounds.max.x : waveArenaBounds.bounds.min.x;
    private float PrayerMaxX => prayerRightWall != null ? prayerRightWall.bounds.min.x : waveArenaBounds.bounds.max.x;

    private bool CanStartPrayerPattern()
    {
        return !IsPrayerAttacking && Time.time >= nextPrayerAt && HasLivingPlayer && HP > 0f
            && waveFloor != null && waveArenaBounds != null && prayerExplosionFrames != null
            && prayerExplosionFrames.Length == 8 && prayerDissolveFrames != null && prayerDissolveFrames.Length == 5
            && PrayerMaxX - PrayerMinX >= 8f
            && Mathf.Abs(rb.linearVelocity.y) < .4f
            && Mathf.Abs(capsuleCollider2D.bounds.min.y - waveFloor.bounds.max.y) < .4f;
    }

    private void StartPrayerPattern()
    {
        prayerStage = PrayerStage.Jumping; prayerAge = 0; prayerLanded = false;
        isAttacking = true; emitted = true; PrayerExplosionCount = 0;
        prayerJumpStart = rb.position;
        float footOffset = capsuleCollider2D.bounds.min.y - transform.position.y;
        prayerLanding = new Vector2(PrayerArenaCenter.x, waveFloor.bounds.max.y - footOffset + .015f);
        prayerGravity = rb.gravityScale; rb.gravityScale = 0; rb.linearVelocity = Vector2.zero;
        float direction = Mathf.Abs(prayerLanding.x - rb.position.x) > .05f
            ? Mathf.Sign(prayerLanding.x - rb.position.x) : Mathf.Sign(playerEntity.transform.position.x - transform.position.x);
        var scale = transform.localScale; scale.x = -Mathf.Abs(scale.x) * (direction == 0 ? 1 : direction);
        transform.localScale = scale;
        foreach (var collider in playerEntity.GetComponentsInChildren<Collider2D>())
        {
            if (collider.isTrigger || Physics2D.GetIgnoreCollision(capsuleCollider2D, collider)) continue;
            Physics2D.IgnoreCollision(capsuleCollider2D, collider, true); prayerIgnoredColliders.Add(collider);
        }
        animator.SetBool("isMoving", false); animator.Play("SkullMonkPrayerJump", 0, 0);
        endsAt = Time.time + 12f;
    }

    // Rise vertically at the takeoff point. Reposition only above the visible
    // arena, then plunge vertically at the center; no visible diagonal glide.
    public static Vector2 EvaluatePrayerLeap(Vector2 start, Vector2 landing, float age, float height)
    {
        if (age <= PrayerTakeoffTime) return start;
        float apex = Mathf.Max(start.y, landing.y) + Mathf.Max(0, height);
        if (age < PrayerTransferTime)
        {
            float rise = Mathf.Clamp01((age - PrayerTakeoffTime) / (PrayerTransferTime - PrayerTakeoffTime));
            return new Vector2(start.x, Mathf.Lerp(start.y, apex, 1f - Mathf.Pow(1f - rise, 2f)));
        }
        if (age < PrayerDropTime) return new Vector2(landing.x, apex);
        float fall = Mathf.Clamp01((age - PrayerDropTime) / (PrayerLandTime - PrayerDropTime));
        return new Vector2(landing.x, Mathf.Lerp(apex, landing.y, fall * fall));
    }

    private void PrayerFixedUpdate()
    {
        rb.linearVelocity = Vector2.zero;
        if (prayerStage == PrayerStage.Jumping && !prayerLanded)
            // Explicit scripted motion avoids physics translation-speed caps
            // turning the high-speed drop into a slow fall or diagonal catch-up.
            rb.position = EvaluatePrayerLeap(prayerJumpStart, prayerLanding, prayerAge, prayerJumpHeight);
    }

    private void TickPrayerPattern(float deltaTime)
    {
        prayerAge += deltaTime;
        if (prayerStage == PrayerStage.Jumping)
        {
            if (!prayerLanded && prayerAge >= PrayerLandTime)
            {
                prayerLanded = true; rb.position = prayerLanding; rb.gravityScale = prayerGravity;
                GiantAttackVisual.CreateDust(PrayerArenaCenter, staffSmokeFrames, spriteRenderer, 0, 2f);
                CombatImpactFeedback.Play(PrayerArenaCenter, true, null, 0f);
            }
            if (prayerAge >= PrayerSettleTime)
            {
                prayerStage = PrayerStage.Preparing; prayerAge = 0;
                var scale = transform.localScale;
                scale.x = -Mathf.Abs(scale.x) * (playerEntity.transform.position.x >= transform.position.x ? 1 : -1);
                transform.localScale = scale;
                animator.Play("SkullMonkPrayerPrepare", 0, 0);
            }
        }
        else if (prayerStage == PrayerStage.Preparing && prayerAge >= .74f)
        {
            prayerStage = PrayerStage.Channeling; prayerAge = 0;
            animator.Play("SkullMonkPrayerAura", 0, 0);
            SpawnPrayerExplosions();
        }
        else if (prayerStage == PrayerStage.Channeling)
        {
            bool finished = true;
            foreach (var blast in prayerBlasts) if (blast != null && !blast.IsFinished) { finished = false; break; }
            if (finished)
            {
                prayerStage = PrayerStage.Recovering; prayerAge = 0;
                animator.Play("SkullMonkPrayerRecover", 0, 0);
            }
        }
        else if (prayerStage == PrayerStage.Recovering && prayerAge >= .65f) FinishAttack();
    }

    public static int CalculatePrayerExplosionCount(float arenaWidth)
        => Mathf.Clamp(2 * Mathf.FloorToInt(Mathf.Max(1, arenaWidth - 2.4f) / 8.4f), 2, 12);

    private void SpawnPrayerExplosions()
    {
        float min = PrayerMinX + 1.2f, max = PrayerMaxX - 1.2f;
        PrayerExplosionCount = CalculatePrayerExplosionCount(PrayerMaxX - PrayerMinX);
        float spacing = (max - min) / PrayerExplosionCount;
        float width = Mathf.Min(3f, spacing * .65f);
        for (int i = 0; i < PrayerExplosionCount; i++)
            prayerBlasts.Add(SkullMonkPrayerBlast.Create(this,
                new Vector2(min + (i + .5f) * spacing, waveFloor.bounds.max.y), width, prayerExplosionHeight,
                prayerWarningSeconds, Attack_Power * prayerDamageMultiplier, _playerLayer,
                prayerExplosionFrames, prayerDissolveFrames, spriteRenderer));
    }

    private void RestorePrayerCollisions()
    {
        foreach (var collider in prayerIgnoredColliders)
            if (capsuleCollider2D != null && collider != null) Physics2D.IgnoreCollision(capsuleCollider2D, collider, false);
        prayerIgnoredColliders.Clear();
    }

    private void EndPrayerPattern()
    {
        if (!IsPrayerAttacking) return;
        if (rb != null)
        {
            rb.gravityScale = prayerGravity;
            rb.linearVelocity = Vector2.zero;
        }
        RestorePrayerCollisions();
        foreach (var blast in prayerBlasts) if (blast != null) blast.Cancel();
        prayerBlasts.Clear(); prayerStage = PrayerStage.None;
        nextPrayerAt = Time.time + prayerCooldown;
    }
}
