using UnityEngine;

namespace EasternFantasy.Quest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(QuestGiver))]
    public sealed class QuestAvailabilityIndicator : MonoBehaviour
    {
        [SerializeField] private QuestGiver questGiver;
        [SerializeField] private GameObject indicatorRoot;

        private QuestManager subscribedManager;

        private void Awake()
        {
            if (questGiver == null)
                questGiver = GetComponent<QuestGiver>();
        }

        private void OnEnable()
        {
            RebindManager();
            Refresh();
        }

        private void Update()
        {
            if (subscribedManager != QuestManager.Instance)
                RebindManager();
        }

        private void RebindManager()
        {
            if (subscribedManager != null)
                subscribedManager.QuestsChanged -= Refresh;

            subscribedManager = QuestManager.Instance;
            if (subscribedManager != null)
                subscribedManager.QuestsChanged += Refresh;

            Refresh();
        }

        private void Refresh()
        {
            if (indicatorRoot != null)
                indicatorRoot.SetActive(questGiver != null && questGiver.CanOfferQuest);
        }

        private void OnDisable()
        {
            if (subscribedManager != null)
                subscribedManager.QuestsChanged -= Refresh;
            subscribedManager = null;
        }
    }
}
