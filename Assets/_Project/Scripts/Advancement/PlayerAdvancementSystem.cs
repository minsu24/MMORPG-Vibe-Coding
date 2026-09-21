using System;
using System.Collections;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using EasternFantasy.Skill;
using UnityEngine;

namespace EasternFantasy.Advancement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerProgression))]
    [RequireComponent(typeof(PlayerSkillSystem))]
    public sealed class PlayerAdvancementSystem : MonoBehaviour
    {
        [Header("Automatic self-advancement")]
        [SerializeField] private AdvancementDefinition automaticAdvancement;
        [SerializeField] private bool startAutomatically = true;

        [Header("Runtime state")]
        [SerializeField] private AdvancementRank currentRank = AdvancementRank.Base;
        [SerializeField] private SkillTier highestUnlockedSkillTier = SkillTier.Base;

        private PlayerProgression progression;
        private PlayerSkillSystem skillSystem;
        private AdvancementDefinition pendingAdvancement;
        private DialogueManager observedDialogueManager;
        private Coroutine automaticRoutine;
        private float attackBonus;
        private float defenseBonus;

        public AdvancementRank CurrentRank => currentRank;
        public float AttackBonus => attackBonus;
        public float DefenseBonus => defenseBonus;
        public bool IsWaitingForDialogue { get; private set; }

        public event Action Advanced;

        private void Start()
        {
            progression = GetComponent<PlayerProgression>();
            skillSystem = GetComponent<PlayerSkillSystem>();
            progression.LevelChanged += HandleLevelChanged;
            EvaluateAutomaticAdvancement();
        }

        public bool IsTierUnlocked(SkillTier tier)
        {
            return tier <= highestUnlockedSkillTier;
        }

        public bool CanAdvance(AdvancementDefinition definition)
        {
            return definition != null
                && progression != null
                && progression.CurrentLevel >= definition.RequiredLevel
                && currentRank < definition.ResultingRank
                && !IsWaitingForDialogue;
        }

        // An NPC advancement interaction can call this with its own definition.
        public bool TryStartAdvancement(AdvancementDefinition definition)
        {
            if (!CanAdvance(definition))
                return false;

            DialogueManager manager = DialogueManager.Instance;
            if (manager == null || manager.IsDialogueActive || definition.Dialogue == null)
                return false;

            pendingAdvancement = definition;
            observedDialogueManager = manager;
            observedDialogueManager.DialogueEnded += CompletePendingAdvancement;
            IsWaitingForDialogue = true;

            if (manager.StartDialogue(definition.Dialogue))
                return true;

            observedDialogueManager.DialogueEnded -= CompletePendingAdvancement;
            observedDialogueManager = null;
            pendingAdvancement = null;
            IsWaitingForDialogue = false;
            return false;
        }

        private void HandleLevelChanged(int newLevel)
        {
            EvaluateAutomaticAdvancement();
        }

        private void EvaluateAutomaticAdvancement()
        {
            if (!startAutomatically || !CanAdvance(automaticAdvancement) || automaticRoutine != null)
                return;

            automaticRoutine = StartCoroutine(WaitForDialogueAndStart());
        }

        private IEnumerator WaitForDialogueAndStart()
        {
            while (CanAdvance(automaticAdvancement))
            {
                DialogueManager manager = DialogueManager.Instance;
                if (manager != null && !manager.IsDialogueActive)
                {
                    TryStartAdvancement(automaticAdvancement);
                    break;
                }

                yield return null;
            }

            automaticRoutine = null;
        }

        private void CompletePendingAdvancement()
        {
            if (observedDialogueManager != null)
                observedDialogueManager.DialogueEnded -= CompletePendingAdvancement;

            AdvancementDefinition completed = pendingAdvancement;
            observedDialogueManager = null;
            pendingAdvancement = null;
            IsWaitingForDialogue = false;

            if (completed == null || currentRank >= completed.ResultingRank)
                return;

            currentRank = completed.ResultingRank;
            if (completed.UnlockedSkillTier > highestUnlockedSkillTier)
                highestUnlockedSkillTier = completed.UnlockedSkillTier;
            attackBonus += completed.AttackBonus;
            defenseBonus += completed.DefenseBonus;
            skillSystem.NotifyAdvancementChanged();
            Advanced?.Invoke();
        }

        private void OnDestroy()
        {
            if (progression != null)
                progression.LevelChanged -= HandleLevelChanged;
            if (observedDialogueManager != null)
                observedDialogueManager.DialogueEnded -= CompletePendingAdvancement;
        }
    }
}
