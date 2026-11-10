using System;
using System.Collections.Generic;
using System.Linq;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Inventory;
using EasternFantasy.Saving;
using UnityEngine;

namespace EasternFantasy.Dungeon
{
    [Serializable]
    public sealed class SavedDungeonRewardItem
    {
        public string item;
        public int quantity;
    }
    [Serializable]
    public sealed class PendingDungeonReward
    {
        public string runId, dungeon, title, returnScene;
        public List<SavedDungeonRewardItem> items = new List<SavedDungeonRewardItem>();
    }
    public static class DungeonRewardProgress
    {
        private static readonly Dictionary<CharacterClassId, List<PendingDungeonReward>> pending = new Dictionary<CharacterClassId, List<PendingDungeonReward>>();
        private static readonly Dictionary<CharacterClassId, HashSet<string>> queuedRuns = new Dictionary<CharacterClassId, HashSet<string>>();
        private static HashSet<string> Runs(CharacterClassId character)
        {
            if (!queuedRuns.TryGetValue(character, out var values)) queuedRuns[character] = values = new HashSet<string>();
            return values;
        }
        private static List<PendingDungeonReward> For(CharacterClassId character)
        {
            if (!pending.TryGetValue(character, out var values)) pending[character] = values = new List<PendingDungeonReward>();
            return values;
        }
        public static IReadOnlyList<PendingDungeonReward> Pending => For(CharacterSelectionState.SelectedClass);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { pending.Clear(); queuedRuns.Clear(); }
        public static void Queue(string runId, DungeonDefinition dungeon)
        {
            if (dungeon == null || string.IsNullOrEmpty(runId) || !Runs(CharacterSelectionState.SelectedClass).Add(runId)) return;
            var value = new PendingDungeonReward { runId = runId, dungeon = SaveAssetCatalog.Id(dungeon), title = dungeon.displayName, returnScene = dungeon.returnSceneName };
            foreach (var reward in dungeon.clearRewards ?? Array.Empty<DungeonReward>())
                if (reward != null && reward.item != null && reward.quantity > 0)
                    value.items.Add(new SavedDungeonRewardItem { item = SaveAssetCatalog.Id(reward.item), quantity = reward.quantity });
            if (value.items.Count > 0) For(CharacterSelectionState.SelectedClass).Add(value);
        }
        public static bool TryClaim(PendingDungeonReward value, PlayerInventory inventory)
        {
            var values = For(CharacterSelectionState.SelectedClass);
            if (value == null || inventory == null || !values.Contains(value)) return false;
            var rewards = new Dictionary<ItemDefinition, int>();
            foreach (var reward in value.items)
            {
                var item = SaveAssetCatalog.Resolve<ItemDefinition>(reward.item);
                if (item == null || reward.quantity <= 0) return false;
                rewards.TryGetValue(item, out int quantity);
                rewards[item] = quantity + reward.quantity;
            }
            // Remove before callbacks so a reentrant UI/save cannot pay this batch twice.
            values.Remove(value);
            if (inventory.TryAddItems(rewards)) return true;
            values.Insert(0, value);
            return false;
        }
        internal static void CaptureSave(SaveGameData data)
        {
            data.pendingDungeonRewards = Clone(For((CharacterClassId)data.characterClass));
            data.dungeonRewardRuns = Runs((CharacterClassId)data.characterClass).ToArray();
        }
        internal static void RestoreSave(SaveGameData data)
        {
            var character = CharacterSelectionState.SelectedClass;
            pending[character] = Clone(data.pendingDungeonRewards);
            queuedRuns[character] = new HashSet<string>(data.dungeonRewardRuns ?? Array.Empty<string>());
            foreach (var value in pending[character]) queuedRuns[character].Add(value.runId);
        }
        private static List<PendingDungeonReward> Clone(List<PendingDungeonReward> values) =>
            values == null ? new List<PendingDungeonReward>() : values.Where(value => value != null).Select(value =>
                new PendingDungeonReward { runId = value.runId, dungeon = value.dungeon, title = value.title, returnScene = value.returnScene,
                    items = (value.items ?? new List<SavedDungeonRewardItem>()).Where(item => item != null).Select(item =>
                        new SavedDungeonRewardItem { item = item.item, quantity = item.quantity }).ToList() }).ToList();
    }
}
