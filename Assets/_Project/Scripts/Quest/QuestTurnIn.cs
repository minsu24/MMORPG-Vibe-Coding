using EasternFantasy.Dialogue;
using UnityEngine;

namespace EasternFantasy.Quest
{
    public class QuestTurnIn : MonoBehaviour
    {
        [SerializeField] private QuestDefinition questDefinition;
        [SerializeField] private DialogueSequence completionDialogue;

        public void TryTurnIn()
        {
            var manager = QuestManager.Instance;
            var dialogue = DialogueManager.Instance;
            if (manager == null || dialogue == null || dialogue.IsDialogueActive) return;

            // QuestManager owns payment; the dialogue only reports that successful payment.
            if (manager.CompleteQuest(questDefinition))
                dialogue.StartQuestCompletion(completionDialogue, questDefinition);
        }
    }
}
