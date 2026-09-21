using UnityEngine;

namespace EasternFantasy.Player
{
    [CreateAssetMenu(
        fileName = "PlayerStatGrowthTable",
        menuName = "Eastern Fantasy/Player/Stat Growth Table")]
    public sealed class PlayerStatGrowthTable : ScriptableObject
    {
        [Header("Class")]
        [SerializeField] private string className = "DOSA";
        [SerializeField] private string resourceName = "MP";

        [Header("Growth per level after level 1")]
        [SerializeField, Min(0f)] private float healthPerLevel = 8f;
        [SerializeField, Min(0f)] private float resourcePerLevel = 25f;
        [SerializeField, Min(0f)] private float attackPerLevel = 3f;
        [SerializeField, Min(0f)] private float defensePerLevel = 2f;

        public string ClassName => className;
        public string ResourceName => resourceName;
        public float HealthPerLevel => healthPerLevel;
        public float ResourcePerLevel => resourcePerLevel;
        public float AttackPerLevel => attackPerLevel;
        public float DefensePerLevel => defensePerLevel;

        public float GetHealthBonus(int level) => healthPerLevel * GetGrowthLevels(level);
        public float GetResourceBonus(int level) => resourcePerLevel * GetGrowthLevels(level);
        public float GetAttackBonus(int level) => attackPerLevel * GetGrowthLevels(level);
        public float GetDefenseBonus(int level) => defensePerLevel * GetGrowthLevels(level);

        private static int GetGrowthLevels(int level)
        {
            return Mathf.Max(0, level - 1);
        }
    }
}
