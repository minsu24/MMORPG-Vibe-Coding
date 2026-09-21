using UnityEngine;

namespace EasternFantasy.Player
{
    [RequireComponent(typeof(PlayerIdleAnimation))]
    [RequireComponent(typeof(PlayerEntity))]
    public sealed class PlayerProjectileShooter : MonoBehaviour
    {
        [SerializeField] private PlayerProjectile projectilePrefab;
        [SerializeField, Min(0f)] private float projectileSpeed = 10f;
        [SerializeField, Min(0f)] private float maximumTravelDistance = 8f;
        [SerializeField] private Vector2 spawnOffset = new Vector2(0.4f, 0.25f);

        private PlayerIdleAnimation playerAnimation;
        private PlayerEntity playerEntity;

        private void Awake()
        {
            playerAnimation = GetComponent<PlayerIdleAnimation>();
            playerEntity = GetComponent<PlayerEntity>();
        }

        public bool TryFire()
        {
            if (projectilePrefab == null)
            {
                Debug.LogError("PlayerProjectileShooter requires a projectile prefab.", this);
                return false;
            }

            if (!playerAnimation.TryRequestAttack())
                return false;

            float directionX = playerAnimation.FacingDirectionX;
            Vector3 position = transform.position + new Vector3(
                spawnOffset.x * directionX,
                spawnOffset.y,
                0f);

            PlayerProjectile projectile = Instantiate(projectilePrefab, position, Quaternion.identity);
            float damage = playerEntity.RollAttackDamage(out _);
            projectile.Launch(
                Vector2.right * directionX,
                projectileSpeed,
                maximumTravelDistance,
                damage,
                playerEntity);

            return true;
        }

        public bool TryFireBurst(int projectileCount, float verticalSpacing, float damageMultiplier)
        {
            if (projectilePrefab == null || projectileCount <= 0)
                return false;
            if (!playerAnimation.TryRequestAttack())
                return false;

            float directionX = playerAnimation.FacingDirectionX;
            float center = (projectileCount - 1) * 0.5f;
            float damage = playerEntity.RollAttackDamage(out _) * Mathf.Max(0f, damageMultiplier);
            for (int i = 0; i < projectileCount; i++)
            {
                float yOffset = (i - center) * verticalSpacing;
                Vector3 position = transform.position + new Vector3(
                    spawnOffset.x * directionX,
                    spawnOffset.y + yOffset,
                    0f);
                PlayerProjectile projectile = Instantiate(projectilePrefab, position, Quaternion.identity);
                projectile.Launch(
                    Vector2.right * directionX,
                    projectileSpeed,
                    maximumTravelDistance,
                    damage,
                    playerEntity);
            }

            return true;
        }
    }
}
