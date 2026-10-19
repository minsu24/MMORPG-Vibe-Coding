using System;
using EasternFantasy.Inventory;
using UnityEngine;

namespace EasternFantasy.Player
{
    [Serializable]
    public sealed class PetCompanionProgress
    {
        [SerializeField] private int level;
        [SerializeField] private bool equipped;
        [SerializeField] private bool unlocked;
        public bool IsUnlocked => unlocked || IsSummoned;
        public bool TryUnlock(PlayerInventory inventory, ItemDefinition soul)
        {
            if (!IsUnlocked && inventory != null && soul != null && inventory.GetMaterialQuantity(soul) > 0) unlocked = true;
            return IsUnlocked;
        }
        public bool IsSummoned => level > 0;
        public bool IsEquipped => IsSummoned && equipped;

        public bool TryEquip()
        {
            if (!IsSummoned) return false;
            equipped = true;
            return true;
        }

        public void Unequip() => equipped = false;
        public int Level => level;
        public int Attack => IsSummoned ? 5 + level - 1 : 5;
        public int GrowthCost => Mathf.Max(1, level);

        public bool TrySummon(PlayerInventory inventory, ItemDefinition soul)
        {
            if (IsSummoned || inventory == null || soul == null || inventory.GetMaterialQuantity(soul) < 1) return false;
            // Commit state before inventory notification so observers see the new companion.
            level = 1;
            if (inventory.RemoveMaterial(soul, 1)) return true;
            level = 0;
            return false;
        }

        public bool TryGrow(PlayerInventory inventory, ItemDefinition fragment)
        {
            if (!IsSummoned || level == int.MaxValue || inventory == null || fragment == null
                || inventory.GetMaterialQuantity(fragment) < GrowthCost) return false;
            int cost = GrowthCost;
            level++;
            if (inventory.RemoveMaterial(fragment, cost)) return true;
            level--;
            return false;
        }
    }
}
