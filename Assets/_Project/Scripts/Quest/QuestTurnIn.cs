using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using UnityEngine;

namespace EasternFantasy.Quest
{
    public class QuestTurnIn : MonoBehaviour
    {
        [SerializeField] private QuestDefinition questDefinition;
        [SerializeField] private DialogueSequence completionDialogue;
        private PlayerCurrency playerCurrency;

        public void TryTurnIn()
        {
            if (QuestManager.Instance == null)
                return;

            QuestProgress progress =
                QuestManager.Instance.FindProgress(questDefinition);

            if (progress == null ||
                progress.Status != QuestStatus.ReadyToTurnIn)
            {
                return;
            }

            QuestManager.Instance.CompleteQuest(questDefinition);

            if (completionDialogue != null &&
                DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(completionDialogue);
            }

            playerCurrency.Add(questDefinition.RequiredAmount);
            playerCurrency.Add(questDefinition.RewardYeopjeon);
            // 이 위치에서 경험치, 골드, 아이템 등의 보상을 지급합니다.
        }
    }
}