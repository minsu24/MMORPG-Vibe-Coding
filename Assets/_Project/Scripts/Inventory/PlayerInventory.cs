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
        public const int ConsumableSlotCount = 4;
        [SerializeField] private InventoryStack[] initialItems = Array.Empty<InventoryStack>();
        [SerializeField] private ItemDefinition[] consumableSlots = new ItemDefinition[ConsumableSlotCount];

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
        public IReadOnlyList<ItemDefinition> ConsumableSlots => consumableSlots;
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
            if (consumableSlots == null || consumableSlots.Length != ConsumableSlotCount)
            {
                ItemDefinition[] previous = consumableSlots;
                consumableSlots = new ItemDefinition[ConsumableSlotCount];
                if (previous != null)
                    Array.Copy(previous, consumableSlots, Mathf.Min(previous.Length, ConsumableSlotCount));
            }
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

        public ItemDefinition GetEquipped(EquipmentSlot slot)
        {
            EnsureInitialized();
            return equippedItems.TryGetValue(slot, out ItemDefinition equipped) ? equipped : null;
        }

        public bool TryEquip(ItemDefinition item)
        {
            EnsureInitialized();
            if (item == null || item.Category != ItemCategory.Equipment
                || item.EquipmentSlot == EquipmentSlot.None || GetQuantity(item) <= 0)
                return false;

            if (IsEquipped(item))
                return true;

            equippedItems[item.EquipmentSlot] = item;
            InventoryChanged?.Invoke();
            return true;
        }

        public bool TryUnequip(EquipmentSlot slot)
        {
            EnsureInitialized();
            if (!equippedItems.Remove(slot))
                return false;

            InventoryChanged?.Invoke();
            return true;
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

        public bool AssignConsumableSlot(int index, ItemDefinition item)
        {
            EnsureInitialized();
            if (index < 0 || index >= ConsumableSlotCount || item == null
                || item.Category != ItemCategory.Consumable || GetQuantity(item) <= 0)
                return false;

            for (int i = 0; i < consumableSlots.Length; i++)
                if (consumableSlots[i] == item)
                    consumableSlots[i] = null;
            consumableSlots[index] = item;
            InventoryChanged?.Invoke();
            return true;
        }

        public void ClearConsumableSlot(int index)
        {
            EnsureInitialized();
            if (index < 0 || index >= ConsumableSlotCount || consumableSlots[index] == null)
                return;
            consumableSlots[index] = null;
            InventoryChanged?.Invoke();
        }

        public bool TryUseConsumableSlot(int index)
        {
            EnsureInitialized();
            return index >= 0 && index < ConsumableSlotCount
                && consumableSlots[index] != null
                && TryUseOrEquip(consumableSlots[index]);
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
                return TryUnequip(item.EquipmentSlot);
            return TryEquip(item);
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
