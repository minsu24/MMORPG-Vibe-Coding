using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Player
{
    [RequireComponent(typeof(PlayerIdleAnimation))]
    [RequireComponent(typeof(PlayerEntity))]
    public sealed class PlayerProjectileShooter : MonoBehaviour, IPlayerBasicAttack
    {
        [SerializeField] private PlayerProjectile projectilePrefab;
        [SerializeField] private PlayerProjectile burstProjectilePrefab;
        [SerializeField, Min(0f)] private float projectileSpeed = 10f;
        [SerializeField, Min(0f)] private float maximumTravelDistance = 8f;
        [SerializeField] private Vector2 spawnOffset = new Vector2(0.4f, 0.25f);
        [SerializeField, Min(0.01f)] private float burstShotInterval = 0.1f;

        private PlayerIdleAnimation playerAnimation;
        private PlayerEntity playerEntity;
        private Coroutine burstRoutine;

        private void Awake()
        {
            playerAnimation = GetComponent<PlayerIdleAnimation>();
            playerEntity = GetComponent<PlayerEntity>();
        }

        public bool TryFire()
        {
            if (!isActiveAndEnabled || !playerAnimation.CanPerformCombatAction)
                return false;
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
            float damage = playerEntity.RollAttackDamage(out bool isCritical);
            projectile.Launch(
                Vector2.right * directionX,
                projectileSpeed,
                maximumTravelDistance,
                damage,
                playerEntity,
                isCritical);

            GetComponent<PlayerBasicAttackAudio>()?.Play();
            playerAnimation.MarkBasicAttackHit(0.28f);
            return true;
        }

        public bool TryAttack()
        {
            return TryFire();
        }

        public bool TryFireBurst(int projectileCount, float verticalSpacing, float damageMultiplier, Action onProjectileFired = null)
        {
            if (!isActiveAndEnabled || burstRoutine != null || !playerAnimation.CanPerformCombatAction)
                return false;
            PlayerProjectile burstPrefab = burstProjectilePrefab != null
                ? burstProjectilePrefab : projectilePrefab;
            if (burstPrefab == null || projectileCount <= 0)
                return false;
            if (!playerAnimation.TryRequestThrow())
                return false;

            float directionX = playerAnimation.FacingDirectionX;
            float center = (projectileCount - 1) * 0.5f;
            float damage = playerEntity.RollAttackDamage(out bool isCritical)
                * Mathf.Max(0f, damageMultiplier);
            burstRoutine = StartCoroutine(FireBurst(burstPrefab, projectileCount, verticalSpacing,
                directionX, center, damage, isCritical, onProjectileFired));
            return true;
        }

        private IEnumerator FireBurst(PlayerProjectile burstPrefab, int projectileCount, float verticalSpacing,
            float directionX, float center, float damage, bool isCritical, Action onProjectileFired)
        {
            int scene = SceneManager.GetActiveScene().handle;
            for (int i = 0; i < projectileCount; i++)
            {
                if (i > 0)
                    yield return new WaitForSeconds(Mathf.Max(0.01f, burstShotInterval));
                if (playerEntity == null || playerEntity.IsDead || scene != SceneManager.GetActiveScene().handle)
                    break;
                float yOffset = (i - center) * verticalSpacing;
                Vector3 position = transform.position + new Vector3(
                    spawnOffset.x * directionX,
                    spawnOffset.y + yOffset,
                    0f);
                PlayerProjectile projectile = Instantiate(burstPrefab, position, Quaternion.identity);
                projectile.Launch(
                    Vector2.right * directionX,
                    projectileSpeed,
                    maximumTravelDistance,
                    damage,
                    playerEntity,
                    isCritical);
                onProjectileFired?.Invoke();
            }
            // Yield once for single-shot calls so StartCoroutine's handle is cleared reliably.
            yield return null;
            burstRoutine = null;
        }

        private void OnDisable()
        {
            if (burstRoutine != null) StopCoroutine(burstRoutine);
            burstRoutine = null;
        }
    }
}
