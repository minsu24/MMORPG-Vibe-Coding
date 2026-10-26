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
        [Tooltip("Damage frame time in the source animation clip; follows Animator playback speed.")]
        [SerializeField, Min(0f)] private float hitDelay = 0.18f;
        [Tooltip("Follow-through tuning for skill chaining. Basic attack recovery follows the animation.")]
        [SerializeField, Min(0f)] private float recoveryDuration = 0.28f;

        [Header("Hit Box")]
        [SerializeField] private Vector2 attackOffset = new Vector2(0.85f, 0.15f);
        [SerializeField] private Vector2 attackSize = new Vector2(1.45f, 1f);
        [SerializeField] private bool invertFacingDirection = true;
        [SerializeField] private LayerMask enemyLayers;
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;

        private Collider2D[] hitBuffer = new Collider2D[16];
        private readonly HashSet<EnemyController> damagedEnemies =
            new HashSet<EnemyController>();

        private PlayerEntity entity;
        private PlayerIdleAnimation animationController;
        private Coroutine attackRoutine;
        private int attackSequence;

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
            if (!isActiveAndEnabled || attackRoutine != null || !animationController.CanPerformCombatAction)
                return false;
            if (!animationController.TryRequestAttack())
                return false;

            attackSequence = animationController.BasicAttackSequence;
            attackRoutine = StartCoroutine(AttackRoutine());
            return true;
        }

        private IEnumerator AttackRoutine()
        {
            while (attackSequence == animationController.BasicAttackSequence
                && animationController.IsBasicAttackActive
                && !animationController.HasReachedBasicAttackHit(hitDelay))
                yield return null;

            if (animationController.CanPerformCombatAction && animationController.IsBasicAttackActive
                && attackSequence == animationController.BasicAttackSequence)
            {
                animationController.RefreshAttackFacing();
                ApplyHit();
                animationController.MarkBasicAttackHit(recoveryDuration);
            }

            while (animationController.IsBasicAttackActive
                && attackSequence == animationController.BasicAttackSequence) yield return null;

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

            PhysicsScene2D physics = gameObject.scene.GetPhysicsScene2D();
            int hitCount;
            while (true)
            {
                hitCount = physics.OverlapBox(center, attackSize, 0f, filter, hitBuffer);
                if (hitCount < hitBuffer.Length) break;
                System.Array.Resize(ref hitBuffer, hitBuffer.Length * 2);
            }

            damagedEnemies.Clear();
            float totalDealtDamage = 0f;
            float damage = entity.RollAttackDamage(out bool isCritical) * damageMultiplier;
            for (int i = 0; i < hitCount; i++)
            {
                EnemyController enemy = hitBuffer[i] != null
                    ? hitBuffer[i].GetComponentInParent<EnemyController>()
                    : null;
                if (enemy == null || enemy.HP <= 0f || !damagedEnemies.Add(enemy))
                    continue;

                float healthBeforeHit = enemy.HP;
                entity.NotifyAttackTarget(enemy);
                enemy.TakeDamage(damage, isCritical);
                if (enemy.HP > 0f)
                    enemy.ApplyKnockback(direction);

                float dealtDamage = Mathf.Max(0f, healthBeforeHit - enemy.HP);
                entity.ApplyLifeSteal(dealtDamage);
                totalDealtDamage += dealtDamage;
            }
            GetComponent<MonkEnergy>()?.GainFromAttackHit(totalDealtDamage);
        }

        private void OnDisable()
        {
            if (attackRoutine != null)
                StopCoroutine(attackRoutine);
            attackRoutine = null;
            damagedEnemies.Clear();
        }

        private void Update()
        {
            if (attackRoutine == null) return;
            if (!animationController.CanPerformCombatAction) animationController.CancelBasicAttack();
            if (attackSequence != animationController.BasicAttackSequence) OnDisable();
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
