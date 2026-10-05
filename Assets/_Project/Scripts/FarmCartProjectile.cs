using EasternFantasy.Player;
using UnityEngine;

public sealed class FarmCartProjectile : MonoBehaviour
{
    private Sprite[] frames;
    private SpriteRenderer visual;
    private FarmCartAttack owner;
    private float damage, age, lifetime, hitRadius, hitOffset, gravity;
    private bool flying;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[32];

    public Vector2 Velocity { get; private set; }
    public float SeparationRadius { get; private set; }
    public float RemainingLifetime => Mathf.Max(0f, lifetime - age);
    public bool IsFlying => flying && isActiveAndEnabled;

    public static FarmCartProjectile Create(FarmCartAttack owner, Vector2 origin,
        Vector2 velocity, Sprite[] frames, float width, float damage, float lifetime, float gravity = 0f)
    {
        GameObject go = new GameObject("FarmCart Projectile - " + frames[0].name);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, owner.gameObject.scene);
        go.transform.position = origin;
        FarmCartProjectile shot = go.AddComponent<FarmCartProjectile>();
        shot.owner = owner;
        shot.frames = frames;
        shot.Velocity = velocity;
        shot.gravity = gravity;
        shot.damage = damage;
        shot.lifetime = lifetime;
        shot.hitRadius = width * 0.16f;
        shot.hitOffset = width * 0.24f;
        shot.SeparationRadius = GetSeparationRadius(frames, width);
        shot.visual = go.AddComponent<SpriteRenderer>();
        SpriteRenderer source = owner.GetComponent<SpriteRenderer>();
        shot.visual.sharedMaterial = source.sharedMaterial;
        shot.visual.sortingLayerID = source.sortingLayerID;
        shot.visual.sortingOrder = source.sortingOrder + 2;
        shot.visual.sprite = frames[0];
        ApplyFlightFacing(shot.visual, velocity);
        go.transform.localScale = Vector3.one * GetScale(frames, width);
        shot.flying = true;
        return shot;
    }

    public static void ApplyFlightFacing(SpriteRenderer renderer, Vector2 velocity)
    {
        renderer.transform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
        // A 180-degree heading turns the artwork upside down unless its local Y is mirrored.
        renderer.flipY = velocity.x < 0f;
    }

    public static float GetScale(Sprite[] sprites, float width)
    {
        float maximumWidth = 0.01f;
        foreach (Sprite sprite in sprites) maximumWidth = Mathf.Max(maximumWidth, sprite.bounds.size.x);
        return width / maximumWidth;
    }

    public static Vector2 GetLobVelocity(Vector2 origin, Vector2 target, float horizontalSpeed,
        float gravity, float arcHeight, float minimumFlightTime)
    {
        gravity = Mathf.Max(0.1f, gravity);
        Vector2 offset = target - origin;
        float height = Mathf.Max(0f, offset.y) + Mathf.Max(1f, arcHeight);
        float flightTime = Mathf.Sqrt(2f * height / gravity)
            + Mathf.Sqrt(2f * (height - offset.y) / gravity);
        flightTime = Mathf.Max(flightTime, minimumFlightTime,
            Mathf.Abs(offset.x) / Mathf.Max(0.1f, horizontalSpeed));
        return new Vector2(offset.x / flightTime,
            offset.y / flightTime + 0.5f * gravity * flightTime);
    }

    public static float GetSeparationRadius(Sprite[] sprites, float width)
    {
        float radius = 0f;
        float scale = GetScale(sprites, width);
        // Enclose the whole drawing, including its motion trail, in every animation frame.
        foreach (Sprite sprite in sprites)
            radius = Mathf.Max(radius, ((Vector2)sprite.bounds.center).magnitude
                + ((Vector2)sprite.bounds.extents).magnitude);
        return radius * scale;
    }

    public static bool PathsHaveClearance(Vector2 origin, Vector2 velocity, float radius,
        float lifetime, FarmCartProjectile existing, float gap, float gravity = 0f)
    {
        if (existing == null || !existing.IsFlying) return true;
        Vector2 separation = origin - (Vector2)existing.transform.position;
        Vector2 relativeVelocity = velocity - existing.Velocity;
        float horizon = Mathf.Min(lifetime, existing.RemainingLifetime);
        float required = radius + existing.SeparationRadius + gap;
        Vector2 relativeAcceleration = Vector2.up * (existing.gravity - gravity);
        // Equal gravity cancels exactly, so the relative path is still a straight line.
        if (relativeAcceleration.sqrMagnitude < 0.0001f)
            return SegmentHasClearance(separation, relativeVelocity * horizon, required);

        // Also allow safe tuning of gravity while older projectiles are in flight.
        float interval = horizon / 32f;
        float curveMargin = relativeAcceleration.magnitude * interval * interval / 8f;
        for (int i = 0; i < 32; i++)
        {
            float start = i * interval, end = (i + 1) * interval;
            Vector2 from = separation + relativeVelocity * start + 0.5f * relativeAcceleration * start * start;
            Vector2 to = separation + relativeVelocity * end + 0.5f * relativeAcceleration * end * end;
            if (!SegmentHasClearance(from, to - from, required + curveMargin)) return false;
        }
        return true;
    }

    private static bool SegmentHasClearance(Vector2 origin, Vector2 step, float required)
    {
        float fraction = step.sqrMagnitude > 0.0001f
            ? Mathf.Clamp01(-Vector2.Dot(origin, step) / step.sqrMagnitude) : 0f;
        return (origin + step * fraction).sqrMagnitude >= required * required;
    }

    private void Update()
    {
        if (flying) visual.sprite = frames[Mathf.FloorToInt(age * 12f) % frames.Length];
    }

    private void FixedUpdate()
    {
        if (!flying) return;
        if (owner == null || !owner.isActiveAndEnabled || age >= lifetime) { Retire(); return; }
        Vector2 origin = transform.position;
        float dt = Time.fixedDeltaTime;
        Vector2 acceleration = Vector2.down * gravity;
        Vector2 step = Velocity * dt + 0.5f * acceleration * dt * dt;
        Vector2 direction = step.normalized;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.DefaultRaycastLayers);
        filter.useTriggers = true;
        int count = Physics2D.CircleCast(origin + direction * hitOffset, hitRadius,
            direction, filter, hits, step.magnitude);
        for (int i = 0; i < count; i++)
        {
            Collider2D collider = hits[i].collider;
            if (collider == null || collider.transform.IsChildOf(owner.transform)) continue;
            PlayerEntity player = collider.GetComponentInParent<PlayerEntity>();
            if (player != null)
            {
                float before = player.HP;
                player.TakeDamage(damage);
                if (!player.IsDead && player.HP < before) player.ApplyKnockback(Mathf.Sign(Velocity.x));
                Retire();
                return;
            }
            if (collider.isTrigger || collider.GetComponentInParent<Entity>() != null) continue;
            if (hits[i].normal.y > 0.5f)
                owner.PlayGroundImpact(hits[i].point, visual);
            Retire();
            return;
        }
        transform.position = origin + step;
        Velocity += acceleration * dt;
        ApplyFlightFacing(visual, Velocity);
        age += dt;
    }

    private void Retire()
    {
        flying = false;
        Destroy(gameObject);
    }
}
