using UnityEngine;

public sealed partial class MON_WillOWisp
{
    [Header("Blink-like hovering")]
    [SerializeField] private Vector2 calmRoamRadius = new Vector2(3.5f, .15f);
    [SerializeField] private Vector2 blinkDistance = new Vector2(1.4f, 2.8f);
    [SerializeField, Min(.02f)] private float blinkDuration = .11f;
    [SerializeField] private Vector2 blinkPause = new Vector2(1.5f, 3f);
    [SerializeField, Min(.01f)] private float idleHoverSpeed = .25f;

    private Vector2 hoverOrigin, blinkTarget;
    private float blinkWait, blinkSpeed;
    private float hoverElapsed, hoverPhase;
    private bool blinking;
    private bool blinkSoundPlayed;
    private RaycastHit2D[] blinkHits = new RaycastHit2D[16];
    private Collider2D[] terrainOverlaps = new Collider2D[16];
    private bool needsSpawnClearance = true;
    private Bounds minimumTerrainBounds;
    private ContactFilter2D terrainFilter;

    protected override bool ControlsLocomotion => true;

    protected override void Awake()
    {
        base.Awake();
        minimumTerrainBounds = spriteRenderer.localBounds;
        hoverOrigin = rb.position;
        hoverPhase = Random.Range(0f, Mathf.PI * 2f);
        blinkWait = Random.Range(blinkPause.x, blinkPause.y);
        // Preserve a solid, hittable enemy collider while allowing the player to walk through it.
        capsuleCollider2D.excludeLayers |= LayerMask.GetMask("Player");
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        terrainFilter.SetLayerMask(~LayerMask.GetMask("Player", "Enemy", "Ignore Raycast"));
        terrainFilter.useTriggers = false;
        FindCameraBounds();
    }

    private void StopBlink()
    {
        blinking = false;
        blinkWait = Random.Range(blinkPause.x, blinkPause.y);
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    protected override void LocomotionFixedUpdate()
    {
        if (Time.timeScale <= 0f) return;
        hoverElapsed += Time.fixedDeltaTime;
        Vector2 cameraCorrection = KeepInsideCamera();
        if (needsSpawnClearance) hoverOrigin += cameraCorrection;
        ResolveTerrainOverlap();
        KeepInsideCamera();
        if (isKnockback)
        {
            rb.linearVelocity = LimitTerrainVelocity(rb.linearVelocity);
            return;
        }
        if (isAttacking) { rb.linearVelocity = Vector2.zero; return; }
        if (provoked && !HasLivingPlayer) { provoked = false; StopBlink(); }
        if (!blinking)
        {
            rb.linearVelocity = Vector2.zero;
            blinkWait -= Time.fixedDeltaTime;
            if (blinkWait > 0f)
            {
                FloatInPlace();
                return;
            }
            ChooseBlinkTarget();
        }
        Vector2 remaining = blinkTarget - rb.position;
        float distance = Mathf.Min(remaining.magnitude, blinkSpeed * Time.fixedDeltaTime);
        if (distance <= .015f) { StopBlink(); return; }
        Vector2 direction = remaining.normalized;
        rb.linearVelocity = LimitTerrainVelocity(direction * (distance / Time.fixedDeltaTime));
        if (rb.linearVelocity.sqrMagnitude < .001f) { StopBlink(); return; }
        if (!blinkSoundPlayed)
        {
            blinkSoundPlayed = true;
            EnemyAttackAudio.PlayFor(this, "Audio/WispMove", .18f);
        }
        if (Mathf.Abs(direction.x) > .01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x) * Mathf.Sign(direction.x);
            transform.localScale = scale;
        }
    }

    private void FloatInPlace()
    {
        float targetY = hoverOrigin.y + Mathf.Sin(hoverElapsed * 1.6f + hoverPhase) * calmRoamRadius.y;
        float nextY = Mathf.MoveTowards(rb.position.y, targetY, idleHoverSpeed * Time.fixedDeltaTime);
        rb.linearVelocity = LimitTerrainVelocity(Vector2.up * ((nextY - rb.position.y) / Time.fixedDeltaTime));
    }

    private Bounds TerrainBounds()
    {
        // The visible flame/tail extends beyond the damage hitbox. Keep both outside the floor.
        // Renderer transforms may still be interpolated at the previous physics position.
        var local = spriteRenderer.localBounds;
        local.Encapsulate(minimumTerrainBounds);
        var bounds = PhysicsBounds(local);
        bounds.Encapsulate(PhysicsBounds(new Bounds(capsuleCollider2D.offset, capsuleCollider2D.size)));
        return bounds;
    }

    private Bounds PhysicsBounds(Bounds local)
    {
        var matrix = transform.localToWorldMatrix;
        Vector3 x = matrix.MultiplyVector(Vector3.right * local.extents.x);
        Vector3 y = matrix.MultiplyVector(Vector3.up * local.extents.y);
        Vector3 z = matrix.MultiplyVector(Vector3.forward * local.extents.z);
        Vector3 extent = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
            Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
        return new Bounds(new Vector3(rb.position.x, rb.position.y, transform.position.z)
            + matrix.MultiplyVector(local.center), extent * 2f);
    }

