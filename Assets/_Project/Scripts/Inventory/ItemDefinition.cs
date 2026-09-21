using System;
using UnityEngine;

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
        Shoes
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
        [SerializeField] private EquipmentSlot equipmentSlot;
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
        public Sprite Icon => icon;
        public ItemCategory Category => category;
        public EquipmentSlot EquipmentSlot => equipmentSlot;
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
