using UnityEngine;

namespace EasternFantasy.Player
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(Rigidbody2D))]
    public sealed class PlayerProjectile : MonoBehaviour
    {
        private Vector2 direction;
        private Vector2 startPosition;
        private float speed;
        private float maximumTravelDistance;
        private float damage;
        private Entity owner;
        private Rigidbody2D body;
        private bool launched;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        public void Launch(
            Vector2 launchDirection,
            float launchSpeed,
            float travelDistance,
            float attackDamage,
            Entity attackOwner)
        {
            direction = launchDirection.normalized;
            speed = Mathf.Max(0f, launchSpeed);
            maximumTravelDistance = Mathf.Max(0f, travelDistance);
            damage = Mathf.Max(0f, attackDamage);
            owner = attackOwner;
            startPosition = body.position;
            launched = true;

            GetComponent<SpriteRenderer>().flipX = direction.x < 0f;
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
                launched = false;
                Destroy(gameObject);
                return;
            }

            body.MovePosition(body.position + direction * stepDistance);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!launched)
                return;

            Entity hitEntity = other.GetComponentInParent<Entity>();
            if (hitEntity == null || hitEntity == owner)
                return;

            float healthBeforeHit = hitEntity.HP;
            hitEntity.TakeDamage(damage);
            float dealtDamage = Mathf.Max(0f, healthBeforeHit - hitEntity.HP);
            if (owner is PlayerEntity playerOwner)
                playerOwner.ApplyLifeSteal(dealtDamage);
            launched = false;
            Destroy(gameObject);
        }
    }
}
