using System;
using System.Collections.Generic;
using System.Linq;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Saving;
using UnityEngine;

namespace EasternFantasy.Inventory
{
    public static class FirstDropProgress
    {
        private static readonly Dictionary<CharacterClassId, HashSet<string>> acquired = new Dictionary<CharacterClassId, HashSet<string>>();
        private static HashSet<string> For(CharacterClassId character)
        {
            if (!acquired.TryGetValue(character, out var entries)) acquired[character] = entries = new HashSet<string>();
            return entries;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => acquired.Clear();
        public static bool IsAcquired(ItemDefinition item) => item != null && For(CharacterSelectionState.SelectedClass).Contains(item.ItemId);
        public static bool CanDrop(ItemDefinition item)
        {
            if (item == null) return false;
            if (!item.FirstAcquisitionOnly) return true;
            if (IsAcquired(item)) return false;
            // Keep only one uncollected copy, including drops cached in an inactive scene.
            return !UnityEngine.Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Any(pickup => pickup.Item == item && !pickup.IsCollected);
        }
        public static void RegisterAcquired(ItemDefinition item)
        {
            if (item != null && item.FirstAcquisitionOnly) For(CharacterSelectionState.SelectedClass).Add(item.ItemId);
        }
        internal static void CaptureSave(SaveGameData data) => data.firstDropItems = For((CharacterClassId)data.characterClass).ToArray();
        internal static void RestoreSave(SaveGameData data)
        {
            var entries = For(CharacterSelectionState.SelectedClass);
            entries.Clear(); entries.UnionWith(data.firstDropItems ?? Array.Empty<string>());
            // Existing saves already holding a unique drop must not receive another one.
            foreach (var saved in data.items ?? new List<SavedItem>())
            {
                var item = SaveAssetCatalog.Resolve<ItemDefinition>(saved.id);
                if (saved.quantity > 0 && item != null && item.FirstAcquisitionOnly) entries.Add(item.ItemId);
            }
        }
    }
}
