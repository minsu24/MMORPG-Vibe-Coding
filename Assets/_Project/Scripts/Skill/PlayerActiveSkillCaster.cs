using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

namespace EasternFantasy.Skill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement2D))]
    [RequireComponent(typeof(PlayerProjectileShooter))]
    [RequireComponent(typeof(PlayerEntity))]
    public sealed class PlayerActiveSkillCaster : MonoBehaviour
    {
        [Header("Skill Definitions")]
        [SerializeField] private SkillDefinition tripleTalismanSkill;
        [SerializeField] private SkillDefinition teleportSkill;
        [SerializeField] private SkillDefinition talismanShieldSkill;

        [Header("Triple Talisman")]
        [SerializeField, Min(0f)] private float projectileVerticalSpacing = 0.28f;
        [SerializeField, Min(0f)] private float damageMultiplier = 0.8f;

        [Header("Teleport")]
        [SerializeField, Min(0f)] private float teleportDistance = 4f;

        [Header("Talisman Shield")]
        [SerializeField, Range(0f, 0.95f)] private float shieldDamageReduction = 0.3f;
        [SerializeField, Min(0f)] private float shieldDuration = 5f;

        private readonly Dictionary<SkillDefinition, float> cooldownEnds =
            new Dictionary<SkillDefinition, float>();
        private PlayerMovement2D movement;
        private PlayerProjectileShooter shooter;
        private PlayerEntity entity;
        private PlayerIdleAnimation animationController;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement2D>();
            shooter = GetComponent<PlayerProjectileShooter>();
            entity = GetComponent<PlayerEntity>();
            animationController = GetComponent<PlayerIdleAnimation>();
        }

        public bool TryCast(SkillDefinition skill)
        {
            if (skill == null || skill.ActivationType != SkillActivationType.Active
                || GetCooldownRemaining(skill) > 0f)
                return false;
            if (entity.IsDead || !entity.TrySpendMana(skill.ManaCost))
                return false;
            bool cast;
            if (skill == tripleTalismanSkill)
            {
                cast = shooter.TryFireBurst(3, projectileVerticalSpacing, damageMultiplier);
            }
            else if (skill == teleportSkill)
            {
                float direction = animationController != null
                    ? animationController.FacingDirectionX
                    : 1f;
                movement.TeleportTo(
                    transform.position + Vector3.right * direction * teleportDistance,
                    transform.rotation);
                cast = true;
            }
            else if (skill == talismanShieldSkill)
            {
                entity.ActivateDamageReduction(shieldDamageReduction, shieldDuration);
                cast = true;
            }
            else
            {
                return false;
            }

            if (cast)
            {
                entity.TrySpendMana(skill.ManaCost);
                cooldownEnds[skill] = Time.time + skill.CooldownSeconds;
            }

            return cast;
        }

        public float GetCooldownRemaining(SkillDefinition skill)
        {
            if (skill == null || !cooldownEnds.TryGetValue(skill, out float endTime))
                return 0f;
            return Mathf.Max(0f, endTime - Time.time);
        }
    }
}
