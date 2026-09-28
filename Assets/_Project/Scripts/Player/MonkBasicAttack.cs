using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerEntity))]
    [RequireComponent(typeof(PlayerIdleAnimation))]
    public sealed class MonkBasicAttack : MonoBehaviour, IPlayerBasicAttack
    {
        [Header("Timing")]
        [Tooltip("Time from attack input to the damage frame.")]
        [SerializeField, Min(0f)] private float hitDelay = 0.18f;
        [Tooltip("Time after the hit before another basic attack can start.")]
        [SerializeField, Min(0f)] private float recoveryDuration = 0.28f;

        [Header("Hit Box")]
        [SerializeField] private Vector2 attackOffset = new Vector2(0.85f, 0.15f);
        [SerializeField] private Vector2 attackSize = new Vector2(1.45f, 1f);
        [SerializeField] private bool invertFacingDirection = true;
        [SerializeField] private LayerMask enemyLayers;
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;

        private readonly Collider2D[] hitBuffer = new Collider2D[12];
        private readonly HashSet<EnemyController> damagedEnemies =
            new HashSet<EnemyController>();

        private PlayerEntity entity;
        private PlayerIdleAnimation animationController;
        private Coroutine attackRoutine;

        private void Awake()
        {
            entity = GetComponent<PlayerEntity>();
            animationController = GetComponent<PlayerIdleAnimation>();

            if (enemyLayers.value == 0)
            {
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0)
                    enemyLayers = 1 << enemyLayer;
            }
        }

        public bool TryAttack()
        {
            if (!isActiveAndEnabled || attackRoutine != null || entity.IsDead)
                return false;
            if (!animationController.TryRequestAttack())
                return false;

            attackRoutine = StartCoroutine(AttackRoutine());
            return true;
        }

        private IEnumerator AttackRoutine()
        {
            if (hitDelay > 0f)
                yield return new WaitForSeconds(hitDelay);

            if (!entity.IsDead)
                ApplyHit();

            if (recoveryDuration > 0f)
                yield return new WaitForSeconds(recoveryDuration);

            attackRoutine = null;
        }

        private void ApplyHit()
        {
            float direction = GetAttackDirection();

            Vector2 center = (Vector2)transform.position + new Vector2(
                attackOffset.x * direction,
                attackOffset.y);

            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = enemyLayers,
                useTriggers = true
            };

            int hitCount = Physics2D.OverlapBox(
                center,
                attackSize,
                0f,
                filter,
                hitBuffer);

            damagedEnemies.Clear();
            float damage = entity.RollAttackDamage(out bool isCritical) * damageMultiplier;
            for (int i = 0; i < hitCount; i++)
            {
                EnemyController enemy = hitBuffer[i] != null
                    ? hitBuffer[i].GetComponentInParent<EnemyController>()
                    : null;
                if (enemy == null || !damagedEnemies.Add(enemy))
                    continue;

                float healthBeforeHit = enemy.HP;
                enemy.TakeDamage(damage, isCritical);
                if (enemy.HP > 0f)
                    enemy.ApplyKnockback(direction);

                float dealtDamage = Mathf.Max(0f, healthBeforeHit - enemy.HP);
                entity.ApplyLifeSteal(dealtDamage);
                if (dealtDamage > 0f)
                    GetComponent<MonkEnergy>()?.RegisterCombat();
            }
        }

        private void OnDisable()
        {
            if (attackRoutine != null)
                StopCoroutine(attackRoutine);
            attackRoutine = null;
            damagedEnemies.Clear();
        }

        private float GetAttackDirection()
        {
            float direction = animationController != null
                ? animationController.FacingDirectionX
                : 1f;
            if (Mathf.Approximately(direction, 0f))
                direction = 1f;
            return invertFacingDirection ? -direction : direction;
        }

        private void OnDrawGizmosSelected()
        {
            if (animationController == null)
                animationController = GetComponent<PlayerIdleAnimation>();
            float direction = GetAttackDirection();
            Vector2 center = (Vector2)transform.position + new Vector2(
                attackOffset.x * direction,
                attackOffset.y);

            Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.45f);
            Gizmos.DrawWireCube(center, attackSize);
        }

        private void OnValidate()
        {
            attackSize.x = Mathf.Max(0.01f, attackSize.x);
            attackSize.y = Mathf.Max(0.01f, attackSize.y);
            damageMultiplier = Mathf.Max(0f, damageMultiplier);
        }
    }
}
