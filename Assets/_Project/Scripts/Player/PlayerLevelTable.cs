using System;
using UnityEngine;

namespace EasternFantasy.Player
{
    [CreateAssetMenu(
        fileName = "PlayerLevelTable",
        menuName = "Eastern Fantasy/Player/Level Table")]
    public sealed class PlayerLevelTable : ScriptableObject
    {
        public const int MaximumLevel = 10;

        [Tooltip("Index 0 is the EXP needed for level 1 to reach level 2. Index 8 is level 9 to 10.")]
        [SerializeField] private int[] requiredExperience =
        {
            100,
            150,
            225,
            325,
            450,
            600,
            800,
            1050,
            1350
        };

        public int GetRequiredExperience(int currentLevel)
        {
            if (currentLevel < 1 || currentLevel >= MaximumLevel)
                return 0;

            return requiredExperience[currentLevel - 1];
        }

        private void OnValidate()
        {
            if (requiredExperience == null)
                requiredExperience = new int[MaximumLevel - 1];

            if (requiredExperience.Length != MaximumLevel - 1)
                Array.Resize(ref requiredExperience, MaximumLevel - 1);

            for (int i = 0; i < requiredExperience.Length; i++)
                requiredExperience[i] = Mathf.Max(1, requiredExperience[i]);
        }
    }
}
