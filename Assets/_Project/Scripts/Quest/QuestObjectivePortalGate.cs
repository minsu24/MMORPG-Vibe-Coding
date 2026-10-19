using TMPro;
using UnityEngine;

namespace EasternFantasy.Quest
{
    [DisallowMultipleComponent, RequireComponent(typeof(PortalManager))]
    public sealed class QuestObjectivePortalGate : MonoBehaviour
    {
        [SerializeField] private QuestDefinition requiredQuest;
        [SerializeField] private TMP_Text blockedHint;
        private PortalManager portal;
        private QuestManager manager;

        private void Awake() => portal = GetComponent<PortalManager>();
        private void OnEnable() => BindManager();
        // PortalManager also initializes its lock in Awake; restore after all Awakes.
        private void Start() => BindManager();
        private void Update()
        {
            if (manager != QuestManager.Instance) BindManager();
        }
        private void BindManager()
        {
            if (manager != null) manager.QuestsChanged -= Refresh;
            manager = QuestManager.Instance;
            if (manager != null) manager.QuestsChanged += Refresh;
            Refresh();
        }
        private void Refresh()
        {
            var progress = manager != null ? manager.FindProgress(requiredQuest) : null;
            bool unlocked = requiredQuest != null && progress != null
                && progress.CurrentAmount >= requiredQuest.RequiredAmount;
            portal.SetLocked(!unlocked);
            if (blockedHint == null) return;
            blockedHint.gameObject.SetActive(!unlocked);
            if (!unlocked && requiredQuest != null)
                blockedHint.text = progress == null
                    ? "촌장에게 거미 처치 의뢰를 받으세요"
                    : $"거미를 처치해야 지나갈 수 있습니다\n{progress.CurrentAmount} / {requiredQuest.RequiredAmount}";
        }
        private void OnDisable()
        {
            if (manager != null) manager.QuestsChanged -= Refresh;
            manager = null;
        }
    }
}