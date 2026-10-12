using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasternFantasy.Inventory
{
    public enum EquipmentSetStat { MoveSpeed, Attack, Defense, MaximumHealth, MaximumMana }
    public enum EquipmentSetModifier { Flat, Percent }

    [Serializable]
    public sealed class EquipmentSetEffect
    {
        [SerializeField] private EquipmentSetStat stat;
        [SerializeField] private EquipmentSetModifier modifier = EquipmentSetModifier.Percent;
        [Tooltip("Percent uses percentage points: 10 means +10%.")]
        [SerializeField, Min(0f)] private float value = 10f;
        public EquipmentSetStat Stat => stat;
        public EquipmentSetModifier Modifier => modifier;
        public float Value => Mathf.Max(0f, value);
        public string Description
        {
            get
            {
                string label;
                switch (stat)
                {
                    case EquipmentSetStat.Attack: label = "공격력"; break;
                    case EquipmentSetStat.Defense: label = "방어력"; break;
                    case EquipmentSetStat.MaximumHealth: label = "최대 체력"; break;
                    case EquipmentSetStat.MaximumMana: label = "최대 마나"; break;
                    default: label = "이동속도"; break;
                }
                return label + " +" + Value.ToString("0.##") +
                    (modifier == EquipmentSetModifier.Percent ? "%" : "");
            }
        }
    }

    [Serializable]
    public sealed class EquipmentSetBonus
    {
        [SerializeField, Min(1)] private int requiredPieces = 6;
        [SerializeField] private EquipmentSetEffect[] effects = { new EquipmentSetEffect() };
        public int RequiredPieces => Mathf.Max(1, requiredPieces);
        public IReadOnlyList<EquipmentSetEffect> Effects => effects ?? Array.Empty<EquipmentSetEffect>();
    }

    [CreateAssetMenu(fileName = "EquipmentSet", menuName = "Eastern Fantasy/Inventory/Equipment Set")]
    public sealed class EquipmentSetDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "수련자 세트";
        [Tooltip("Empty accepts every equipment slot. Different grades of the same set count together.")]
        [SerializeField] private EquipmentSlot[] eligibleSlots = Array.Empty<EquipmentSlot>();
        [Tooltip("All reached thresholds apply cumulatively.")]
        [SerializeField] private EquipmentSetBonus[] bonuses = { new EquipmentSetBonus() };
        public string DisplayName => displayName;
        public IReadOnlyList<EquipmentSetBonus> Bonuses => bonuses ?? Array.Empty<EquipmentSetBonus>();
        public bool IncludesSlot(EquipmentSlot slot) => slot != EquipmentSlot.None &&
            (eligibleSlots == null || eligibleSlots.Length == 0 || Array.IndexOf(eligibleSlots, slot) >= 0);
    }
}