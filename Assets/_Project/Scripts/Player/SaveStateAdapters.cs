using System;
using System.Linq;
using EasternFantasy.Saving;
using EasternFantasy.Skill;
using UnityEngine;

// Restore state directly so acquisition, quest and advancement rewards never replay.
namespace EasternFantasy.Inventory
{
    public sealed partial class PlayerInventory
    {
        internal void CaptureSave(SaveGameData data)
        {
            foreach (var stack in Items) data.items.Add(new SavedItem { id = SaveAssetCatalog.Id(stack.Item), quantity = stack.Quantity, slot = stack.SlotIndex, equipped = IsEquipped(stack.Item) });
            data.consumableSlots = consumableSlots.Select(SaveAssetCatalog.Id).ToArray();
        }
        internal void RestoreSave(SaveGameData data)
        {
            initialized = true; items.Clear(); equippedItems.Clear();
            foreach (var entry in data.items)
            {
                var item = SaveAssetCatalog.Resolve<ItemDefinition>(entry.id);
                if (item == null || entry.quantity <= 0) continue;
                items.Add(new InventoryStack(item, entry.quantity, Mathf.Clamp(entry.slot, 0, SlotsPerCategory - 1)));
                if (entry.equipped && CanEquip(item)) equippedItems[item.EquipmentSlot] = item;
            }
            consumableSlots = new ItemDefinition[ConsumableSlotCount];
            for (int i = 0; i < consumableSlots.Length && i < (data.consumableSlots?.Length ?? 0); i++)
                consumableSlots[i] = SaveAssetCatalog.Resolve<ItemDefinition>(data.consumableSlots[i]);
            InventoryChanged?.Invoke();
        }
    }
    public sealed partial class PlayerCurrency
    {
        internal void RestoreSave(int amount) { initialized = true; yeopjeon = Mathf.Max(0, amount); NotifyChanged(); }
    }
}
namespace EasternFantasy.Player
{
    public sealed partial class PlayerProgression
    {
        internal void RestoreSave(SaveGameData data)
        {
            currentLevel = Mathf.Clamp(data.level, 1, PlayerLevelTable.MaximumLevel);
            currentExperience = IsMaximumLevel ? 0 : Mathf.Clamp(data.experience, 0, Mathf.Max(0, RequiredExperience - 1));
            LevelChanged?.Invoke(currentLevel);
            ExperienceChanged?.Invoke(currentExperience, RequiredExperience);
        }
    }
    public sealed partial class PlayerEntity
    {
        internal void RestoreSave(SaveGameData data)
        {
            RecalculateStats(); Revive();
            HP = Mathf.Clamp(data.health, 1f, maxHP);
            MP = Mathf.Clamp(data.mana, 0f, maxMP);
            Mental = Mathf.Clamp(data.mental, 0f, maxMental);
            StatsChanged?.Invoke();
        }
    }
    public sealed partial class MonkEnergy { internal void RestoreSave(float value) => SetEnergy(Mathf.Clamp(value, 0f, Maximum)); }
}
namespace EasternFantasy.Skill
{
    public sealed partial class PlayerSkillSystem
    {
        internal void CaptureSave(SaveGameData data)
        {
            data.skillPoints = skillPoints;
            foreach (var pair in skillLevels) data.skills.Add(new SavedSkill { id = SaveAssetCatalog.Id(pair.Key), level = pair.Value });
            data.quickSlots = quickSlots.Select(SaveAssetCatalog.Id).ToArray();
        }
        internal void RestoreSave(SaveGameData data)
        {
            skillPoints = Mathf.Max(0, data.skillPoints);
            skillLevels.Clear(); companionSkills.Clear(); companionCooldownEnds.Clear(); EnsureSkillState();
            foreach (var entry in data.skills)
            {
                var skill = SaveAssetCatalog.Resolve<SkillDefinition>(entry.id);
                if (skill != null && skillLevels.ContainsKey(skill)) skillLevels[skill] = Mathf.Clamp(entry.level, 0, skill.MaximumLevel);
            }
            quickSlots = new SkillDefinition[QuickSlotCount];
            for (int i = 0; i < quickSlots.Length && i < (data.quickSlots?.Length ?? 0); i++)
                quickSlots[i] = SaveAssetCatalog.Resolve<SkillDefinition>(data.quickSlots[i]);
            SkillsChanged?.Invoke(); QuickSlotsChanged?.Invoke();
        }
    }
}
namespace EasternFantasy.Advancement
{
    public sealed partial class PlayerAdvancementSystem
    {
        internal void CaptureSave(SaveGameData data)
        {
            data.advancementRank = (int)currentRank; data.unlockedTier = (int)highestUnlockedSkillTier;
            data.advancementAttack = attackBonus; data.advancementDefense = defenseBonus;
        }
        internal void RestoreSave(SaveGameData data)
        {
            if (automaticRoutine != null) StopCoroutine(automaticRoutine);
            automaticRoutine = null;
            if (observedDialogueManager != null) observedDialogueManager.DialogueEnded -= CompletePendingAdvancement;
            observedDialogueManager = null; pendingAdvancement = null; IsWaitingForDialogue = false;
            currentRank = (AdvancementRank)data.advancementRank; highestUnlockedSkillTier = (SkillTier)data.unlockedTier;
            attackBonus = data.advancementAttack; defenseBonus = data.advancementDefense;
            Advanced?.Invoke();
        }
        internal void ResumeAfterSaveLoad() => EvaluateAutomaticAdvancement();
    }
}
namespace EasternFantasy.Quest
{
    public sealed partial class QuestManager
    {
        internal void CaptureSave(SaveGameData data)
        {
            foreach (var quest in quests) data.quests.Add(new SavedQuest { id = SaveAssetCatalog.Id(quest.Definition), amount = quest.CurrentAmount, status = (int)quest.Status });
        }
        internal void RestoreSave(SaveGameData data)
        {
            quests.Clear(); playerProgression = null; player = null;
            foreach (var entry in data.quests)
            {
                var definition = SaveAssetCatalog.Resolve<QuestDefinition>(entry.id);
                if (definition != null) quests.Add(new QuestProgress(definition) { CurrentAmount = Mathf.Clamp(entry.amount, 0, definition.RequiredAmount), Status = (QuestStatus)Mathf.Clamp(entry.status, 0, 2) });
            }
            QuestsChanged?.Invoke();
        }
    }
    public sealed partial class ToadHuntQuestController
    {
        internal void CaptureSave(SaveGameData data)
        {
            data.toadDefeated = defeated; data.toadHasPosition = hasDeathPosition; data.toadPosition = deathPosition;
        }
        internal void RestoreSave(SaveGameData data)
        {
            StopAllCoroutines();
            if (Relic != null) Destroy(Relic.gameObject);
            Relic = null; inspectingInventory = null;
            offerPending = inspecting = claiming = explanationPending = equipmentPresentationPending = false;
            defeated = data.toadDefeated; hasDeathPosition = data.toadHasPosition; deathPosition = data.toadPosition;
            RefreshBindings(); RefreshProgress();
        }
    }
}
namespace EasternFantasy.Dialogue
{
    public sealed partial class DialogueTrigger
    {
        internal static string[] CaptureSave() => playedSequences.Where(s => s != DialogueManager.Instance?.SaveActiveSequence).Select(SaveAssetCatalog.Id).ToArray();
        internal static void RestoreSave(string[] ids)
        {
            playedSequences.Clear();
            foreach (string id in ids ?? Array.Empty<string>()) { var asset = SaveAssetCatalog.Resolve<DialogueSequence>(id); if (asset != null) playedSequences.Add(asset); }
        }
    }
    public sealed partial class DialogueManager
    {
        internal DialogueSequence SaveActiveSequence => currentSequence;
        internal static string[] CaptureSave() => rewardedSequences.Select(SaveAssetCatalog.Id).ToArray();
        internal static void RestoreSave(string[] ids)
        {
            rewardedSequences.Clear();
            foreach (string id in ids ?? Array.Empty<string>()) { var asset = SaveAssetCatalog.Resolve<DialogueSequence>(id); if (asset != null) rewardedSequences.Add(asset); }
        }
    }
}
public abstract partial class PersistentBossController
{
    internal static void CaptureSave(SaveGameData data) { data.bossDefeats = sessionDefeats.ToArray(); data.dungeonDefeats = dungeonDefeats.ToArray(); }
    internal static void RestoreSave(SaveGameData data)
    {
        sessionDefeats.Clear(); dungeonDefeats.Clear();
        sessionDefeats.UnionWith(SceneStateIdentity.NormalizeEncounterKeys(data.bossDefeats));
        dungeonDefeats.UnionWith(SceneStateIdentity.NormalizeEncounterKeys(data.dungeonDefeats));
    }
}
public partial class MON_Goblin
{
    internal static string[] CaptureSave() => defeatedEncounters.ToArray();
    internal static void RestoreSave(string[] ids)
    {
        defeatedEncounters.Clear();
        defeatedEncounters.UnionWith(SceneStateIdentity.NormalizeEncounterKeys(ids));
    }
}
public sealed partial class SceneWorldState
{
    internal static void ClearForNewCharacter()
    {
        foreach (var state in maps.Values) DestroyDrops(state);
        maps.Clear(); sceneEnemies.Clear(); trackedSceneHandle = 0;
    }
}
namespace EasternFantasy.Dungeon
{
    public sealed partial class DungeonRunController
    {
        internal void CaptureSave(SaveGameData data) { data.dungeon = SaveAssetCatalog.Id(definition); data.dungeonSeconds = RemainingSeconds; data.dungeonCleared = IsCleared; }
        internal void RestoreSave(SaveGameData data)
        {
            RemainingSeconds = Mathf.Max(.1f, data.dungeonSeconds); IsCleared = data.dungeonCleared;
            ui.SetTimer(RemainingSeconds, IsCleared, IsCleared || definition.timeLimitSeconds > 0);
        }
    }
}
