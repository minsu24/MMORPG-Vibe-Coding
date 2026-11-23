using EasternFantasy.Dialogue;
using EasternFantasy.Saving;
using UnityEngine;

namespace EasternFantasy.Quest
{
    public sealed class TravelerQuestPlacement : MonoBehaviour
    {
        [SerializeField] private GameObject traveler;
        [SerializeField] private QuestDefinition relocationQuest;
        [SerializeField] private bool destination;
        [SerializeField] private QuestDefinition finalQuest;
        [SerializeField] private MON_SkullMonk boss;
        private bool resolved;
        private bool movedToOuterGrounds;

        private void Update()
        {
            if (SaveGameService.IsRestoring)
            {
                resolved = false;
                return;
            }
            var manager = QuestManager.Instance;
            if (manager == null || traveler == null) return;

            if (boss != null && boss.WasDefeatedThisSession
                && manager.FindProgress(finalQuest)?.Status == QuestStatus.Active)
                manager.AddProgress(finalQuest);

            var finalStatus = manager.FindProgress(finalQuest)?.Status;
            bool visible;
            if (relocationQuest == null)
            {
                visible = finalStatus == QuestStatus.ReadyToTurnIn
                    || (finalStatus == QuestStatus.Completed && traveler.activeSelf
                        && DialogueManager.Instance?.IsDialogueActive == true);
            }
            else
            {
                // The first move only happens after leaving the current scene.
                if (!resolved)
                {
                    movedToOuterGrounds = manager.FindProgress(relocationQuest) != null;
                    resolved = true;
                }
                visible = destination
                    ? movedToOuterGrounds && finalStatus != QuestStatus.ReadyToTurnIn && finalStatus != QuestStatus.Completed
                    : !movedToOuterGrounds || finalStatus == QuestStatus.Completed;
            }

            if (traveler.activeSelf != visible)
                traveler.SetActive(visible);
        }
    }
}
