using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public sealed class FarmCartAttack : EnemyController
{
    [Header("Three Frames Per Crop")]
    [SerializeField] private Sprite[] radishFrames;
    [SerializeField] private Sprite[] pumpkinFrames;
    [SerializeField] private Sprite[] hayFrames;
    [Header("Ground Impact")]
    [SerializeField] private Sprite[] impactDustFrames;
    [SerializeField, Min(0.1f)] private float impactDustHeight = 2.5f;
    [Header("Avoidable Barrage")]
    [SerializeField, Min(1f)] private float minimumPlayerDistance = 5f;
    [SerializeField, Min(1f)] private float projectileSpeed = 12f;
    [Header("Lobbed Trajectory")]
    [Tooltip("Minimum height above the launch point and target.")]
    [SerializeField, Min(1f)] private float arcHeight = 4f;
    [SerializeField, Min(0.1f)] private float projectileGravity = 18f;
    [SerializeField, Min(0.1f)] private float projectileWidth = 6f;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 5.5f;
    [SerializeField, Min(0f)] private float projectileGap = 0.6f;
    [SerializeField, Min(0.1f)] private float warningDuration = 0.6f;
    [SerializeField, Min(0f)] private float shotInterval = 0.35f;
    [SerializeField, Min(1)] private int shotsPerBurst = 6;
    [SerializeField, Min(0f)] private float burstRest = 1.8f;
    [SerializeField, Min(0.1f)] private float minimumFlightTime = 0.9f;
    [SerializeField] private Vector2 targetScatter = new Vector2(4f, 2f);

    private readonly List<FarmCartProjectile> activeShots = new List<FarmCartProjectile>();
    private readonly int[] cropBag = { 0, 1, 2 };
    private int bagIndex = 3, shotsInBurst;
    private SpriteRenderer warning;
    private Sprite[] pendingFrames;
    private Vector2 pendingOrigin, pendingVelocity;
    private float nextShotTime, warningEnds, pendingRadius;

    public void PlayGroundImpact(Vector2 point, SpriteRenderer projectileRenderer)
    {
        GiantAttackVisual.CreateDust(point, impactDustFrames, projectileRenderer, 0f, impactDustHeight);
    }

    // A stationary ranged enemy: do not chase, turn, or deal contact damage.
    public override float ContactDamage => 0f;
    protected override bool ControlsAttackMovement => true;

    private void Start()
    {
        inFarAttackRange = true;
        nextShotTime = Time.time + 1f;
    }

    protected override void AttackFixedUpdate() => rb.linearVelocity = Vector2.zero;

    public override void ApplyKnockback(float directionX) { }

    protected override bool CanUseAbility()
    {
        // Finish an announced shot even if the player leaves detection range.
        return HP > 0f && (warning != null || base.CanUseAbility());
    }

    protected override void MonsterAbility()
    {
        if (Time.timeScale <= 0f) return;
        activeShots.RemoveAll(s => s == null || !s.IsFlying);
        if (!HasLivingPlayer)
        {
            ClearWarning();
            return;
        }
        if (warning != null)
        {
            float pulse = 0.5f + 0.25f * Mathf.Sin(Time.time * 22f);
            warning.color = new Color(1f, 0.8f, 0.45f, pulse);
            if (Time.time < warningEnds) return;
            // Keep the promised trajectory. Wait if another projectile would cross it.
            if (!HasClearance(pendingOrigin, pendingVelocity, pendingRadius)) return;
            FirePendingShot();
            return;
        }
        float distance = Vector2.Distance(playerEntity.transform.position, spriteRenderer.bounds.center);
        if (Time.time < nextShotTime || distance > _detectRange || distance < minimumPlayerDistance) return;
        PrepareShot();
    }

    private void PrepareShot()
    {
        if (!HasFrames(radishFrames) || !HasFrames(pumpkinFrames) || !HasFrames(hayFrames)) return;
        Sprite[] frames = NextCrop();
        float radius = FarmCartProjectile.GetSeparationRadius(frames, projectileWidth);
        float facing = playerEntity.transform.position.x < spriteRenderer.bounds.center.x ? -1f : 1f;
        Vector2 origin = new Vector2(spriteRenderer.bounds.center.x + facing * (spriteRenderer.bounds.extents.x + 0.2f),
            spriteRenderer.bounds.center.y + spriteRenderer.bounds.extents.y * 0.55f);
        Collider2D playerCollider = playerEntity.GetComponent<Collider2D>();
        Vector2 targetCenter = playerCollider != null ? (Vector2)playerCollider.bounds.center : (Vector2)playerEntity.transform.position;
        for (int attempt = 0; attempt < 16; attempt++)
        {
            Vector2 target = targetCenter + new Vector2(Random.Range(-targetScatter.x, targetScatter.x),
                Random.Range(-targetScatter.y, targetScatter.y));
            Vector2 aim = target - origin;
            if (aim.sqrMagnitude < 4f || aim.x * facing <= 0f) continue;
            Vector2 velocity = FarmCartProjectile.GetLobVelocity(origin, target,
                projectileSpeed, projectileGravity, arcHeight, minimumFlightTime);
            if (!HasClearance(origin, velocity, radius)) continue;
            pendingFrames = frames;
            pendingRadius = radius;
            pendingOrigin = origin;
            pendingVelocity = velocity;
            ShowWarning();
            return;
        }
        nextShotTime = Time.time + 0.15f;
    }

    private bool HasClearance(Vector2 origin, Vector2 velocity, float radius)
    {
        foreach (FarmCartProjectile shot in activeShots)
            if (!FarmCartProjectile.PathsHaveClearance(origin, velocity, radius,
                projectileLifetime, shot, projectileGap, projectileGravity)) return false;
        return true;
    }

    private static bool HasFrames(Sprite[] frames) => frames != null && frames.Length == 3
        && frames[0] != null && frames[1] != null && frames[2] != null;

    private Sprite[] NextCrop()
    {
        if (bagIndex >= cropBag.Length)
        {
            for (int i = cropBag.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int swap = cropBag[i]; cropBag[i] = cropBag[j]; cropBag[j] = swap;
            }
            bagIndex = 0;
        }
        int crop = cropBag[bagIndex++];
        return crop == 0 ? radishFrames : crop == 1 ? pumpkinFrames : hayFrames;
    }

    private void ShowWarning()
    {
        GameObject go = new GameObject("FarmCart Launch Warning");
        go.transform.SetParent(transform, true);
        go.transform.position = pendingOrigin;
        // World size is independent of the cart's scale.
        go.transform.localScale = Vector3.one * (FarmCartProjectile.GetScale(pendingFrames, projectileWidth) / Mathf.Abs(transform.lossyScale.x));
        warning = go.AddComponent<SpriteRenderer>();
        warning.sprite = pendingFrames[0];
        FarmCartProjectile.ApplyFlightFacing(warning, pendingVelocity);
        warning.sharedMaterial = spriteRenderer.sharedMaterial;
        warning.sortingLayerID = spriteRenderer.sortingLayerID;
        warning.sortingOrder = spriteRenderer.sortingOrder + 1;
        warning.color = new Color(1f, 0.8f, 0.45f, 0.6f);
        warningEnds = Time.time + warningDuration;
    }

    private void FirePendingShot()
    {
        activeShots.Add(FarmCartProjectile.Create(this, pendingOrigin, pendingVelocity,
            pendingFrames, projectileWidth, Attack_Power, projectileLifetime, projectileGravity));
        ClearWarning();
        shotsInBurst++;
        if (shotsInBurst >= shotsPerBurst)
        {
            shotsInBurst = 0;
            nextShotTime = Time.time + burstRest;
        }
        else nextShotTime = Time.time + shotInterval;
    }

    private void ClearWarning()
    {
        if (warning != null) Destroy(warning.gameObject);
        warning = null;
    }

    protected override void OnDefeated()
    {
        StopBarrage();
        base.OnDefeated();
    }

    protected override void OnDisable()
    {
        StopBarrage();
        base.OnDisable();
    }

    private void StopBarrage()
    {
        ClearWarning();
        foreach (FarmCartProjectile shot in activeShots)
            if (shot != null) Destroy(shot.gameObject);
        activeShots.Clear();
        shotsInBurst = 0;
        nextShotTime = Time.time + 1f;
    }
}
