using UnityEngine;

public sealed partial class MON_SkullMonk
{
    [Header("Giant staff judgment")]
    [SerializeField] private Sprite[] giantStaffFallFrames;
    [SerializeField] private Sprite[] giantStaffImpactFrames;
    [SerializeField] private Sprite giantStaffHighResolutionSprite;
    [SerializeField, Min(.1f)] private float giantStaffImpactHeight = 2.4f;
    [SerializeField, Min(5f)] private float giantStaffCooldown = 24f;
    [SerializeField, Min(3f)] private float giantStaffWarningSeconds = 3.5f;
    [SerializeField, Min(3f)] private float giantStaffEdgeSafety = 5f;
    private enum GiantStaffStage { None, Throwing, Warning, Commanding, Waiting, Regenerating }
    private GiantStaffStage giantStaffStage;
    private float giantStaffAge, nextGiantStaffAt;
    private SkullMonkGiantStaff giantStaff;
    private readonly System.Collections.Generic.List<Collider2D> giantStaffIgnoredColliders = new System.Collections.Generic.List<Collider2D>();
    public bool IsGiantStaffAttacking => giantStaffStage != GiantStaffStage.None;
    public const float GiantStaffReleaseTime = .50f;
    public const float GiantStaffThrowTime = .90f;
    public const float GiantStaffCommandTime = .82f;
    public const float GiantStaffCommandDropTime = .46f;
    public const float GiantStaffRegenerateTime = 1.25f;

    private bool CanStartGiantStaffPattern()
    {
        return !IsGiantStaffAttacking && Time.time >= nextGiantStaffAt && HasLivingPlayer && HP > 0f
            && waveFloor != null && waveArenaBounds != null && PrayerMaxX - PrayerMinX >= 16f
            && giantStaffFallFrames != null && giantStaffFallFrames.Length == 4
            && giantStaffImpactFrames != null && giantStaffImpactFrames.Length == 8
            && animator != null && animator.HasState(0, Animator.StringToHash("SkullMonkGiantStaffThrow"))
            && Mathf.Abs(rb.linearVelocity.y) < .4f
            && Mathf.Abs(capsuleCollider2D.bounds.min.y - waveFloor.bounds.max.y) < .4f;
    }

    private void StartGiantStaffPattern()
    {
        float direction = playerEntity.transform.position.x >= transform.position.x ? 1f : -1f;
        var scale = transform.localScale; scale.x = -Mathf.Abs(scale.x) * direction; transform.localScale = scale;
        giantStaffStage = GiantStaffStage.Throwing; giantStaffAge = 0f;
        isAttacking = true; emitted = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        animator.SetBool("isMoving", false);
        animator.Play("SkullMonkGiantStaffThrow", 0, 0f);
        // The stationary boss must not body-block a run toward either safe edge.
        foreach (var collider in playerEntity.GetComponentsInChildren<Collider2D>())
        {
            if (collider.isTrigger || Physics2D.GetIgnoreCollision(capsuleCollider2D, collider)) continue;
            Physics2D.IgnoreCollision(capsuleCollider2D, collider, true);
            giantStaffIgnoredColliders.Add(collider);
        }
        float width = PrayerMaxX - PrayerMinX;
        float safety = Mathf.Clamp(giantStaffEdgeSafety, 3f, width * .25f);
        // At base running speed, even a player at the arena center can clear the footprint.
        float warning = Mathf.Max(giantStaffWarningSeconds, (width * .5f - safety + 1.2f) / 6f + .75f);
        giantStaff = SkullMonkGiantStaff.Create(this, PrayerArenaCenter, width - safety * 2f, warning,
            _playerLayer, giantStaffFallFrames, giantStaffImpactFrames, spriteRenderer,
            (Vector2)transform.position + new Vector2(direction * 1.1f, 1.7f), giantStaffHighResolutionSprite,
            giantStaffImpactHeight);
        endsAt = Time.time + warning + SkullMonkGiantStaff.ImpactLifetime + 4f;
    }

    private void TickGiantStaffPattern(float deltaTime)
    {
        giantStaffAge += Mathf.Max(0f, deltaTime);
        if (giantStaff == null || giantStaff.IsFinished) { FinishAttack(); return; }
        if (giantStaffStage == GiantStaffStage.Throwing)
        {
            if (giantStaffAge >= GiantStaffReleaseTime) giantStaff.Release();
            if (giantStaffAge >= GiantStaffThrowTime)
            {
                giantStaffStage = GiantStaffStage.Warning;
                // Hold the empty raised hand until the deliberate downward command.
            }
        }
        if (giantStaffStage == GiantStaffStage.Warning && giantStaff.TimeUntilDrop <= GiantStaffCommandDropTime)
        {
            giantStaffStage = GiantStaffStage.Commanding; giantStaffAge = 0f;
            animator.Play("SkullMonkGiantStaffCommand", 0, 0f);
        }
        if (giantStaffStage == GiantStaffStage.Commanding && giantStaffAge >= GiantStaffCommandTime)
        {
            giantStaffStage = GiantStaffStage.Waiting;
            animator.Play("SkullMonkNoStaffIdle", 0, 0f);
        }
        if (giantStaffStage == GiantStaffStage.Waiting && giantStaff.IsDissolving)
        {
            giantStaffStage = GiantStaffStage.Regenerating;
            animator.Play("SkullMonkStaffRegenerate", 0, 0f);
        }
    }

    private void EndGiantStaffPattern()
    {
        if (!IsGiantStaffAttacking) return;
        if (giantStaff != null) giantStaff.Cancel();
        foreach (var collider in giantStaffIgnoredColliders)
            if (capsuleCollider2D != null && collider != null) Physics2D.IgnoreCollision(capsuleCollider2D, collider, false);
        giantStaffIgnoredColliders.Clear();
        giantStaff = null; giantStaffStage = GiantStaffStage.None;
        nextGiantStaffAt = Time.time + giantStaffCooldown;
    }
}
