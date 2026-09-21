using System;
using UnityEngine;

namespace EasternFantasy.Quest
{
    public enum QuestType
    {
        Main,
        Sub,
        Repeatable
    }

    [CreateAssetMenu(
        fileName = "QuestDefinition",
        menuName = "Eastern Fantasy/Quest/Quest Definition")]
    public sealed class QuestDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private string questId;
        [SerializeField] private QuestType questType;
        [SerializeField] private string title;
        [SerializeField, TextArea(2, 5)] private string objectiveDescription;
        [SerializeField, Min(1)] private int requiredAmount = 1;
        [SerializeField, Min(0)] private int rewardYeopjeon = 20;
        [SerializeField, Min(0)] private int rewardEXP = 100;
        public string QuestId => questId;
        public QuestType Type => questType;
        public string Title => title;
        public string ObjectiveDescription => objectiveDescription;
        public int RequiredAmount => requiredAmount;
        public int RewardYeopjeon => rewardYeopjeon;
        public int RewardEXP => rewardEXP;

        public string TypeLabel
        {
            get
            {
                switch (questType)
                {
                    case QuestType.Main: return "MAIN";
                    case QuestType.Sub: return "SUB";
                    case QuestType.Repeatable: return "REPEAT";
                    default: return questType.ToString().ToUpperInvariant();
                }
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(questId))
                questId = Guid.NewGuid().ToString("N");

            requiredAmount = Mathf.Max(1, requiredAmount);
            rewardYeopjeon = Mathf.Max(0, rewardYeopjeon);
            rewardEXP = Mathf.Max(0, rewardEXP);
        }
    }
}
