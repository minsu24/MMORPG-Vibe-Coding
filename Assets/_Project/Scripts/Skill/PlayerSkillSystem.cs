using System;
using System.Collections.Generic;
using EasternFantasy.Advancement;
using EasternFantasy.Player;
using UnityEngine;

namespace EasternFantasy.Skill
{
    public interface IPlayerSkillCaster
    {
        bool TryCast(SkillDefinition skill);
        float GetCooldownRemaining(SkillDefinition skill);
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerProgression))]
    public sealed partial class PlayerSkillSystem : MonoBehaviour, IPlayerSkillCaster
    {
        [SerializeField, Min(0)] private int skillPoints;
        [SerializeField] private SkillDefinition[] availableSkills = Array.Empty<SkillDefinition>();
        public const int QuickSlotCount = 8;
        [SerializeField] private SkillDefinition[] quickSlots = new SkillDefinition[QuickSlotCount];

        private readonly Dictionary<SkillDefinition, int> skillLevels =
            new Dictionary<SkillDefinition, int>();
        private readonly HashSet<SkillDefinition> companionSkills = new HashSet<SkillDefinition>();
        private readonly Dictionary<SkillDefinition, float> companionCooldownEnds = new Dictionary<SkillDefinition, float>();
        private PetCompanionActor companion;
        private PlayerProgression progression;

        public void SetCompanionSkillUnlocked(SkillDefinition skill, bool unlocked)
        {
            if (skill == null || !skill.IsCompanionSkill) return;
            bool changed = unlocked ? companionSkills.Add(skill) : companionSkills.Remove(skill);
            if (changed) SkillsChanged?.Invoke();
        }
        public void BindCompanion(PetCompanionActor actor) => companion = actor;
        public void UnbindCompanion(PetCompanionActor actor) { if (companion == actor) companion = null; }

        public string GetCompanionSkillUnavailableReason(SkillDefinition skill)
        {
            if (skill == null || !skill.IsCompanionSkill) return string.Empty;
            if (!companionSkills.Contains(skill)) return "동행요괴를 먼저 소환해 주세요";
            if (companion == null || companion.CompanionId != skill.CompanionId) return "도깨비를 장착해 주세요";
            return companion.GetActiveSkillUnavailableReason();
        }

        public bool TryCast(SkillDefinition skill)
        {
            if (skill == null) return false;
            var input = GetComponent<PlayerInputReader>();
            if (Time.timeScale <= 0f || (input != null && !input.CanPerformCombatAction)) return false;
            if (!skill.IsCompanionSkill)
            {
                var caster = ResolveClassSkillCaster();
                return caster != null && caster.TryCast(skill);
            }
            if (GetSkillLevel(skill) <= 0 || GetCooldownRemaining(skill) > 0f
                || !string.IsNullOrEmpty(GetCompanionSkillUnavailableReason(skill))) return false;
            if (!companion.TryUseActiveSkill(skill)) return false;
            companionCooldownEnds[skill] = Time.time + skill.CooldownSeconds;
            return true;
        }

        public float GetCooldownRemaining(SkillDefinition skill)
        {
            if (skill == null) return 0f;
            if (!skill.IsCompanionSkill) return ResolveClassSkillCaster()?.GetCooldownRemaining(skill) ?? 0f;
            return companionCooldownEnds.TryGetValue(skill, out float end) ? Mathf.Max(0f, end - Time.time) : 0f;
        }
        private PlayerAdvancementSystem advancementSystem;

        public int SkillPoints => skillPoints;
        public IReadOnlyList<SkillDefinition> AvailableSkills => availableSkills;
        public IReadOnlyList<SkillDefinition> QuickSlots => quickSlots;
        public event Action SkillsChanged;
        public event Action QuickSlotsChanged;

        public void ConfigureClass(SkillDefinition[] skills)
        {
            availableSkills = skills ?? Array.Empty<SkillDefinition>();
            skillLevels.Clear();
            quickSlots = new SkillDefinition[QuickSlotCount];
            EnsureSkillState();
        }

        private void Awake()
        {
            EnsureSkillState();
        }

        private void EnsureSkillState()
        {
            if (quickSlots == null || quickSlots.Length != QuickSlotCount)
            {
                SkillDefinition[] previous = quickSlots;
                quickSlots = new SkillDefinition[QuickSlotCount];
                if (previous != null)
                    Array.Copy(previous, quickSlots, Mathf.Min(previous.Length, QuickSlotCount));
            }
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
            progression.LeveledUp += GrantLevelUpPoint;
            SkillsChanged?.Invoke();
        }

        public int GetSkillLevel(SkillDefinition skill)
        {
            EnsureSkillState();
            if (skill != null && skill.IsCompanionSkill) return companionSkills.Contains(skill) ? 1 : 0;
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
            IPlayerSkillCaster caster = ResolveSkillCaster();
            return caster != null && caster.TryCast(quickSlots[index]);
        }

        public IPlayerSkillCaster ResolveSkillCaster() => this;

        private IPlayerSkillCaster ResolveClassSkillCaster()
        {
            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
                if (component != this && component is IPlayerSkillCaster caster
                    && component.isActiveAndEnabled)
                    return caster;

            return null;
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

        public void GrantSkillPoints(int amount)
        {
            if (amount <= 0) return;
            skillPoints += amount;
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
                progression.LeveledUp -= GrantLevelUpPoint;
        }
    }
}
