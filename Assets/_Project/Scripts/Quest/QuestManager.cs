using System;
using System.Collections.Generic;
using UnityEngine;
using EasternFantasy.Inventory;
using EasternFantasy.Player;

namespace EasternFantasy.Quest
{
    public enum QuestStatus
    {
        Active,
        ReadyToTurnIn,
        Completed
    }

    [Serializable]
    public sealed class QuestProgress
    {
        [SerializeField] private QuestDefinition definition;
        [SerializeField] private int currentAmount;
        [SerializeField] private QuestStatus status;
        public QuestProgress(QuestDefinition definition)
        {
            this.definition = definition;
            Status = QuestStatus.Active;
        }

        public QuestDefinition Definition => definition;
        public int CurrentAmount { get => currentAmount; internal set => currentAmount = value; }
        public QuestStatus Status { get => status; internal set => status = value; }
    }

    [DisallowMultipleComponent]
    public sealed partial class QuestManager : MonoBehaviour
    {
        [SerializeField] private List<QuestProgress> quests = new List<QuestProgress>();
        private GameObject player;
        private PlayerProgression playerProgression;
        private PlayerInventory collectionInventory;
        private float nextInventoryCheck;

        private void Update()
        {
            if (Time.unscaledTime < nextInventoryCheck) return;
            nextInventoryCheck = Time.unscaledTime + .25f;
            BindCollectionInventory();
        }

        private void BindCollectionInventory()
        {
            var inventory = FindFirstObjectByType<PlayerInventory>();
            if (collectionInventory == inventory) return;
            if (collectionInventory != null) collectionInventory.InventoryChanged -= RefreshCollectionProgress;
            collectionInventory = inventory;
            if (collectionInventory != null) collectionInventory.InventoryChanged += RefreshCollectionProgress;
            RefreshCollectionProgress();
        }

        public void RefreshCollectionProgress()
        {
            bool changed = false;
            foreach (var progress in quests)
            {
                if (progress.Definition == null || progress.Definition.RequiredItem == null || progress.Status == QuestStatus.Completed) continue;
                int amount = collectionInventory != null ? Mathf.Min(collectionInventory.GetMaterialQuantity(progress.Definition.RequiredItem), progress.Definition.RequiredAmount) : 0;
                var status = amount >= progress.Definition.RequiredAmount ? QuestStatus.ReadyToTurnIn : QuestStatus.Active;
                if (progress.CurrentAmount == amount && progress.Status == status) continue;
                progress.CurrentAmount = amount;
                progress.Status = status;
                changed = true;
            }
            if (changed) QuestsChanged?.Invoke();
        }

        public static QuestManager Instance { get; private set; }
        public IReadOnlyList<QuestProgress> Quests => quests;
        public event Action QuestsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            player = GameObject.FindGameObjectWithTag("Player");
            playerProgression = player != null ? player.GetComponent<PlayerProgression>() : null;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            // Awake is not called again after an in-play script reload.
            if (Instance == null) Instance = this;
        }

        public bool CanAccept(QuestDefinition definition)
        {
            if (definition == null)
                return false;

            if (definition.PrerequisiteQuest != null
                && FindProgress(definition.PrerequisiteQuest)?.Status != QuestStatus.Completed)
                return false;

            QuestProgress existing = FindProgress(definition);
            if (existing == null)
                return true;

            return definition.Type == QuestType.Repeatable
                && existing.Status == QuestStatus.Completed;
        }

        public bool AcceptQuest(QuestDefinition definition)
        {
            if (!CanAccept(definition))
                return false;

            QuestProgress existing = FindProgress(definition);
            if (existing != null)
                quests.Remove(existing);

            quests.Add(new QuestProgress(definition));
            BindCollectionInventory();
            RefreshCollectionProgress();
            EasternFantasy.UI.MapRegionDiscovery.RevealQuestDestinations(definition);
            QuestsChanged?.Invoke();
            return true;
        }

        public bool AddProgress(QuestDefinition definition, int amount = 1)
        {
            if (definition == null || definition.RequiredItem != null) return false;
            QuestProgress progress = FindProgress(definition);
            if (progress == null || progress.Status != QuestStatus.Active || amount <= 0)
                return false;

            progress.CurrentAmount = Mathf.Min(
                progress.CurrentAmount + amount,
                definition.RequiredAmount);

            if (progress.CurrentAmount >= definition.RequiredAmount)
                progress.Status = QuestStatus.ReadyToTurnIn;

            QuestsChanged?.Invoke();
            return true;
        }

        public bool CompleteQuest(QuestDefinition definition)
        {
            BindCollectionInventory();
            RefreshCollectionProgress();
            QuestProgress progress = FindProgress(definition);
            if (progress == null || progress.Status != QuestStatus.ReadyToTurnIn)
                return false;

            progress.Status = QuestStatus.Completed;
            var rewards = new Dictionary<ItemDefinition, int>();
            foreach (var reward in definition.RewardItems)
            {
                if (reward == null || reward.Item == null || reward.Quantity <= 0) continue;
                rewards.TryGetValue(reward.Item, out int amount);
                rewards[reward.Item] = amount + reward.Quantity;
            }
            if (rewards.Count > 0 || definition.RequiredItem != null)
            {
                var inventory = FindFirstObjectByType<PlayerInventory>();
                if (inventory == null || !inventory.TryExchangeMaterialForItems(definition.RequiredItem,
                    definition.RequiredItem != null ? definition.RequiredAmount : 0, rewards))
                {
                    progress.Status = QuestStatus.ReadyToTurnIn;
                    EasternFantasy.UI.SystemNoticeUI.ShowFailure("제출할 아이템 수량과 보상을 받을 인벤토리 공간을 확인해 주세요.", this);
                    return false;
                }
            }
            if (definition.RewardYeopjeon > 0)
            {
                PlayerCurrency currency = FindFirstObjectByType<PlayerCurrency>(FindObjectsInactive.Include);
                currency?.Add(definition.RewardYeopjeon);
            }
            if (definition.RewardEXP > 0)
            {
                if (playerProgression == null)
                    playerProgression = FindFirstObjectByType<PlayerProgression>();
                playerProgression?.AddExperience(definition.RewardEXP);
            }
            QuestsChanged?.Invoke();
            return true;
        }

        public QuestProgress FindProgress(QuestDefinition definition)
        {
            if (definition == null)
                return null;

            return quests.Find(progress => progress.Definition == definition);
        }

        private void OnDestroy()
        {
            if (collectionInventory != null) collectionInventory.InventoryChanged -= RefreshCollectionProgress;
            if (Instance == this)
                Instance = null;
        }
    }
}
