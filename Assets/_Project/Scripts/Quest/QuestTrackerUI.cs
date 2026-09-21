using System.Text;
using TMPro;
using UnityEngine;

namespace EasternFantasy.Quest
{
    [DisallowMultipleComponent]
    public sealed class QuestTrackerUI : MonoBehaviour
    {
        [SerializeField] private GameObject trackerRoot;
        [SerializeField] private TMP_Text questListText;

        private QuestManager manager;

        private void Start()
        {
            manager = QuestManager.Instance;
            if (manager == null)
            {
                Debug.LogError("QuestTrackerUI needs an active QuestManager.", this);
                if (trackerRoot != null) trackerRoot.SetActive(false);
                return;
            }

            manager.QuestsChanged += Refresh;
            Refresh();
        }

        public void Refresh()
        {
            if (manager == null || trackerRoot == null || questListText == null)
                return;

            var builder = new StringBuilder();
            foreach (QuestProgress progress in manager.Quests)
            {
                if (progress.Status == QuestStatus.Completed)
                    continue;

                if (builder.Length > 0)
                    builder.AppendLine().AppendLine();

                QuestDefinition quest = progress.Definition;
                builder.Append('[').Append(quest.TypeLabel).Append("] ").AppendLine(quest.Title);
                builder.Append(quest.ObjectiveDescription);
                builder.Append("  ").Append(progress.CurrentAmount).Append('/').Append(quest.RequiredAmount);

                if (progress.Status == QuestStatus.ReadyToTurnIn)
                    builder.Append("  COMPLETE");
            }

            bool hasTrackedQuest = builder.Length > 0;
            trackerRoot.SetActive(hasTrackedQuest);
            questListText.text = builder.ToString();
        }

        private void OnDestroy()
        {
            if (manager != null)
                manager.QuestsChanged -= Refresh;
        }
    }
}
