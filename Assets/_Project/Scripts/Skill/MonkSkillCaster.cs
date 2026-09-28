using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

namespace EasternFantasy.Skill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MonkEnergy))]
    [RequireComponent(typeof(PlayerEntity))]
    public sealed class MonkSkillCaster : MonoBehaviour, IPlayerSkillCaster
    {
        [SerializeField] private SkillDefinition straightPunchSkill;
        [SerializeField] private SkillDefinition parrySkill;
        [SerializeField] private SkillDefinition energyWaveSkill;
        [SerializeField, Min(0f)] private float punchEnergyOnHit = 10f;
        [SerializeField, Min(0f)] private float parryWindow = 0.35f;
        [SerializeField] private Vector2 punchOffset = new Vector2(0.85f, 0.15f);
        [SerializeField] private Vector2 punchSize = new Vector2(1.45f, 1f);
        [SerializeField, Min(0f)] private float punchDamageMultiplier = 1.25f;
        [SerializeField, Min(0.1f)] private float waveRange = 8f;
        [SerializeField, Min(0.1f)] private float waveSpeed = 14f;
        [SerializeField, Min(0f)] private float waveDamageMultiplier = 3.5f;
        [SerializeField] private bool invertFacingDirection = true;
        [SerializeField] private LayerMask enemyLayers;

        private readonly Collider2D[] hitBuffer = new Collider2D[32];
        private readonly HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>();
        private readonly Dictionary<SkillDefinition, float> cooldownEnds = new Dictionary<SkillDefinition, float>();
        private PlayerEntity entity;
        private MonkEnergy energy;
        private PlayerIdleAnimation animationController;

        private void Awake()
        {
            entity = GetComponent<PlayerEntity>();
            energy = GetComponent<MonkEnergy>();
            animationController = GetComponent<PlayerIdleAnimation>();
            if (enemyLayers.value == 0)
            {
                int layer = LayerMask.NameToLayer("Enemy");
                if (layer >= 0) enemyLayers = 1 << layer;
            }
        }

        public bool TryCast(SkillDefinition skill)
        {
            if (skill == null || entity.IsDead || Time.timeScale <= 0f
                || GetCooldownRemaining(skill) > 0f) return false;

            if (skill == straightPunchSkill)
            {
                if (animationController != null && !animationController.TryRequestAttack())
                    return false;
                int hitCount = DamageInFront(punchOffset, punchSize, punchDamageMultiplier);
                if (hitCount > 0) energy.Gain(punchEnergyOnHit);
            }
            else if (skill == parrySkill)
            {
                energy.BeginParry(parryWindow);
            }
            else if (skill == energyWaveSkill)
            {
                if (!energy.IsFull) return false;
                if (animationController != null && !animationController.TryRequestAttack())
                    return false;
                MonkEnergyWave.Launch(entity, energy, GetDirection(),
                    waveRange, waveSpeed, waveDamageMultiplier, enemyLayers);
                energy.TrySpendFullCharge();
            }
            else return false;

            if (skill.CooldownSeconds > 0f)
                cooldownEnds[skill] = Time.time + skill.CooldownSeconds;
            return true;
        }

        public float GetCooldownRemaining(SkillDefinition skill)
        {
            return skill != null && cooldownEnds.TryGetValue(skill, out float end)
                ? Mathf.Max(0f, end - Time.time) : 0f;
        }

        private int DamageInFront(Vector2 offset, Vector2 size, float multiplier)
        {
            float direction = GetDirection();
            Vector2 center = (Vector2)transform.position + new Vector2(offset.x * direction, offset.y);
            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = enemyLayers,
                useTriggers = true
            };
            int count = Physics2D.OverlapBox(center, size, 0f, filter, hitBuffer);
            hitEnemies.Clear();
            float damage = entity.RollAttackDamage(out bool critical) * multiplier;
            for (int i = 0; i < count; i++)
            {
                EnemyController enemy = hitBuffer[i] != null
                    ? hitBuffer[i].GetComponentInParent<EnemyController>() : null;
                if (enemy == null || !hitEnemies.Add(enemy)) continue;
                float before = enemy.HP;
                enemy.TakeDamage(damage, critical);
                float dealt = Mathf.Max(0f, before - enemy.HP);
                if (dealt <= 0f) continue;
                entity.ApplyLifeSteal(dealt);
                energy.RegisterCombat();
                if (enemy.HP > 0f) enemy.ApplyKnockback(direction);
            }
            return hitEnemies.Count;
        }

        private float GetDirection()
        {
            float direction = animationController != null ? animationController.FacingDirectionX : 1f;
            if (Mathf.Approximately(direction, 0f)) direction = 1f;
            return invertFacingDirection ? -direction : direction;
        }
    }
}
