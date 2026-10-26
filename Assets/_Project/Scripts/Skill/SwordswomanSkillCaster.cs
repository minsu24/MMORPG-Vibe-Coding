using System.Collections;
using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

namespace EasternFantasy.Skill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerEntity), typeof(PlayerMovement2D), typeof(PlayerIdleAnimation))]
    [RequireComponent(typeof(PlayerSkillSystem))]
    public sealed class SwordswomanSkillCaster : MonoBehaviour, IPlayerSkillCaster
    {
        [SerializeField] private SkillDefinition dashSkill;
        [SerializeField] private SkillDefinition doubleSlashSkill;
        [SerializeField] private SkillDefinition drawingSkill;
        [Header("Dash")]
        [SerializeField, Min(0.01f), Tooltip("Distance travelled in world units, unless blocked by terrain.")]
        private float dashDistance = 4f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.25f;
        [Header("Double Slash - Forward Rectangle")]
        [SerializeField] private Vector2 doubleSlashOffset = new Vector2(1.6f, 0.15f);
        [SerializeField] private Vector2 doubleSlashSize = new Vector2(3f, 1.8f);
        [SerializeField, Min(0f), Tooltip("Attack damage multiplier for each of the two strikes.")]
        private float doubleSlashDamageMultiplier = 1.2f;
        [SerializeField] private LayerMask enemyLayers;

        private PlayerEntity entity;
        private PlayerMovement2D movement;
        private PlayerIdleAnimation animationController;
        private PlayerSkillSystem skills;
        private SwordswomanDrawingAttack drawingAttack;
        private Coroutine dashRoutine;
        private Coroutine slashRoutine;
        private bool slashActive;
        private float slashDirection;
        private int lastStrike;
        private Collider2D[] hitBuffer = new Collider2D[16];
        private readonly HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>();
        private readonly Dictionary<SkillDefinition, float> cooldownEnds = new Dictionary<SkillDefinition, float>();

        private void Awake()
        {
            entity = GetComponent<PlayerEntity>();
            movement = GetComponent<PlayerMovement2D>();
            animationController = GetComponent<PlayerIdleAnimation>();
            skills = GetComponent<PlayerSkillSystem>();
            drawingAttack = GetComponent<SwordswomanDrawingAttack>();
            if (enemyLayers.value == 0)
            {
                int layer = LayerMask.NameToLayer("Enemy");
                if (layer >= 0) enemyLayers = 1 << layer;
            }
        }

        public bool TryCast(SkillDefinition skill)
        {
            if (!isActiveAndEnabled || !animationController.CanPerformCombatAction
                || skill == null || (skill != dashSkill && skill != doubleSlashSkill && skill != drawingSkill)
                || skill.ActivationType != SkillActivationType.Active
                || entity.IsDead || entity.isKnockback || Time.timeScale <= 0f
                || dashRoutine != null || slashActive || (drawingAttack != null && drawingAttack.IsActive)
                || movement.IsMovementLocked || movement.IsDashing
                || skills.GetSkillLevel(skill) <= 0 || GetCooldownRemaining(skill) > 0f
                || !SkillResourcePayment.CanAfford(entity, skill))
                return false;

            if (skill == dashSkill)
            {
                if (!movement.CanDash || !animationController.TryRequestSkillAnimation("SwordDash", dashDuration))
                    return false;
                if (!movement.BeginDash(animationController.FacingDirectionX, dashDistance, dashDuration))
                {
                    animationController.FinishSkillAnimation();
                    return false;
                }
                if (!SkillResourcePayment.TrySpend(entity, skill))
                {
                    movement.EndDash();
                    animationController.FinishSkillAnimation();
                    return false;
                }
                dashRoutine = StartCoroutine(WaitForDash());
            }
            else if (skill == doubleSlashSkill)
            {
                if (!animationController.TryRequestSkillAnimation("SwordDoubleSlash")) return false;
                if (!SkillResourcePayment.TrySpend(entity, skill))
                {
                    animationController.FinishSkillAnimation();
                    return false;
                }
                slashActive = true;
                lastStrike = 0;
                slashDirection = animationController.FacingDirectionX < 0f ? -1f : 1f;
                movement.SetMovementLocked(true);
                slashRoutine = StartCoroutine(WaitForDoubleSlash());
            }
            else if (drawingAttack == null || !drawingAttack.TryStart(skill))
            {
                return false;
            }
            cooldownEnds[skill] = Time.time + skill.CooldownSeconds;
            return true;
        }

        public float GetCooldownRemaining(SkillDefinition skill)
        {
            return skill != null && cooldownEnds.TryGetValue(skill, out float end)
                ? Mathf.Max(0f, end - Time.time) : 0f;
        }

        public void OnDoubleSlashHit(int strike)
        {
            if (!slashActive || entity.IsDead || entity.isKnockback
                || strike != lastStrike + 1 || strike > 2) return;
            lastStrike = strike;
            Vector2 center = (Vector2)transform.position
                + new Vector2(doubleSlashOffset.x * slashDirection, doubleSlashOffset.y);
            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true, layerMask = enemyLayers, useTriggers = true
            };
            PhysicsScene2D physics = gameObject.scene.GetPhysicsScene2D();
            int count;
            while (true)
            {
                count = physics.OverlapBox(center, doubleSlashSize, 0f, filter, hitBuffer);
                if (count < hitBuffer.Length) break;
                System.Array.Resize(ref hitBuffer, hitBuffer.Length * 2);
            }
            hitEnemies.Clear();
            float damage = entity.RollAttackDamage(out bool critical) * doubleSlashDamageMultiplier;
            for (int i = 0; i < count; i++)
            {
                EnemyController enemy = hitBuffer[i] != null
                    ? hitBuffer[i].GetComponentInParent<EnemyController>() : null;
                if (enemy == null || enemy.HP <= 0f || !hitEnemies.Add(enemy)) continue;
                Vector3 textPosition = enemy.textSpawnPoint != null
                    ? enemy.textSpawnPoint.position : enemy.transform.position;
                GameObject textPrefab = enemy.damageTextPrefab;
                float before = enemy.HP;
                entity.NotifyAttackTarget(enemy);
                enemy.TakeDamage(damage, critical, false);
                float dealt = Mathf.Max(0f, before - enemy.HP);
                if (dealt <= 0f) continue;
                entity.ApplyLifeSteal(dealt);
                if (textPrefab != null)
                {
                    Vector3 offset = new Vector3(strike == 1 ? -0.18f : 0.18f, strike == 1 ? 0f : 0.2f, 0f);
                    GameObject text = Instantiate(textPrefab, textPosition + offset, Quaternion.identity);
                    text.GetComponent<DamageText>()?.Setup(damage, critical);
                }
                // Keep the target in range for the follow-up, then push it back.
                if (strike == 2 && enemy.HP > 0f) enemy.ApplyKnockback(slashDirection);
            }
        }

        private IEnumerator WaitForDash()
        {
            while (movement.IsDashing && !entity.IsDead) yield return null;
            movement.EndDash();
            animationController.FinishSkillAnimation();
            dashRoutine = null;
        }

        private IEnumerator WaitForDoubleSlash()
        {
            yield return null;
            while (animationController.IsAttacking && !entity.IsDead && !entity.isKnockback)
                yield return null;
            FinishDoubleSlash();
        }

        private void FinishDoubleSlash()
        {
            slashActive = false;
            slashRoutine = null;
            movement.SetMovementLocked(false);
            animationController.FinishSkillAnimation();
            if (!entity.IsDead) animationController.SetJumping(!movement.IsGrounded);
        }

        private void OnDisable()
        {
            if (dashRoutine != null)
            {
                StopCoroutine(dashRoutine);
                dashRoutine = null;
                if (movement != null) movement.EndDash();
                if (animationController != null) animationController.FinishSkillAnimation();
            }
            if (slashRoutine != null) StopCoroutine(slashRoutine);
            if (slashActive) FinishDoubleSlash();
            hitEnemies.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            var animation = GetComponent<PlayerIdleAnimation>();
            float direction = animation != null && animation.FacingDirectionX < 0f ? -1f : 1f;
            Gizmos.color = new Color(1f, 0.25f, 0.3f, 0.8f);
            Gizmos.DrawWireCube(transform.position
                + new Vector3(doubleSlashOffset.x * direction, doubleSlashOffset.y), doubleSlashSize);
        }
    }
}
