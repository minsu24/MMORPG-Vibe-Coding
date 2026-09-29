using EasternFantasy.Player;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(Rigidbody2D))]
public sealed class FrogSpitProjectile : MonoBehaviour
{
    [SerializeField] private Sprite[] animationFrames;
    [SerializeField, Min(1f)] private float framesPerSecond = 10f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;
    private EnemyController owner;
    private Vector2 direction;
    private Vector2 startPosition;
    private float speed;
    private float maximumTravelDistance;
    private float damage;
    private float animationTime;
    private bool launched;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 launchDirection, float launchSpeed,
        float travelDistance, float attackDamage, EnemyController attackOwner)
    {
        direction = launchDirection.sqrMagnitude > 0.001f
            ? launchDirection.normalized
            : Vector2.right;
        speed = Mathf.Max(0f, launchSpeed);
        maximumTravelDistance = Mathf.Max(0f, travelDistance);
        damage = Mathf.Max(0f, attackDamage);
        owner = attackOwner;
        startPosition = body.position;
        transform.right = direction;
        launched = true;
    }

    private void Update()
    {
        if (animationFrames == null || animationFrames.Length == 0)
            return;

        animationTime += Time.deltaTime * framesPerSecond;
        int frame = Mathf.FloorToInt(animationTime) % animationFrames.Length;
        spriteRenderer.sprite = animationFrames[frame];
    }

    private void FixedUpdate()
    {
        if (!launched)
            return;

        float travelledDistance = Vector2.Distance(startPosition, body.position);
        float remainingDistance = maximumTravelDistance - travelledDistance;
        float stepDistance = speed * Time.fixedDeltaTime;
        if (remainingDistance <= stepDistance)
        {
            body.position = startPosition + direction * maximumTravelDistance;
            Destroy(gameObject);
            return;
        }

        body.MovePosition(body.position + direction * stepDistance);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!launched || other.GetComponentInParent<EnemyController>() == owner)
            return;

        PlayerEntity hitPlayer = other.GetComponentInParent<PlayerEntity>();
        if (hitPlayer != null)
        {
            if (!hitPlayer.IsDead && !hitPlayer.isInvincible)
            {
                hitPlayer.TakeDamage(damage);
                if (!hitPlayer.IsDead)
                    hitPlayer.ApplyKnockback(Mathf.Sign(direction.x));
            }

            launched = false;
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger && other.GetComponentInParent<EnemyController>() == null)
        {
            launched = false;
            Destroy(gameObject);
        }
    }
}
