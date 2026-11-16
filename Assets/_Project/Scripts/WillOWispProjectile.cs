using EasternFantasy.Player;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D))]
public sealed class WillOWispProjectile : MonoBehaviour
{
    [SerializeField] private Sprite[] flightFrames;
    [SerializeField] private Sprite[] impactFrames;
    [SerializeField, Min(1f)] private float framesPerSecond = 10f;

    private SpriteRenderer artwork;
    private Rigidbody2D body;
    private CircleCollider2D hitbox;
    private Vector2 origin, control, destination, travelDirection;
    private float duration, elapsed, impactElapsed, damage;
    private bool launched, impacted;
    private RaycastHit2D[] castHits = new RaycastHit2D[16];

    public Vector2 Destination => destination;
    public bool HasImpacted => impacted;

    private void Awake()
    {
        artwork = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<CircleCollider2D>();
        hitbox.isTrigger = true;
    }

    public void Launch(Vector2 targetPosition, float seconds, float height, float attackDamage, EnemyController owner)
    {
        origin = body.position;
        destination = targetPosition;
        duration = Mathf.Max(.1f, seconds);
        // The midpoint rises by height above the straight path, with fixed distinct endpoints.
        control = (origin + destination) * .5f + Vector2.up * (Mathf.Max(.1f, height) * 2f);
        damage = Mathf.Max(0f, attackDamage);
        elapsed = impactElapsed = 0f;
        impacted = false;
        launched = true;
        hitbox.enabled = true;
        body.gravityScale = 0f;
        var renderer = owner.GetComponent<SpriteRenderer>();
        artwork.sortingLayerID = renderer.sortingLayerID;
        artwork.sortingOrder = renderer.sortingOrder + 1;
        if (flightFrames != null && flightFrames.Length > 0) artwork.sprite = flightFrames[0];
    }

    private void FixedUpdate()
    {
        if (!launched) return;
        if (impacted)
        {
            impactElapsed += Time.fixedDeltaTime;
            int frame = Mathf.FloorToInt(impactElapsed * framesPerSecond);
            if (impactFrames == null || frame >= impactFrames.Length) { Destroy(gameObject); return; }
            artwork.sprite = impactFrames[frame];
            return;
        }
        elapsed += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        Vector2 next = (1f - t) * (1f - t) * origin + 2f * (1f - t) * t * control + t * t * destination;
        Vector2 step = next - body.position;
        travelDirection = step.sqrMagnitude > .000001f ? step.normalized : Vector2.down;
        if (flightFrames != null && flightFrames.Length > 0)
            artwork.sprite = flightFrames[Mathf.FloorToInt(elapsed * framesPerSecond) % flightFrames.Length];
        // The fireball artwork points down-left. Follow the tangent without mirroring its art.
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(travelDirection.y, travelDirection.x) * Mathf.Rad2Deg + 135f);
        var filter = new ContactFilter2D();
        filter.SetLayerMask(~LayerMask.GetMask("Enemy", "Ignore Raycast"));
        filter.useTriggers = false;
        float radius = hitbox.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        int count;
        do
        {
            count = gameObject.scene.GetPhysicsScene2D().CircleCast(body.position, radius,
                travelDirection, step.magnitude, filter, castHits);
            if (count < castHits.Length) break;
            System.Array.Resize(ref castHits, castHits.Length * 2);
        } while (true);
        RaycastHit2D nearest = default;
        float distance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var hit = castHits[i];
            if (hit.collider == null || hit.collider == hitbox || hit.collider.GetComponentInParent<EnemyController>() != null
                || hit.distance >= distance) continue;
            nearest = hit;
            distance = hit.distance;
        }
        if (nearest.collider != null)
        {
            body.position += travelDirection * distance;
            Impact(nearest.collider);
            return;
        }
        body.MovePosition(next);
        if (t >= 1f) { body.position = destination; Impact(null); }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!launched || impacted || other.isTrigger || other.GetComponentInParent<EnemyController>() != null) return;
        Impact(other);
    }

    private void Impact(Collider2D other)
    {
        if (impacted) return;
        impacted = true;
        hitbox.enabled = false;
        body.linearVelocity = Vector2.zero;
        transform.rotation = Quaternion.identity;
        if (impactFrames == null || impactFrames.Length == 0) { Destroy(gameObject); return; }
        artwork.sprite = impactFrames[0];
        var victim = other != null ? other.GetComponentInParent<PlayerEntity>() : null;
        if (victim == null || victim.IsDead) return;
        uint health = victim.HitReactionVersion;
        victim.TakeDamage(damage);
        if (!victim.IsDead && victim.HitReactionVersion != health) victim.ApplyKnockback(Mathf.Sign(travelDirection.x));
    }
}
