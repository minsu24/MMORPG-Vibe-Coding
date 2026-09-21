using System;
using System.Collections.Generic;
using EasternFantasy.Advancement;
using EasternFantasy.Player;
using UnityEngine;

namespace EasternFantasy.Skill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerProgression))]
    public sealed class PlayerSkillSystem : MonoBehaviour
    {
        [SerializeField, Min(0)] private int skillPoints;
        [SerializeField] private SkillDefinition[] availableSkills = Array.Empty<SkillDefinition>();
        [SerializeField] private SkillDefinition[] quickSlots = new SkillDefinition[3];

        private readonly Dictionary<SkillDefinition, int> skillLevels =
            new Dictionary<SkillDefinition, int>();
        private PlayerProgression progression;
        private PlayerAdvancementSystem advancementSystem;

        public int SkillPoints => skillPoints;
        public IReadOnlyList<SkillDefinition> AvailableSkills => availableSkills;
        public IReadOnlyList<SkillDefinition> QuickSlots => quickSlots;
        public event Action SkillsChanged;
        public event Action QuickSlotsChanged;

        private void Awake()
        {
            EnsureSkillState();
        }

        private void EnsureSkillState()
        {
            if (quickSlots == null || quickSlots.Length != 3)
                quickSlots = new SkillDefinition[3];
            if (availableSkills == null)
                availableSkills = Array.Empty<SkillDefinition>();
            foreach (SkillDefinition skill in availableSkills)
                if (skill != null && !skillLevels.ContainsKey(skill))
                    skillLevels.Add(skill, 0);
        }

        private void Start()
        {
            EnsureSkillState();
            progression = GetComponent<PlayerProgression>();
            advancementSystem = GetComponent<PlayerAdvancementSystem>();
            skillPoints += Mathf.Max(0, progression.CurrentLevel - 1);
            progression.LevelChanged += GrantLevelUpPoint;
            SkillsChanged?.Invoke();
        }

        public int GetSkillLevel(SkillDefinition skill)
        {
            EnsureSkillState();
            return skill != null && skillLevels.TryGetValue(skill, out int level)
                ? level
                : 0;
        }

        public bool CanLearn(SkillDefinition skill)
        {
            if (skill == null || skillPoints <= 0)
                return false;
            if (advancementSystem == null
                ? skill.Tier != SkillTier.Base
                : !advancementSystem.IsTierUnlocked(skill.Tier))
                return false;
            if (!skillLevels.ContainsKey(skill) || GetSkillLevel(skill) >= skill.MaximumLevel)
                return false;

            foreach (SkillRequirement requirement in skill.Requirements)
            {
                if (requirement == null || requirement.Skill == null)
                    continue;
                if (GetSkillLevel(requirement.Skill) < requirement.RequiredLevel)
                    return false;
            }

            return true;
        }

        public bool LearnSkill(SkillDefinition skill)
        {
            if (!CanLearn(skill))
                return false;

            skillLevels[skill]++;
            skillPoints--;
            SkillsChanged?.Invoke();
            return true;
        }

        public float GetEffectTotal(SkillEffectType effectType)
        {
            float total = 0f;
            foreach (KeyValuePair<SkillDefinition, int> pair in skillLevels)
            {
                if (pair.Key != null && pair.Key.EffectType == effectType)
                    total += pair.Key.EffectPerLevel * pair.Value;
            }

            return total;
        }

        public bool CanPlaceInQuickSlot(SkillDefinition skill)
        {
            return skill != null
                && skill.ActivationType == SkillActivationType.Active
                && GetSkillLevel(skill) > 0;
        }

        public bool AssignQuickSlot(int index, SkillDefinition skill)
        {
            if (index < 0 || index >= quickSlots.Length || !CanPlaceInQuickSlot(skill))
                return false;

            for (int i = 0; i < quickSlots.Length; i++)
                if (quickSlots[i] == skill)
                    quickSlots[i] = null;
            quickSlots[index] = skill;
            QuickSlotsChanged?.Invoke();
            return true;
        }

        public void ClearQuickSlot(int index)
        {
            if (index < 0 || index >= quickSlots.Length || quickSlots[index] == null)
                return;
            quickSlots[index] = null;
            QuickSlotsChanged?.Invoke();
        }

        public bool TryUseQuickSlot(int index)
        {
            if (index < 0 || index >= quickSlots.Length || !CanPlaceInQuickSlot(quickSlots[index]))
                return false;
            PlayerActiveSkillCaster caster = GetComponent<PlayerActiveSkillCaster>();
            return caster != null && caster.TryCast(quickSlots[index]);
        }

        public string GetRequirementText(SkillDefinition skill)
        {
            if (skill != null
                && (advancementSystem == null
                    ? skill.Tier != SkillTier.Base
                    : !advancementSystem.IsTierUnlocked(skill.Tier)))
                return "필요: 1차 전직";

            if (skill == null || skill.Requirements.Length == 0)
                return string.Empty;

            SkillRequirement requirement = skill.Requirements[0];
            return requirement != null && requirement.Skill != null
                ? $"필요: {requirement.Skill.DisplayName} Lv.{requirement.RequiredLevel}"
                : string.Empty;
        }

        public void NotifyAdvancementChanged()
        {
            SkillsChanged?.Invoke();
        }

        private void GrantLevelUpPoint(int newLevel)
        {
            skillPoints++;
            SkillsChanged?.Invoke();
        }

        private void OnDestroy()
        {
            if (progression != null)
                progression.LevelChanged -= GrantLevelUpPoint;
        }
    }
}
