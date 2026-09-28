using System;
using UnityEngine;

namespace EasternFantasy.Player
{
    [CreateAssetMenu(
        fileName = "PlayerLevelTable",
        menuName = "Eastern Fantasy/Player/Level Table")]
    public sealed class PlayerLevelTable : ScriptableObject
    {
        public const int MaximumLevel = 15;

        [Tooltip("Index 0 is level 1 to 2; index 13 is level 14 to 15.")]
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
            1350,
            1800,
            2400,
            3200,
            4000,
            5500
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
