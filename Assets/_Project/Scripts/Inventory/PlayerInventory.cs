using System;
using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

namespace EasternFantasy.Inventory
{
    [Serializable]
    public sealed class InventoryStack
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity = 1;
        [NonSerialized] private int slotIndex;

        public ItemDefinition Item => item;
        public int Quantity => quantity;
        public int SlotIndex => slotIndex;

        public InventoryStack(ItemDefinition definition, int amount, int index = 0)
        {
            item = definition;
            quantity = Mathf.Max(1, amount);
            slotIndex = Mathf.Max(0, index);
        }

        public void SetQuantity(int amount)
        {
            quantity = Mathf.Max(0, amount);
        }

        public void SetSlotIndex(int index)
        {
            slotIndex = Mathf.Max(0, index);
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerEntity))]
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private InventoryStack[] initialItems = Array.Empty<InventoryStack>();

        private readonly List<InventoryStack> items = new List<InventoryStack>();
        private readonly Dictionary<EquipmentSlot, ItemDefinition> equippedItems =
            new Dictionary<EquipmentSlot, ItemDefinition>();
        private PlayerEntity playerEntity;
        private bool initialized;

        public const int SlotsPerCategory = 20;

        public IReadOnlyList<InventoryStack> Items
        {
            get
            {
                EnsureInitialized();
                return items;
            }
        }
        public event Action InventoryChanged;

        public float EquippedAttackBonus
        {
            get
            {
                float total = 0f;
                foreach (ItemDefinition item in equippedItems.Values)
                    if (item != null)
                        total += item.AttackBonus;
                return total;
            }
        }

        public float EquippedDefenseBonus
        {
            get
            {
                float total = 0f;
                foreach (ItemDefinition item in equippedItems.Values)
                    if (item != null)
                        total += item.DefenseBonus;
                return total;
            }
        }

        private void Awake()
        {
            playerEntity = GetComponent<PlayerEntity>();
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;
            if (playerEntity == null)
                playerEntity = GetComponent<PlayerEntity>();
            items.Clear();
            var nextSlots = new Dictionary<ItemCategory, int>();
            foreach (InventoryStack stack in initialItems)
            {
                if (stack?.Item != null && stack.Quantity > 0)
                {
                    int slot = nextSlots.TryGetValue(stack.Item.Category, out int next) ? next : 0;
                    items.Add(new InventoryStack(stack.Item, stack.Quantity, slot));
                    nextSlots[stack.Item.Category] = slot + 1;
                }
            }
        }

        public bool CanAdd(ItemDefinition item, int amount = 1)
        {
            EnsureInitialized();
            if (item == null || amount <= 0)
                return false;

            InventoryStack existing = FindStack(item);
            if (existing != null)
                return existing.Quantity + amount <= item.MaximumStack;

            return FindFirstEmptySlot(item.Category) >= 0;
        }

        public bool AddItem(ItemDefinition item, int amount = 1)
        {
            if (!CanAdd(item, amount))
                return false;

            InventoryStack existing = FindStack(item);
            if (existing != null)
                existing.SetQuantity(existing.Quantity + amount);
            else
                items.Add(new InventoryStack(item, amount, FindFirstEmptySlot(item.Category)));

            InventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveItem(ItemDefinition item, int amount = 1)
        {
            EnsureInitialized();
            InventoryStack stack = FindStack(item);
            if (stack == null || amount <= 0 || stack.Quantity < amount)
                return false;

            stack.SetQuantity(stack.Quantity - amount);
            if (stack.Quantity <= 0)
            {
                if (IsEquipped(item))
                    equippedItems.Remove(item.EquipmentSlot);
                items.Remove(stack);
            }

            InventoryChanged?.Invoke();
            return true;
        }

        public bool MoveItem(ItemDefinition item, int targetSlotIndex)
        {
            EnsureInitialized();
            if (item == null || targetSlotIndex < 0 || targetSlotIndex >= SlotsPerCategory)
                return false;

            InventoryStack source = FindStack(item);
            if (source == null || source.SlotIndex == targetSlotIndex)
                return false;

            InventoryStack target = items.Find(stack =>
                stack.Item.Category == item.Category && stack.SlotIndex == targetSlotIndex);
            int previousIndex = source.SlotIndex;
            source.SetSlotIndex(targetSlotIndex);
            target?.SetSlotIndex(previousIndex);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool IsEquipped(ItemDefinition item)
        {
            EnsureInitialized();
            return item != null
                && item.EquipmentSlot != EquipmentSlot.None
                && equippedItems.TryGetValue(item.EquipmentSlot, out ItemDefinition equipped)
                && equipped == item;
        }

        public bool TryUseOrEquip(ItemDefinition item)
        {
            EnsureInitialized();
            if (item == null || GetQuantity(item) <= 0)
                return false;

            if (item.Category == ItemCategory.Equipment)
                return ToggleEquipment(item);
            if (item.Category == ItemCategory.Consumable)
                return UseConsumable(item);
            return false;
        }

        public int GetQuantity(ItemDefinition item)
        {
            EnsureInitialized();
            InventoryStack stack = FindStack(item);
            return stack != null ? stack.Quantity : 0;
        }

        private bool ToggleEquipment(ItemDefinition item)
        {
            if (item.EquipmentSlot == EquipmentSlot.None)
                return false;

            if (IsEquipped(item))
                equippedItems.Remove(item.EquipmentSlot);
            else
                equippedItems[item.EquipmentSlot] = item;

            InventoryChanged?.Invoke();
            return true;
        }

        private bool UseConsumable(ItemDefinition item)
        {
            bool used;
            switch (item.ConsumableEffect)
            {
                case ConsumableEffect.RestoreHealth:
                    used = playerEntity.RestoreHealth(item.UseAmount);
                    break;
                case ConsumableEffect.RestoreMana:
                    used = playerEntity.RestoreMana(item.UseAmount);
                    break;
                default:
                    return false;
            }

            if (!used)
                return false;

            return RemoveItem(item);
        }

        private InventoryStack FindStack(ItemDefinition item)
        {
            return items.Find(stack => stack.Item == item);
        }

        private int FindFirstEmptySlot(ItemCategory category)
        {
            for (int slot = 0; slot < SlotsPerCategory; slot++)
            {
                bool occupied = items.Exists(stack =>
                    stack.Item.Category == category && stack.SlotIndex == slot);
                if (!occupied)
                    return slot;
            }

            return -1;
        }
    }
}
