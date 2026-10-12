using System;
using UnityEngine;
using EasternFantasy.CharacterSelection;

namespace EasternFantasy.Inventory
{
    public enum ItemCategory
    {
        Equipment,
        Consumable,
        Miscellaneous
    }

    public enum EquipmentSlot
    {
        None,
        Weapon,
        Hat,
        Top,
        Bottom,
        Gloves,
        Shoes,
        Ring,
        Necklace,
        Bracelet
    }

    public enum ConsumableEffect
    {
        None,
        RestoreHealth,
        RestoreMana
    }

    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "Eastern Fantasy/Inventory/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemCategory category;
        [SerializeField] private ItemGrade grade = ItemGrade.Common;
        [SerializeField] private Sprite gradeFrame;
        [SerializeField] private ItemGradeAppearance gradeAppearance;
        [Tooltip("Definitions of the same item at each grade, including this one.")]
        [SerializeField] private ItemDefinition[] gradeVariants = Array.Empty<ItemDefinition>();
        [SerializeField] private EquipmentSlot equipmentSlot;
        [Tooltip("장착 가능한 직업 목록. 비어 있으면 모든 직업이 장착할 수 있습니다.")]
        [SerializeField] private CharacterClassId[] allowedClasses = Array.Empty<CharacterClassId>();
        [SerializeField] private EquipmentSetDefinition equipmentSet;
        [SerializeField, Min(0f)] private float attackBonus;
        [SerializeField, Min(0f)] private float defenseBonus;
        [SerializeField] private ConsumableEffect consumableEffect;
        [SerializeField, Min(0f)] private float useAmount;
        [SerializeField, Min(1)] private int maximumStack = 99;
        [SerializeField, Min(0)] private int buyPrice;
        [SerializeField, Min(0)] private int sellPrice;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => HasGrade && gradeAppearance != null && gradeAppearance.GetIcon(Grade) != null
            ? gradeAppearance.GetIcon(Grade) : icon;
        public bool HasGrade => category != ItemCategory.Consumable;
        public ItemGrade Grade => HasGrade ? grade : ItemGrade.None;
        public Sprite GradeFrame => !HasGrade ? null : gradeAppearance != null ? gradeAppearance.GetFrame(Grade) : gradeFrame;
        public string GradeName => ItemGradeUtility.GetName(Grade);
        public Color GradeColor => ItemGradeUtility.GetColor(Grade);
        public string GradedDisplayName => HasGrade ? $"[{GradeName}] {displayName}" : displayName;
        public ItemDefinition GetGradeVariant(ItemGrade targetGrade)
        {
            if (!HasGrade || targetGrade == ItemGrade.None) return null;
            if (Grade == targetGrade) return this;
            if (gradeVariants == null) return null;
            foreach (ItemDefinition variant in gradeVariants)
                if (variant != null && variant.Grade == targetGrade && variant.Category == category
                    && variant.EquipmentSlot == equipmentSlot) return variant;
            return null;
        }
        public ItemCategory Category => category;
        public EquipmentSlot EquipmentSlot => equipmentSlot;
        public EquipmentSetDefinition EquipmentSet => category == ItemCategory.Equipment ? equipmentSet : null;
        public bool HasClassRestriction => allowedClasses != null && allowedClasses.Length > 0;
        public bool CanBeEquippedBy(CharacterClassId characterClass) =>
            !HasClassRestriction || Array.IndexOf(allowedClasses, characterClass) >= 0;
        public string AllowedClassesLabel => !HasClassRestriction ? "모든 직업" :
            string.Join(", ", Array.ConvertAll(allowedClasses, GetClassName));
        private static string GetClassName(CharacterClassId characterClass)
        {
            switch (characterClass)
            {
                case CharacterClassId.Dosa: return "도사";
                case CharacterClassId.Monk: return "수도승";
                case CharacterClassId.Swordswoman: return "여검사";
                default: return characterClass.ToString();
            }
        }
        public float AttackBonus => attackBonus;
        public float DefenseBonus => defenseBonus;
        public ConsumableEffect ConsumableEffect => consumableEffect;
        public float UseAmount => useAmount;
        public int MaximumStack => maximumStack;
        public int BuyPrice => buyPrice;
        public int SellPrice => sellPrice;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
                itemId = Guid.NewGuid().ToString("N");

            attackBonus = Mathf.Max(0f, attackBonus);
            defenseBonus = Mathf.Max(0f, defenseBonus);
            useAmount = Mathf.Max(0f, useAmount);
            maximumStack = Mathf.Max(1, maximumStack);
            buyPrice = Mathf.Max(0, buyPrice);
            sellPrice = Mathf.Clamp(sellPrice, 0, buyPrice);
        }
    }
}
