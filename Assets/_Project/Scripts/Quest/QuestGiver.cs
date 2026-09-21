using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using Unity.VisualScripting;
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
        private PlayerCurrency playerCurrency;

        public QuestDefinition Quest => quest;
        public bool CanOfferQuest => quest != null
            && offerDialogue != null
            && QuestManager.Instance != null
            && QuestManager.Instance.CanAccept(quest);

        // Call this from the NPC interaction script or a UnityEvent.
        public void OfferQuest()
        {
            if (quest == null || offerDialogue == null)
            {
                Debug.LogWarning("QuestGiver needs a Quest and Offer Dialogue.", this);
                return;
            }

            if (QuestManager.Instance == null || DialogueManager.Instance == null)
            {
                Debug.LogError("QuestGiver needs both QuestManager and DialogueManager.", this);
                return;
            }

            if (!CanOfferQuest)
                return;

            DialogueManager.Instance.StartQuestOffer(
                offerDialogue,
                quest.Title,
                ResolveOffer);
        }

        private void ResolveOffer(bool accepted)
        {
            if (accepted)
                QuestManager.Instance.AcceptQuest(quest);

            DialogueSequence followUp = accepted ? acceptedDialogue : declinedDialogue;
            if (followUp != null)
                DialogueManager.Instance.StartDialogue(followUp);
        }

        public void Interact()
        {
            if (QuestManager.Instance == null)
                return;

            QuestProgress progress =
                QuestManager.Instance.FindProgress(quest);

            // 아직 퀘스트를 받지 않음
            if (progress == null)
            {
                OfferQuest();
                return;
            }

            // 퀘스트 진행 중
            if (progress.Status == QuestStatus.Active)
            {
                if (progressDialogue != null)
                    DialogueManager.Instance.StartDialogue(progressDialogue);

                return;
            }

            // 목표를 모두 달성하여 완료 보고 가능
            if (progress.Status == QuestStatus.ReadyToTurnIn)
            {
                QuestManager.Instance.CompleteQuest(quest);

                if (completionDialogue != null)
                    DialogueManager.Instance.StartDialogue(completionDialogue);
                
                return;
            }

            if(progress.Status == QuestStatus.Completed)
            {
                if(RegularDialogue != null)
                    DialogueManager.Instance.StartDialogue(RegularDialogue);
                
                return;
            }
            // 이미 완료한 일반 퀘스트는 아무 작업도 하지 않습니다.
        }
    }
}
