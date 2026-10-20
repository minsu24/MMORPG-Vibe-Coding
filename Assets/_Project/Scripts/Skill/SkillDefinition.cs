using System;
using UnityEngine;

namespace EasternFantasy.Skill
{
    public enum SkillTier
    {
        Base,
        FirstAdvancement
    }

    public enum SkillEffectType
    {
        None,
        MaximumHealth,
        MaximumMana,
        MoveSpeed
    }

    public enum SkillActivationType
    {
        Passive,
        Active
    }

    public enum SkillResourceCost
    {
        Mana = 0,
        None = 1,
        FullEnergy = 2,
        Energy = 3
    }

    [Serializable]
    public sealed class SkillRequirement
    {
        [SerializeField] private SkillDefinition skill;
        [SerializeField, Min(1)] private int requiredLevel = 1;

        public SkillDefinition Skill => skill;
        public int RequiredLevel => requiredLevel;
    }

    [CreateAssetMenu(
        fileName = "SkillDefinition",
        menuName = "Eastern Fantasy/Skill/Skill Definition")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private string skillId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 4)] private string shortDescription;
        [SerializeField, TextArea(3, 8)] private string detailedDescription;
        [SerializeField] private SkillTier tier;
        [SerializeField] private SkillActivationType activationType;
        [SerializeField, Min(1)] private int maximumLevel = 1;
        [SerializeField, Min(0f)] private float cooldownSeconds;
        [SerializeField, Min(0f)] private float cooldownReductionPerLevel;
        [SerializeField, Min(0f)] private float dashDistance;
        [SerializeField, Min(0f)] private float dashDistancePerLevel;
        [SerializeField, Min(0f)] private float damageMultiplier;
        [SerializeField, Min(0f)] private float damageMultiplierPerLevel;
        [SerializeField, Min(1)] private int damageHitCount = 1;
        [SerializeField, Min(0f)] private float manaCost;
        [SerializeField] private SkillResourceCost resourceCost;
        [SerializeField, Min(0f), Tooltip("Amount spent when Resource Cost is Energy.")] private float energyCost;

        [SerializeField] private Sprite icon;
        [SerializeField, Tooltip("Empty for class skills; companion identifier for pet active skills.")] private string companionId;
        [SerializeField] private SkillEffectType effectType;
        [SerializeField, Min(0f)] private float effectPerLevel;
        [SerializeField] private SkillRequirement[] requirements = Array.Empty<SkillRequirement>();

        public string SkillId => skillId;
        public string DisplayName => displayName;
        public string ShortDescription => shortDescription;
        public string DetailedDescription => detailedDescription;
        public SkillTier Tier => tier;
        public SkillActivationType ActivationType => activationType;
        public int MaximumLevel => maximumLevel;
        public float CooldownSeconds => cooldownSeconds;
        public int DamageHitCount => Mathf.Max(1, damageHitCount);

        public float GetDamageMultiplier(int level)
        {
            return damageMultiplier + damageMultiplierPerLevel * (Mathf.Clamp(level, 1, maximumLevel) - 1);
        }

        public float GetCooldownSeconds(int level)
        {
            return Mathf.Max(0f, cooldownSeconds
                - cooldownReductionPerLevel * (Mathf.Clamp(level, 1, maximumLevel) - 1));
        }

        public float GetDashDistance(int level)
        {
            return dashDistance + dashDistancePerLevel * (Mathf.Clamp(level, 1, maximumLevel) - 1);
        }

        public string GetLevelEffectDescription(int level)
        {
            string result = damageMultiplier > 0f
                ? $"공격력의 {GetDamageMultiplier(level) * 100f:0.#}% × {DamageHitCount}회"
                : string.Empty;
            if (cooldownReductionPerLevel > 0f)
                result += (result.Length > 0 ? " · " : string.Empty)
                    + $"재사용 대기시간 {GetCooldownSeconds(level):0.#}초";
            if (dashDistance > 0f)
                result += (result.Length > 0 ? " · " : string.Empty)
                    + $"돌진 거리 {GetDashDistance(level):0.##}";
            return result;
        }
        public float ManaCost => manaCost;
        public SkillResourceCost ResourceCost => resourceCost;
        public float EnergyCost => energyCost;

        public Sprite Icon => icon;
        public string CompanionId => companionId;
        public bool IsCompanionSkill => !string.IsNullOrEmpty(companionId);
        public SkillEffectType EffectType => effectType;
        public float EffectPerLevel => effectPerLevel;
        public SkillRequirement[] Requirements => requirements;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(skillId))
                skillId = Guid.NewGuid().ToString("N");

            maximumLevel = Mathf.Max(1, maximumLevel);
            effectPerLevel = Mathf.Max(0f, effectPerLevel);
            cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
            manaCost = Mathf.Max(0f, manaCost);
            energyCost = Mathf.Max(0f, energyCost);
        }
    }
}
