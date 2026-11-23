using EasternFantasy.Dialogue;
using UnityEngine;

namespace EasternFantasy.Quest
{
    public sealed class QuestDialogueSet : ScriptableObject
    {
        [SerializeField] private QuestDefinition quest;
        [SerializeField] private DialogueSequence offer, accepted, declined, progress, completion, regular;

        public QuestDefinition Quest => quest;
        public DialogueSequence Offer => offer;
        public DialogueSequence Accepted => accepted;
        public DialogueSequence Declined => declined;
        public DialogueSequence Progress => progress;
        public DialogueSequence Completion => completion;
        public DialogueSequence Regular => regular;
    }
}
