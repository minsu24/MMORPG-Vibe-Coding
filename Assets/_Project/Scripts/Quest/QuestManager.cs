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

    public sealed class QuestProgress
    {
        public QuestProgress(QuestDefinition definition)
        {
            Definition = definition;
            Status = QuestStatus.Active;
        }

        public QuestDefinition Definition { get; }
        public int CurrentAmount { get; internal set; }
        public QuestStatus Status { get; internal set; }
    }

    [DisallowMultipleComponent]
    public sealed partial class QuestManager : MonoBehaviour
    {
        private readonly List<QuestProgress> quests = new List<QuestProgress>();
        private GameObject player;
        private PlayerProgression playerProgression;

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
            EasternFantasy.UI.MapRegionDiscovery.RevealQuestDestinations(definition);
            QuestsChanged?.Invoke();
            return true;
        }

        public bool AddProgress(QuestDefinition definition, int amount = 1)
        {
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
            if (rewards.Count > 0)
            {
                var inventory = FindFirstObjectByType<PlayerInventory>();
                if (inventory == null || !inventory.TryAddItems(rewards))
                {
                    progress.Status = QuestStatus.ReadyToTurnIn;
                    EasternFantasy.UI.SystemNoticeUI.Show("물약 보상을 받을 수 있도록 소비 인벤토리를 정리한 뒤 다시 완료해 주세요.", this);
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
            if (Instance == this)
                Instance = null;
        }
    }
}
