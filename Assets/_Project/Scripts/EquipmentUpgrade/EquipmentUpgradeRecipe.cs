using System;
using System.Collections.Generic;
using EasternFantasy.Inventory;
using UnityEngine;

namespace EasternFantasy.EquipmentUpgrade
{
    public enum UpgradeEquipmentKind { Weapon, Armor }

    [Serializable]
    public sealed class UpgradeMaterial
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity = 1;
        public ItemDefinition Item => item;
        public int Quantity => quantity;
    }

    [CreateAssetMenu(fileName = "UpgradeRecipe", menuName = "Eastern Fantasy/Inventory/Equipment Upgrade Recipe")]
    public sealed class EquipmentUpgradeRecipe : ScriptableObject
    {
        [Tooltip("Enable after entering the materials and cost. Unconfigured recipes never grant free upgrades.")]
        [SerializeField] private bool configured;
        [SerializeField] private UpgradeEquipmentKind equipmentKind;
        [SerializeField] private ItemGrade sourceGrade;
        [SerializeField, Min(0)] private int costYeopjeon;
        [Tooltip("Up to four distinct ingredient items. Wearable equipment cannot be used as a material. Repeated entries are summed.")]
        [SerializeField] private UpgradeMaterial[] materials = Array.Empty<UpgradeMaterial>();
        public bool Configured => configured;
        public UpgradeEquipmentKind EquipmentKind => equipmentKind;
        public ItemGrade SourceGrade => sourceGrade;
        public ItemGrade TargetGrade => (ItemGrade)((int)sourceGrade + 1);
        public int CostYeopjeon => costYeopjeon;

        public bool Matches(ItemDefinition item) => item != null &&
            item.Category == ItemCategory.Equipment && item.Grade == sourceGrade &&
            (equipmentKind == UpgradeEquipmentKind.Weapon ? item.EquipmentSlot == EquipmentSlot.Weapon :
                item.EquipmentSlot == EquipmentSlot.Hat || item.EquipmentSlot == EquipmentSlot.Top ||
                item.EquipmentSlot == EquipmentSlot.Bottom || item.EquipmentSlot == EquipmentSlot.Gloves ||
                item.EquipmentSlot == EquipmentSlot.Shoes);

        public bool TryGetRequirements(out Dictionary<ItemDefinition, int> requirements, out string error)
        {
            requirements = new Dictionary<ItemDefinition, int>();
            error = "승급 조건이 아직 설정되지 않았습니다.";
            if (!configured) return false;
            error = "승급 재료 및 비용 설정을 확인해 주세요.";
            if (sourceGrade < ItemGrade.Common || sourceGrade >= ItemGrade.Mythic || costYeopjeon < 0 ||
                materials == null || materials.Length == 0) return false;
            foreach (var material in materials)
            {
                if (material == null || material.Item == null || material.Quantity < 1 ||
                    (material.Item.Category == ItemCategory.Equipment && material.Item.EquipmentSlot != EquipmentSlot.None)) return false;
                requirements.TryGetValue(material.Item, out int previous);
                long sum = (long)previous + material.Quantity;
                if (sum > int.MaxValue) return false;
                requirements[material.Item] = (int)sum;
            }
            if (requirements.Count > 4) return false;
            error = null;
            return true;
        }
    }
}