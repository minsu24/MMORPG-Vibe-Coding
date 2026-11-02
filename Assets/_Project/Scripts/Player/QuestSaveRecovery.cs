using System.Collections.Generic;
using System.Linq;
using EasternFantasy.Quest;
using UnityEngine;

namespace EasternFantasy.Saving
{
    // Recover proven history only. Visiting a map or defeating a boss alone does
    // not prove that its quest was accepted/completed, and grants no quest rewards.
    public static class QuestSaveRecovery
    {
        public static bool RecoverMissingProgress(SaveGameData data)
        {
            if (data == null) return false;
            if (data.quests == null) data.quests = new List<SavedQuest>();
            bool changed = false;
            var visited = new HashSet<QuestDefinition>();
            var toad = Resources.Load<ToadHuntDefinition>("ToadHunt");
            if (toad != null && toad.quest != null && toad.necklace != null && data.toadDefeated)
            {
                string necklaceId = SaveAssetCatalog.Id(toad.necklace);
                if (data.items != null && data.items.Any(item => item != null && item.id == necklaceId && item.quantity > 0))
                    changed |= RestoreCompleted(data, toad.quest, visited);
            }
            // Accepting a later quest requires completion of its prerequisites.
            foreach (var entry in data.quests.ToArray())
            {
                if (entry == null) continue;
                var definition = SaveAssetCatalog.Resolve<QuestDefinition>(entry.id);
                if (definition != null && definition.Type != QuestType.Repeatable)
                    changed |= RestoreCompleted(data, definition.PrerequisiteQuest, visited);
            }
            return changed;
        }

        private static bool RestoreCompleted(SaveGameData data, QuestDefinition quest, HashSet<QuestDefinition> visited)
        {
            if (quest == null || quest.Type == QuestType.Repeatable || !visited.Add(quest)) return false;
            string id = SaveAssetCatalog.Id(quest);
            if (string.IsNullOrEmpty(id)) return false;
            bool changed = false;
            var saved = data.quests.Find(entry => entry != null && entry.id == id);
            if (saved == null)
            {
                saved = new SavedQuest { id = id };
                data.quests.Add(saved); changed = true;
            }
            if (saved.status != (int)QuestStatus.Completed || saved.amount != quest.RequiredAmount)
            {
                saved.status = (int)QuestStatus.Completed;
                saved.amount = quest.RequiredAmount;
                changed = true;
            }
            return RestoreCompleted(data, quest.PrerequisiteQuest, visited) || changed;
        }
    }
}
