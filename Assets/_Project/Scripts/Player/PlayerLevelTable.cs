using System;
using UnityEngine;

namespace EasternFantasy.Player
{
    [CreateAssetMenu(
        fileName = "PlayerLevelTable",
        menuName = "Eastern Fantasy/Player/Level Table")]
    public sealed class PlayerLevelTable : ScriptableObject
    {
        public const int MaximumLevel = 30;

        private static readonly int[] DefaultRequiredExperience =
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
            5500,
            6600,
            7900,
            9500,
            11400,
            13700,
            16400,
            19700,
            23600,
            28300,
            34000,
            40800,
            49000,
            58800,
            70600,
            84700
        };

        [Tooltip("Index 0 is level 1 to 2; index 28 is level 29 to 30.")]
        [SerializeField] private int[] requiredExperience = (int[])DefaultRequiredExperience.Clone();

        public int GetRequiredExperience(int currentLevel)
        {
            if (currentLevel < 1 || currentLevel >= MaximumLevel)
                return 0;

            int index = currentLevel - 1;
            return requiredExperience != null && index < requiredExperience.Length
                ? Mathf.Max(1, requiredExperience[index])
                : DefaultRequiredExperience[index];
        }

        private void OnValidate()
        {
            int previousLength = requiredExperience != null ? requiredExperience.Length : 0;
            if (requiredExperience == null || requiredExperience.Length != MaximumLevel - 1)
                Array.Resize(ref requiredExperience, MaximumLevel - 1);

            for (int i = 0; i < requiredExperience.Length; i++)
                requiredExperience[i] = i >= previousLength
                    ? DefaultRequiredExperience[i] : Mathf.Max(1, requiredExperience[i]);
        }
    }
}
