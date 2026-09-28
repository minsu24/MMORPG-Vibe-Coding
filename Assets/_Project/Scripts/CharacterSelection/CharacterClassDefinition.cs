using System;
using EasternFantasy.Advancement;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using EasternFantasy.Skill;
using UnityEngine;

namespace EasternFantasy.CharacterSelection
{
    public enum CharacterClassId
    {
        Dosa,
        Monk,
        Swordswoman
    }

    [CreateAssetMenu(
        fileName = "CharacterClass",
        menuName = "Eastern Fantasy/Character/Class Definition")]
    public sealed class CharacterClassDefinition : ScriptableObject
    {
        [SerializeField] private CharacterClassId classId;
        [SerializeField] private string displayName;
        [SerializeField] private string combatRole;
        [SerializeField, TextArea(3, 6)] private string description;
        [SerializeField] private Sprite portrait;
        [SerializeField] private bool playable;

        [Header("Runtime")]
        [Tooltip("Player prefab spawned when this class is selected.")]
        [SerializeField] private GameObject playerPrefab;
        [Tooltip("Name and portrait used for the player side of dialogue UI.")]
        [SerializeField] private DialogueCharacter dialogueCharacter;
        [SerializeField] private PlayerStatGrowthTable statGrowth;
        [SerializeField] private SkillDefinition[] availableSkills = Array.Empty<SkillDefinition>();
        [SerializeField] private AdvancementDefinition automaticAdvancement;

        [Header("Stats (1-5)")]
        [SerializeField, Range(1, 5)] private int attack = 3;
        [SerializeField, Range(1, 5)] private int defense = 3;
        [SerializeField, Range(1, 5)] private int mobility = 3;
        [SerializeField, Range(1, 5)] private int range = 3;

        public CharacterClassId ClassId => classId;
        public string DisplayName => displayName;
        public string CombatRole => combatRole;
        public string Description => description;
        public Sprite Portrait => portrait;
        public bool Playable => playable && playerPrefab != null;
        public bool MarkedPlayable => playable;
        public GameObject PlayerPrefab => playerPrefab;
        public DialogueCharacter DialogueCharacter => dialogueCharacter;
        public PlayerStatGrowthTable StatGrowth => statGrowth;
        public SkillDefinition[] AvailableSkills => availableSkills;
        public AdvancementDefinition AutomaticAdvancement => automaticAdvancement;
        public int Attack => attack;
        public int Defense => defense;
        public int Mobility => mobility;
        public int Range => range;

        private void OnValidate()
        {
            if (availableSkills == null)
                availableSkills = Array.Empty<SkillDefinition>();
        }
    }
}
