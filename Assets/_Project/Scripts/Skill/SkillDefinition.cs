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
        [SerializeField] private Sprite icon;
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
        public Sprite Icon => icon;
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
        }
    }
}
