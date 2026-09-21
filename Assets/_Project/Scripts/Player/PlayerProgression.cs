using System;
using UnityEngine;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerEntity))]
    public sealed class PlayerProgression : MonoBehaviour
    {
        [SerializeField] private PlayerLevelTable levelTable;
        [SerializeField, Range(1, PlayerLevelTable.MaximumLevel)] private int currentLevel = 1;
        [SerializeField, Min(0)] private int currentExperience;

        public int CurrentLevel => currentLevel;
        public int CurrentExperience => currentExperience;
        public int RequiredExperience => levelTable != null
            ? levelTable.GetRequiredExperience(currentLevel)
            : 0;
        public bool IsMaximumLevel => currentLevel >= PlayerLevelTable.MaximumLevel;
        public float ExperienceNormalized => IsMaximumLevel
            ? 1f
            : RequiredExperience > 0
                ? (float)currentExperience / RequiredExperience
                : 0f;

        public event Action<int, int> ExperienceChanged;
        public event Action<int> LevelChanged;

        private void Awake()
        {
            currentLevel = Mathf.Clamp(currentLevel, 1, PlayerLevelTable.MaximumLevel);
            currentExperience = Mathf.Max(0, currentExperience);

            if (levelTable == null)
            {
                Debug.LogError("PlayerProgression requires a PlayerLevelTable.", this);
                enabled = false;
                return;
            }

            NormalizeProgress();
        }

        // Float keeps compatibility with the existing enemy reward field.
        public void AddExperience(float amount)
        {
            if (!enabled || IsMaximumLevel || amount <= 0f)
                return;

            currentExperience += Mathf.Max(0, Mathf.RoundToInt(amount));
            NormalizeProgress();
            ExperienceChanged?.Invoke(currentExperience, RequiredExperience);
        }

        private void NormalizeProgress()
        {
            while (!IsMaximumLevel)
            {
                int required = RequiredExperience;
                if (required <= 0 || currentExperience < required)
                    break;

                currentExperience -= required;
                currentLevel++;
                LevelChanged?.Invoke(currentLevel);
            }

            if (IsMaximumLevel)
                currentExperience = 0;
            else
                currentExperience = Mathf.Clamp(currentExperience, 0, RequiredExperience - 1);
        }

        private void OnValidate()
        {
            currentLevel = Mathf.Clamp(currentLevel, 1, PlayerLevelTable.MaximumLevel);
            currentExperience = Mathf.Max(0, currentExperience);
        }
    }
}
