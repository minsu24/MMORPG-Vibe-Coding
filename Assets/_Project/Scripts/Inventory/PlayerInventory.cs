using System;
using System.Collections.Generic;
using EasternFantasy.Player;
using EasternFantasy.EquipmentUpgrade;
using EasternFantasy.CharacterSelection;
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
        public CharacterClassId CurrentClass
        {
            get
            {
                var runtime = GetComponent<PlayerClassRuntime>();
                return runtime != null ? runtime.RepresentedClass : CharacterClassId.Dosa;
            }
        }
        public bool CanEquip(ItemDefinition item) => item != null &&
            item.IsEquippable &&
            item.CanBeEquippedBy(CurrentClass);

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
        public static event Action<PlayerInventory, ItemDefinition> ItemAcquired;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAcquisitionEvents() { ItemAcquired = null; }

        public float EquippedJumpHeightBonus
        {
            get
            {
                EnsureInitialized();
                float total = 0f;
                foreach (ItemDefinition item in equippedItems.Values)
                    if (item != null && item.IsAccessory && item.SpecialAbility == AccessoryAbility.JumpHeight)
                        total += item.SpecialAbilityValue;
                return total;
            }
        }

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

        public int GetEquippedSetCount(EquipmentSetDefinition set)
        {
            EnsureInitialized();
            if (set == null) return 0;
            int count = 0;
            foreach (var pair in equippedItems)
                if (pair.Value != null && pair.Value.EquipmentSet == set && set.IncludesSlot(pair.Key))
                    count++;
            return count;
        }

        // Sum flat bonuses and percentage points across all active thresholds and sets.
        public float ApplySetBonuses(EquipmentSetStat stat, float baseValue)
        {
            EnsureInitialized();
            float flat = 0f, percent = 0f;
            var visited = new HashSet<EquipmentSetDefinition>();
            foreach (ItemDefinition item in equippedItems.Values)
            {
                EquipmentSetDefinition set = item != null ? item.EquipmentSet : null;
                if (set == null || !visited.Add(set)) continue;
                int count = GetEquippedSetCount(set);
                foreach (EquipmentSetBonus bonus in set.Bonuses)
                {
                    if (bonus == null || count < bonus.RequiredPieces) continue;
                    foreach (EquipmentSetEffect effect in bonus.Effects)
                    {
                        if (effect == null || effect.Stat != stat) continue;
                        if (effect.Modifier == EquipmentSetModifier.Percent) percent += effect.Value;
                        else flat += effect.Value;
                    }
                }
            }
            return Mathf.Max(0f, (baseValue + flat) * (1f + percent / 100f));
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
            ItemAcquired?.Invoke(this, item);
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

        // Replace one owned item; never mutate the shared item definition.
        public bool TryChangeGrade(ItemDefinition item, ItemGrade targetGrade) =>
            ChangeGrade(item, targetGrade, true);

        private bool ChangeGrade(ItemDefinition item, ItemGrade targetGrade, bool notify)
        {
            EnsureInitialized();
            if (item == null) return false;
            InventoryStack source = FindStack(item);
            ItemDefinition replacement = item.GetGradeVariant(targetGrade);
            if (source == null || replacement == null || replacement == item) return false;
            InventoryStack target = FindStack(replacement);
            if (target != null && target.Quantity >= replacement.MaximumStack) return false;
            if (target == null && source.Quantity > 1 && FindFirstEmptySlot(item.Category) < 0) return false;
            int index = source.SlotIndex;
            bool equipped = IsEquipped(item);
            if (equipped && !CanEquip(replacement)) return false;
            source.SetQuantity(source.Quantity - 1);
            if (source.Quantity == 0) items.Remove(source);
            if (target != null) target.SetQuantity(target.Quantity + 1);
            else items.Add(new InventoryStack(replacement, 1,
                source.Quantity == 0 ? index : FindFirstEmptySlot(item.Category)));
            if (equipped) equippedItems[item.EquipmentSlot] = replacement;
            if (notify) InventoryChanged?.Invoke();
            return true;
        }

        public bool CanUpgradeEquipment(ItemDefinition item, EquipmentUpgradeRecipe recipe,
            PlayerCurrency currency, out string error)
        {
            EnsureInitialized();
            error = "승급할 장비를 선택해 주세요.";
            if (item == null || GetQuantity(item) == 0) return false;
            error = item.Grade == ItemGrade.Mythic ? "최고 등급 장비입니다." : "이 장비의 승급 조건이 없습니다.";
            if (recipe == null || !recipe.Matches(item)) return false;
            if (!recipe.TryGetRequirements(out var requirements, out error)) return false;
            ItemDefinition replacement = item.GetGradeVariant(recipe.TargetGrade);
            error = "다음 등급의 아이템 데이터가 없습니다.";
            if (replacement == null || replacement == item) return false;
            error = "다음 등급 장비를 현재 직업이 장착할 수 없습니다.";
            if (IsEquipped(item) && !CanEquip(replacement)) return false;
            InventoryStack source = FindStack(item), target = FindStack(replacement);
            error = "인벤토리 공간이나 다음 등급의 최대 수량을 확인해 주세요.";
            if (target != null && target.Quantity >= replacement.MaximumStack) return false;
            if (target == null && source.Quantity > 1 && FindFirstEmptySlot(item.Category) < 0) return false;
            error = "엽전이 부족합니다.";
            if (currency == null || !currency.CanAfford(recipe.CostYeopjeon)) return false;
            foreach (var pair in requirements)
            {
                if (GetMaterialQuantity(pair.Key) >= pair.Value) continue;
                error = pair.Key.DisplayName + " 재료가 부족합니다.";
                return false;
            }
            error = null;
            return true;
        }

        public bool TryUpgradeEquipment(ItemDefinition item, EquipmentUpgradeRecipe recipe,
            PlayerCurrency currency, out ItemDefinition result, out string error)
        {
            result = null;
            if (!CanUpgradeEquipment(item, recipe, currency, out error)) return false;
            recipe.TryGetRequirements(out var requirements, out error);
            // No callbacks occur between validation and commit. Observers see the complete transaction.
            if (!ChangeGrade(item, recipe.TargetGrade, false)) return false;
            currency.TrySpendSilently(recipe.CostYeopjeon);
            foreach (var pair in requirements)
            {
                ConsumeMaterial(pair.Key, pair.Value);
            }
            result = item.GetGradeVariant(recipe.TargetGrade);
            error = null;
            InventoryChanged?.Invoke();
            currency.NotifyChanged();
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
            if (!CanEquip(item)) return false;
            if (item == null || !item.IsEquippable
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

            if (item.IsEquippable)
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

        public int GetMaterialQuantity(ItemDefinition material)
        {
            EnsureInitialized();
            int total = 0;
            if (material == null) return total;
            foreach (var stack in items)
                if (material.MatchesMaterial(stack.Item) || stack.Item == material) total += stack.Quantity;
            return total;
        }

        // Consume tutorial materials first so ordinary, sellable materials are preserved.
        private void ConsumeMaterial(ItemDefinition material, int amount)
        {
            for (int pass = 0; pass < 2 && amount > 0; pass++)
            {
                for (int i = items.Count - 1; i >= 0 && amount > 0; i--)
                {
                    var stack = items[i];
                    if (stack.Item.CanSell != (pass == 1)
                        || !(material.MatchesMaterial(stack.Item) || stack.Item == material)) continue;
                    int consumed = Mathf.Min(amount, stack.Quantity);
                    stack.SetQuantity(stack.Quantity - consumed);
                    amount -= consumed;
                    if (stack.Quantity == 0) items.RemoveAt(i);
                }
            }
        }

        public bool RemoveMaterial(ItemDefinition material, int amount = 1)
        {
            if (material == null || amount <= 0 || GetMaterialQuantity(material) < amount) return false;
            ConsumeMaterial(material, amount);
            InventoryChanged?.Invoke();
            return true;
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
