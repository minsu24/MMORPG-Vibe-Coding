using EasternFantasy.Dialogue;
using UnityEngine;

namespace EasternFantasy.Quest
{
    [DisallowMultipleComponent]
    public sealed class QuestGiver : MonoBehaviour
    {
        [SerializeField] private QuestDefinition quest;
        [SerializeField] private DialogueSequence offerDialogue;
        [SerializeField] private DialogueSequence acceptedDialogue;
        [SerializeField] private DialogueSequence declinedDialogue;
        [SerializeField] private DialogueSequence progressDialogue;
        [SerializeField] private DialogueSequence completionDialogue;
        [SerializeField] private DialogueSequence RegularDialogue;
        [SerializeField] private DialogueSequence unavailableDialogue;
        [SerializeField] private bool allowQuestOffer = true;
        [SerializeField] private bool allowQuestTurnIn = true;

        [Header("Optional next quest, offered after this NPC's first quest is completed")]
        [SerializeField] private QuestDefinition nextQuest;
        [SerializeField] private DialogueSequence nextOfferDialogue;
        [SerializeField] private DialogueSequence nextAcceptedDialogue;
        [SerializeField] private DialogueSequence nextDeclinedDialogue;
        [SerializeField] private DialogueSequence nextProgressDialogue;
        [SerializeField] private DialogueSequence nextCompletionDialogue;
        [SerializeField] private DialogueSequence nextRegularDialogue;
        [SerializeField] private bool allowNextQuestTurnIn = true;
        [SerializeField] private bool allowNextQuestOffer = true;

        private bool IsNextQuest => nextQuest != null && QuestManager.Instance != null
            && QuestManager.Instance.FindProgress(quest)?.Status == QuestStatus.Completed;
        public QuestDefinition Quest => IsNextQuest ? nextQuest : quest;
        public bool CanTurnInCurrentQuest => IsNextQuest ? allowNextQuestTurnIn : allowQuestTurnIn;
        private DialogueSequence OfferDialogue => IsNextQuest ? nextOfferDialogue : offerDialogue;
        public bool CanOfferQuest => (IsNextQuest ? allowNextQuestOffer : allowQuestOffer) && Quest != null && OfferDialogue != null
            && QuestManager.Instance != null && QuestManager.Instance.CanAccept(Quest);

        public void OfferQuest()
        {
            var manager = DialogueManager.Instance;
            if (!CanOfferQuest || manager == null || manager.IsDialogueActive)
                return;

            // Capture the offered quest and its responses before opening the choice UI.
            QuestDefinition offeredQuest = Quest;
            DialogueSequence accepted = IsNextQuest ? nextAcceptedDialogue : acceptedDialogue;
            DialogueSequence declined = IsNextQuest ? nextDeclinedDialogue : declinedDialogue;
            manager.StartQuestOffer(OfferDialogue, offeredQuest.Title, accept =>
            {
                if (accept && !QuestManager.Instance.AcceptQuest(offeredQuest)) return;
                DialogueSequence response = accept ? accepted : declined;
                if (response != null) manager.StartDialogue(response);
            });
        }

        public void Interact()
        {
            var manager = QuestManager.Instance;
            var dialogue = DialogueManager.Instance;
            if (manager == null || dialogue == null || dialogue.IsDialogueActive)
                return;

            QuestProgress progress = manager.FindProgress(Quest);
            if (progress == null)
            {
                if (CanOfferQuest) OfferQuest();
                else Play(unavailableDialogue != null ? unavailableDialogue : RegularDialogue);
                return;
            }
            if (progress.Status == QuestStatus.Active
                || (progress.Status == QuestStatus.ReadyToTurnIn && !CanTurnInCurrentQuest))
            {
                Play(IsNextQuest ? nextProgressDialogue : progressDialogue);
                return;
            }
            if (progress.Status == QuestStatus.ReadyToTurnIn)
            {
                DialogueSequence completion = IsNextQuest ? nextCompletionDialogue : completionDialogue;
                QuestDefinition completedQuest = Quest;
                if (manager.CompleteQuest(completedQuest))
                    dialogue.StartQuestCompletion(completion, completedQuest);
                return;
            }
            Play(IsNextQuest ? nextRegularDialogue : RegularDialogue);
        }

        private static void Play(DialogueSequence sequence)
        {
            if (sequence != null) DialogueManager.Instance.StartDialogue(sequence);
        }
    }
}
