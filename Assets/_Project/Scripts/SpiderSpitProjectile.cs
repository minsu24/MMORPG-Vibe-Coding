using EasternFantasy.Player;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D))]
public sealed class SpiderSpitProjectile : MonoBehaviour
{
    [SerializeField] private Sprite[] flightFrames;
    [SerializeField] private Sprite[] impactFrames;
    private SpriteRenderer artwork;
    private Rigidbody2D body;
    private CircleCollider2D hitbox;
    private Vector2 direction;
    private float speed, remainingDistance, damage, elapsed;
    private bool launched, impacted;
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[24];

    public bool HasImpacted => impacted;

    private void Awake()
    {
        artwork = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<CircleCollider2D>();
        hitbox.isTrigger = true;
    }

    public void Launch(Vector2 aim, float launchSpeed, float distance, float attackDamage, EnemyController owner)
    {
        direction = aim.sqrMagnitude > .001f ? aim.normalized : Vector2.right;
        speed = Mathf.Max(.1f, launchSpeed);
        remainingDistance = Mathf.Max(0, distance);
        damage = attackDamage;
        // Native projectile artwork faces right; rotate it for both directions.
        transform.right = direction;
        artwork.sortingLayerID = owner.GetComponent<SpriteRenderer>().sortingLayerID;
        artwork.sortingOrder = owner.GetComponent<SpriteRenderer>().sortingOrder + 1;
        launched = true;
    }

    private void FixedUpdate()
    {
        if (!launched) return;
        elapsed += Time.fixedDeltaTime;
        if (impacted)
        {
            int index = Mathf.FloorToInt(elapsed / .09f);
            if (impactFrames == null || index >= impactFrames.Length) { Destroy(gameObject); return; }
            artwork.sprite = impactFrames[index];
            return;
        }
        if (flightFrames != null && flightFrames.Length > 0)
            artwork.sprite = flightFrames[Mathf.FloorToInt(elapsed / .08f) % flightFrames.Length];
        float step = Mathf.Min(speed * Time.fixedDeltaTime, remainingDistance);
        var filter = new ContactFilter2D();
        filter.SetLayerMask(~LayerMask.GetMask("Enemy", "Ignore Raycast"));
        filter.useTriggers = false;
        int count = Physics2D.CircleCast(body.position, hitbox.radius, direction, filter, castHits, step);
        for (int i = 0; i < count; i++)
        {
            var collider = castHits[i].collider;
            if (collider == null || collider == hitbox || collider.GetComponentInParent<EnemyController>() != null) continue;
            body.position += direction * castHits[i].distance;
            Impact(collider);
            return;
        }
        body.MovePosition(body.position + direction * step);
        remainingDistance -= step;
        if (remainingDistance <= 0f) { launched = false; Destroy(gameObject); }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!launched || impacted || other.isTrigger || other.GetComponentInParent<EnemyController>() != null) return;
        Impact(other);
    }

    private void Impact(Collider2D collider)
    {
        if (impacted) return;
        impacted = true;
        hitbox.enabled = false;
        body.linearVelocity = Vector2.zero;
        elapsed = 0f;
        transform.rotation = Quaternion.identity;
        if (impactFrames != null && impactFrames.Length > 0) artwork.sprite = impactFrames[0];
        EnemyAttackAudio.PlayAt(gameObject, "Audio/SpiderProjectileImpact", .27f);
        var victim = collider.GetComponentInParent<PlayerEntity>();
        if (victim == null || victim.IsDead) return;
        float before = victim.HP;
        victim.TakeDamage(damage);
        if (!victim.IsDead && victim.HP < before) victim.ApplyKnockback(Mathf.Sign(direction.x));
    }
}
