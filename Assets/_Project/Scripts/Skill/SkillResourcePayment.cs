using EasternFantasy.Player;

namespace EasternFantasy.Skill
{
    public static class SkillResourcePayment
    {
        public static bool CanAfford(PlayerEntity player, SkillDefinition skill)
        {
            if (player == null || player.IsDead || skill == null) return false;
            switch (skill.ResourceCost)
            {
                case SkillResourceCost.None:
                    return true;
                case SkillResourceCost.Mana:
                    return player.MP >= skill.ManaCost;
                case SkillResourceCost.Energy:
                    return player.Energy != null && player.Energy.Current >= skill.EnergyCost;
                case SkillResourceCost.FullEnergy:
                    return player.Energy != null && player.Energy.IsFull;
                default:
                    return false;
            }
        }

        public static bool TrySpend(PlayerEntity player, SkillDefinition skill)
        {
            if (!CanAfford(player, skill)) return false;
            switch (skill.ResourceCost)
            {
                case SkillResourceCost.None:
                    return true;
                case SkillResourceCost.Mana:
                    return player.TrySpendMana(skill.ManaCost);
                case SkillResourceCost.Energy:
                    return player.Energy.TrySpend(skill.EnergyCost);
                case SkillResourceCost.FullEnergy:
                    return player.Energy.TrySpendFullCharge();
                default:
                    return false;
            }
        }

        public static string GetUnavailableReason(PlayerEntity player, SkillDefinition skill)
        {
            if (CanAfford(player, skill)) return string.Empty;
            if (player == null || skill == null) return "캐릭터를 찾을 수 없습니다";
            switch (skill.ResourceCost)
            {
                case SkillResourceCost.Mana:
                    return $"MP 부족 (필요 {skill.ManaCost:0.#})";
                case SkillResourceCost.Energy:
                    return player.Energy == null ? "기력을 사용할 수 없습니다"
                        : $"기력 부족 (필요 {skill.EnergyCost:0.#})";
                case SkillResourceCost.FullEnergy:
                    return player.Energy == null ? "기력을 사용할 수 없습니다"
                        : "기력이 완전히 충전되어야 합니다";
                default:
                    return string.Empty;
            }
        }
    }
}