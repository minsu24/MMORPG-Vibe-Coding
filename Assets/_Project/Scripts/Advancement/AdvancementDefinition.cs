using EasternFantasy.Dialogue;
using EasternFantasy.Skill;
using UnityEngine;

namespace EasternFantasy.Advancement
{
    public enum AdvancementRank
    {
        Base,
        First
    }

    [CreateAssetMenu(
        fileName = "AdvancementDefinition",
        menuName = "Eastern Fantasy/Advancement/Definition")]
    public sealed class AdvancementDefinition : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField, Min(1)] private int requiredLevel = 10;
        [SerializeField] private AdvancementRank resultingRank = AdvancementRank.First;
        [SerializeField] private SkillTier unlockedSkillTier = SkillTier.FirstAdvancement;
        [SerializeField] private DialogueSequence dialogue;
        [SerializeField, Min(0f)] private float attackBonus = 5f;
        [SerializeField, Min(0f)] private float defenseBonus = 3f;

        public string DisplayName => displayName;
        public int RequiredLevel => requiredLevel;
        public AdvancementRank ResultingRank => resultingRank;
        public SkillTier UnlockedSkillTier => unlockedSkillTier;
        public DialogueSequence Dialogue => dialogue;
        public float AttackBonus => attackBonus;
        public float DefenseBonus => defenseBonus;
    }
}
