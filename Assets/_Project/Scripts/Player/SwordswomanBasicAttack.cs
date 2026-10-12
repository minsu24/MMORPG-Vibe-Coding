using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerEntity), typeof(PlayerIdleAnimation), typeof(PlayerMovement2D))]
    public sealed class SwordswomanBasicAttack : MonoBehaviour, IPlayerBasicAttack
    {
        [Header("Timing")]
        [SerializeField, Min(0f)] private float hitDelay = 0.375f;
        [SerializeField, Min(0f)] private float recoveryDuration = 0.375f;
        [Header("Forward Hit Box")]
        [SerializeField] private Vector2 attackOffset = new Vector2(1.05f, 0.15f);
        [SerializeField] private Vector2 attackSize = new Vector2(1.8f, 1.6f);
        [SerializeField] private LayerMask enemyLayers;
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;
        [Header("Sword Energy")]
        [SerializeField] private SwordSlashEffect slashEffectPrefab;

        private Collider2D[] hitBuffer = new Collider2D[16];
        private readonly HashSet<EnemyController> damagedEnemies = new HashSet<EnemyController>();
        private PlayerEntity entity;
        private PlayerMovement2D movement;
        private PlayerIdleAnimation animationController;
        private SpriteRenderer characterRenderer;
        private Coroutine attackRoutine;
        private float attackDirection;

        private void Awake()
        {
            entity = GetComponent<PlayerEntity>();
            movement = GetComponent<PlayerMovement2D>();
            animationController = GetComponent<PlayerIdleAnimation>();
            Animator animator = GetComponentInChildren<Animator>();
            characterRenderer = animator != null ? animator.GetComponent<SpriteRenderer>() : null;
            if (enemyLayers.value == 0)
            {
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0) enemyLayers = 1 << enemyLayer;
            }
        }

        private bool CanHit => !entity.IsDead && !entity.isKnockback
            && !movement.IsMovementLocked && !movement.IsDashing;

        public bool TryAttack()
        {
            if (!isActiveAndEnabled || attackRoutine != null || !CanHit) return false;
            if (!animationController.TryRequestAttack()) return false;
            attackRoutine = StartCoroutine(AttackRoutine());
            return true;
        }

        private IEnumerator AttackRoutine()
        {
            if (hitDelay > 0f) yield return new WaitForSeconds(hitDelay);
            if (CanHit)
            {
                // Resolve facing at the hit frame, allowing turns during the wind-up.
                attackDirection = animationController.FacingDirectionX < 0f ? -1f : 1f;
                SpawnSlash();
                ApplyHit();
            }
            if (recoveryDuration > 0f) yield return new WaitForSeconds(recoveryDuration);
            attackRoutine = null;
        }

        private void SpawnSlash()
        {
            if (slashEffectPrefab == null) return;
            SwordSlashEffect slash = Instantiate(slashEffectPrefab,
                transform.position + Vector3.up * attackOffset.y, Quaternion.identity);
            slash.Play(attackDirection, characterRenderer);
        }

        private void ApplyHit()
        {
            Vector2 center = (Vector2)transform.position
                + new Vector2(attackOffset.x * attackDirection, attackOffset.y);
            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true, layerMask = enemyLayers, useTriggers = true
            };
            // Query this player's physics scene, also supporting isolated scenes.
            PhysicsScene2D physics = gameObject.scene.GetPhysicsScene2D();
            int count;
            while (true)
            {
                count = physics.OverlapBox(center, attackSize, 0f, filter, hitBuffer);
                if (count < hitBuffer.Length) break;
                System.Array.Resize(ref hitBuffer, hitBuffer.Length * 2);
            }
            damagedEnemies.Clear();
            float damage = entity.RollAttackDamage(out bool critical) * damageMultiplier;
            for (int i = 0; i < count; i++)
            {
                EnemyController enemy = hitBuffer[i] != null
                    ? hitBuffer[i].GetComponentInParent<EnemyController>() : null;
                if (enemy == null || enemy.HP <= 0f || !damagedEnemies.Add(enemy)) continue;
                float before = enemy.HP;
                enemy.TakeDamage(damage, critical);
                if (enemy.HP > 0f) enemy.ApplyKnockback(attackDirection);
                entity.ApplyLifeSteal(Mathf.Max(0f, before - enemy.HP));
            }
        }

        private void OnDisable()
        {
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = null;
            damagedEnemies.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            PlayerIdleAnimation animation = GetComponent<PlayerIdleAnimation>();
            float direction = animation != null && animation.FacingDirectionX < 0f ? -1f : 1f;
            Gizmos.color = new Color(0.35f, 0.9f, 1f, 0.7f);
            Gizmos.DrawWireCube(transform.position
                + new Vector3(attackOffset.x * direction, attackOffset.y), attackSize);
        }
    }
}
