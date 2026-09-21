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
        public bool Playable => playable;
        public int Attack => attack;
        public int Defense => defense;
        public int Mobility => mobility;
        public int Range => range;
    }
}
