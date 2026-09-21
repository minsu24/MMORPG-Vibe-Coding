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
    public sealed class QuestManager : MonoBehaviour
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
            playerProgression = player.GetComponent<PlayerProgression>();
            DontDestroyOnLoad(gameObject);
        }

        public bool CanAccept(QuestDefinition definition)
        {
            if (definition == null)
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
            if (definition.RewardYeopjeon > 0)
            {
                PlayerCurrency currency = FindFirstObjectByType<PlayerCurrency>(FindObjectsInactive.Include);
                currency?.Add(definition.RewardYeopjeon);
                playerProgression.AddExperience(definition.RewardEXP);

                
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
