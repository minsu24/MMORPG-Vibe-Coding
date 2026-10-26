using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using UnityEngine;

namespace EasternFantasy.Quest
{
    [CreateAssetMenu(menuName = "Eastern Fantasy/Quest/Toad Hunt Flow")]
    public sealed class ToadHuntDefinition : ScriptableObject
    {
        public QuestDefinition prerequisite;
        public DialogueSequence prerequisiteCompletion;
        public QuestDefinition quest;
        public ItemDefinition necklace;
        public DialogueSequence discoveryMonologue;
        public DialogueSequence accessoryExplanation;
        public QuestRewardRelic relicPrefab;
        public string bossScene = "Marshland";
    }
}