    private Vector2 LimitTerrainVelocity(Vector2 velocity)
    {
        Vector2 desired = rb.position + velocity * Time.fixedDeltaTime;
        desired.y = Mathf.Clamp(desired.y, hoverOrigin.y - calmRoamRadius.y, hoverOrigin.y + calmRoamRadius.y);
        desired = ClampToCamera(desired);
        velocity = (desired - rb.position) / Time.fixedDeltaTime;
        float distance = velocity.magnitude * Time.fixedDeltaTime;
        if (distance <= .0001f) return Vector2.zero;
        Vector2 direction = velocity.normalized;
        var bounds = TerrainBounds();
        int count;
        do
        {
            // Shape queries treat one-way platforms as solid from every direction for this enemy.
            count = gameObject.scene.GetPhysicsScene2D().BoxCast(bounds.center, bounds.size, 0f,
                direction, distance + .06f, terrainFilter, blinkHits);
            if (count == blinkHits.Length) System.Array.Resize(ref blinkHits, blinkHits.Length * 2);
            else break;
        } while (true);
        for (int i = 0; i < count; i++)
        {
            var hit = blinkHits[i];
            if (hit.collider == null || hit.collider.GetComponentInParent<EnemyController>() != null) continue;
            distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - .06f));
        }
        return direction * (distance / Time.fixedDeltaTime);
    }

    private void ResolveTerrainOverlap()
    {
        Vector2 original = rb.position;
        // Spawn points and animation bounds can initially overlap an existing platform.
        for (int pass = 0; pass < 6; pass++)
        {
            var bounds = TerrainBounds();
            int count;
            do
            {
                count = gameObject.scene.GetPhysicsScene2D().OverlapBox(bounds.center, bounds.size,
                    0f, terrainFilter, terrainOverlaps);
                if (count == terrainOverlaps.Length) System.Array.Resize(ref terrainOverlaps, terrainOverlaps.Length * 2);
                else break;
            } while (true);
            Vector2 correction = Vector2.zero;
            for (int i = 0; i < count; i++)
            {
                var other = terrainOverlaps[i];
                if (other == null || other.GetComponentInParent<EnemyController>() != null) continue;
                var obstacle = other.bounds;
                float left = obstacle.min.x - bounds.max.x - .06f;
                float right = obstacle.max.x - bounds.min.x + .06f;
                float down = obstacle.min.y - bounds.max.y - .06f;
                float up = obstacle.max.y - bounds.min.y + .06f;
                if (needsSpawnClearance && obstacle.size.x > obstacle.size.y * 2f)
                    correction = Vector2.up * up; // Start above a floor, never underneath it.
                else
                {
                    float x = Mathf.Abs(left) < Mathf.Abs(right) ? left : right;
                    float y = Mathf.Abs(down) < Mathf.Abs(up) ? down : up;
                    correction = Mathf.Abs(x) < Mathf.Abs(y) ? Vector2.right * x : Vector2.up * y;
                }
                break;
            }
            if (correction.sqrMagnitude < .000001f) break;
            rb.position += correction;
            Physics2D.SyncTransforms();
        }
        if (needsSpawnClearance) hoverOrigin += rb.position - original;
        needsSpawnClearance = false;
    }

    private void ChooseBlinkTarget()
    {
        Vector2 start = rb.position;
        float distance = Random.Range(blinkDistance.x, blinkDistance.y);
        if (provoked && HasLivingPlayer)
        {
            Vector2 aim = playerEntity.transform.position;
            if (Vector2.Distance(start, aim) > attackRange * .85f)
                blinkTarget = Vector2.MoveTowards(start, aim, distance);
            else
                blinkTarget = start + new Vector2((Random.value < .5f ? -1 : 1) * distance,
                    Random.Range(-calmRoamRadius.y, calmRoamRadius.y));
        }
        else
        {
            // Wander around the spawn point; passing players never influence the neutral route.
            Vector2 offset = start - hoverOrigin;
            float sign = Mathf.Abs(offset.x) > calmRoamRadius.x * .65f
                ? -Mathf.Sign(offset.x) : (Random.value < .5f ? -1f : 1f);
            blinkTarget = new Vector2(
                Mathf.Clamp(start.x + sign * distance, hoverOrigin.x - calmRoamRadius.x, hoverOrigin.x + calmRoamRadius.x),
                hoverOrigin.y + Random.Range(-calmRoamRadius.y, calmRoamRadius.y));
        }
        blinkTarget.y = Mathf.Clamp(blinkTarget.y, hoverOrigin.y - calmRoamRadius.y, hoverOrigin.y + calmRoamRadius.y);
        blinkTarget = ClampToCamera(blinkTarget);
        blinkSpeed = Vector2.Distance(start, blinkTarget) / Mathf.Max(.02f, blinkDuration);
        blinking = true;
        blinkSoundPlayed = false;
    }
}
