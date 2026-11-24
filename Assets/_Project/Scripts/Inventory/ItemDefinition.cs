using System;
using UnityEngine;
using EasternFantasy.CharacterSelection;

namespace EasternFantasy.Inventory
{
    public enum ItemCategory
    {
        Equipment,
        Consumable,
        Miscellaneous,
        Accessory
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
        Bracelet,
        Ring2,
        Title
    }

    public enum AccessoryAbility
    {
        None,
        JumpHeight,
        HealthRegeneration,
        ManaRegeneration
    }

    public enum ConsumableEffect
    {
        None,
        RestoreHealth,
        RestoreMana,
        IncreaseAttackPower,
        IncreaseDamageReduction
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
        [SerializeField, Min(0f), Tooltip("칭호 공격력 비율. 0.05 = 5%.")]
        private float titleAttackBonusRate;
        [SerializeField] private Sprite titleBanner;
        [SerializeField, Min(0f)] private float defenseBonus;
        [Header("Accessory special ability")]
        [SerializeField] private AccessoryAbility specialAbility;
        [SerializeField, Min(0f), Tooltip("JumpHeight uses a fractional bonus (0.2 = 20%). Regeneration uses the amount restored each second.")]
        private float specialAbilityValue;
        [SerializeField] private ConsumableEffect consumableEffect;
        [SerializeField, Min(0f)] private float useAmount;
        [SerializeField, Tooltip("Use Amount is a fraction of the maximum stat when enabled. For example, 0.3 restores 30%.")]
        private bool restoreFractionOfMaximum;
        [SerializeField, Min(0f)] private float buffDuration;
        [SerializeField, Min(1)] private int maximumStack = 99;
        [SerializeField, Min(0)] private int buyPrice;
        [SerializeField, Min(0)] private int sellPrice;

        [SerializeField] private bool cannotSell;
        [SerializeField, Tooltip("Enemy loot stops after this character first acquires this item, even after selling it.")]
        private bool firstAcquisitionOnly;
        public bool FirstAcquisitionOnly => firstAcquisitionOnly;
        [Tooltip("The ordinary version of this material. Tutorial and ordinary stacks stay separate, but both can be consumed.")]
        [SerializeField] private ItemDefinition materialSource;

        public bool CanSell => !cannotSell;
        public ItemDefinition MaterialIdentity => materialSource != null ? materialSource : this;
        public bool MatchesMaterial(ItemDefinition other) => other != null
            && Category == ItemCategory.Miscellaneous && other.Category == ItemCategory.Miscellaneous
            && MaterialIdentity == other.MaterialIdentity;
        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => HasGrade && gradeAppearance != null && gradeAppearance.GetIcon(Grade) != null
            ? gradeAppearance.GetIcon(Grade) : icon;
        public bool HasGrade => equipmentSlot != EquipmentSlot.Title && grade != ItemGrade.None;
        public ItemGrade Grade => HasGrade ? grade : ItemGrade.None;
        private static ItemGradeAppearance sharedGradeFrames;
        public Sprite GradeFrame
        {
            get
            {
                if (!HasGrade) return null;
                Sprite frame = gradeAppearance != null ? gradeAppearance.GetFrame(Grade) : gradeFrame;
                if (frame != null || (!IsAccessory && category != ItemCategory.Consumable)) return frame;
                if (sharedGradeFrames == null)
                    sharedGradeFrames = Resources.Load<ItemGradeAppearance>("UI/ItemGradeFrames");
                return sharedGradeFrames != null ? sharedGradeFrames.GetFrame(Grade) : null;
            }
        }
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
        public ItemCategory InventoryCategory => category == ItemCategory.Accessory ? ItemCategory.Equipment : category;
        public bool IsAccessory => category == ItemCategory.Accessory
            || equipmentSlot == EquipmentSlot.Necklace || equipmentSlot == EquipmentSlot.Bracelet || equipmentSlot == EquipmentSlot.Ring;
        public bool IsEquippable => equipmentSlot != EquipmentSlot.None &&
            (category == ItemCategory.Equipment || (category == ItemCategory.Accessory &&
                (equipmentSlot == EquipmentSlot.Necklace || equipmentSlot == EquipmentSlot.Bracelet || equipmentSlot == EquipmentSlot.Ring)));
        public AccessoryAbility SpecialAbility => specialAbility;
        public float SpecialAbilityValue => Mathf.Max(0f, specialAbilityValue);
        public string SpecialAbilityDescription => specialAbility switch
        {
            AccessoryAbility.JumpHeight => $"점프 높이 +{SpecialAbilityValue * 100f:0.#}%",
            AccessoryAbility.HealthRegeneration => $"1초마다 체력 {SpecialAbilityValue:0.#} 회복",
            AccessoryAbility.ManaRegeneration => $"1초마다 마나 {SpecialAbilityValue:0.#} 회복",
            _ => string.Empty
        };
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
        public float TitleAttackBonusRate => equipmentSlot == EquipmentSlot.Title ? titleAttackBonusRate : 0f;
        public Sprite TitleBanner => titleBanner;
        public float DefenseBonus => defenseBonus;
        public ConsumableEffect ConsumableEffect => consumableEffect;
        public float UseAmount => useAmount;
        public bool RestoreFractionOfMaximum => restoreFractionOfMaximum;
        public float BuffDuration => buffDuration;
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
            buffDuration = Mathf.Max(0f, buffDuration);
            maximumStack = Mathf.Max(1, maximumStack);
            buyPrice = Mathf.Max(0, buyPrice);
            sellPrice = Mathf.Clamp(sellPrice, 0, buyPrice);
        }
    }
}
