using System;
using System.Collections.Generic;
using System.Linq;
using EasternFantasy.Saving;
using EasternFantasy.CharacterSelection;
using UnityEngine;

namespace EasternFantasy.UI
{
    public sealed partial class ShopWindowUI
    {
        internal static void CaptureSave(SaveGameData data) => data.shopTutorial = tutorialCompletedClasses.Contains(CharacterSelectionState.SelectedClass);
        internal static void RestoreSave(SaveGameData data)
        {
            Instance?.Close();
            tutorialCompletedClasses.Remove(CharacterSelectionState.SelectedClass);
            if (data.shopTutorial) tutorialCompletedClasses.Add(CharacterSelectionState.SelectedClass);
        }
    }
    public sealed partial class DungeonWindowUI
    {
        internal static void CaptureSave(SaveGameData data) => data.dungeonTutorial = tutorialCompletedClasses.Contains(CharacterSelectionState.SelectedClass);
        internal static void RestoreSave(SaveGameData data)
        {
            Instance?.Close();
            tutorialCompletedClasses.Remove(CharacterSelectionState.SelectedClass);
            if (data.dungeonTutorial) tutorialCompletedClasses.Add(CharacterSelectionState.SelectedClass);
        }
    }
    public sealed partial class EquipmentUpgradeWindowUI
    {
        internal static void CaptureSave(SaveGameData data) => data.upgradeTutorial = tutorialCompletedClasses.Contains(CharacterSelectionState.SelectedClass);
        internal static void RestoreSave(SaveGameData data)
        {
            Instance?.Close();
            tutorialCompletedClasses.Remove(CharacterSelectionState.SelectedClass);
            if (data.upgradeTutorial) tutorialCompletedClasses.Add(CharacterSelectionState.SelectedClass);
        }
    }
    public sealed partial class InventoryEquipmentTutorialUI
    {
        internal static void CaptureSave(SaveGameData data) => data.inventoryTutorial = HasFinishedForCurrentClass;
        internal static void RestoreSave(SaveGameData data)
        {
            active?.Finish();
            var id = CharacterSelectionState.SelectedClass;
            shownClasses.Remove(id); completedClasses.Remove(id);
            if (data.inventoryTutorial) { shownClasses.Add(id); completedClasses.Add(id); }
        }
    }
    public sealed partial class StatsSkillTutorialUI
    {
        internal static void CaptureSave(SaveGameData data) => data.statsTutorial = HasFinishedForCurrentClass;
        internal static void RestoreSave(SaveGameData data)
        {
            active?.Finish();
            var id = CharacterSelectionState.SelectedClass;
            shownClasses.Remove(id); finishedClasses.Remove(id);
            if (data.statsTutorial) { shownClasses.Add(id); finishedClasses.Add(id); }
        }
    }
    public sealed partial class MapTutorialUI
    {
        internal static void CaptureSave(SaveGameData data) => data.mapTutorial = completedClasses.Contains(CharacterSelectionState.SelectedClass);
        internal static void RestoreSave(SaveGameData data)
        {
            active?.End();
            completedClasses.Remove(CharacterSelectionState.SelectedClass);
            if (data.mapTutorial) completedClasses.Add(CharacterSelectionState.SelectedClass);
        }
    }
    public sealed partial class PetCompanionWindowUI
    {
        [Serializable] private sealed class SavedCompanions { public List<CompanionClassProgress> entries = new List<CompanionClassProgress>(); }
        internal void CaptureSave(SaveGameData data)
        {
            var id = CharacterSelectionState.SelectedClass;
            data.companionTutorial = tutorialCompletedThisSession.Contains(id);
            data.companionGuide = guideCompletedThisSession.Contains(id);
            data.companionsJson = JsonUtility.ToJson(new SavedCompanions { entries = progressByClass.FindAll(p => p.characterClass == id) });
        }
        internal void RestoreSave(SaveGameData data)
        {
            Close(); DestroyActor();
            var id = CharacterSelectionState.SelectedClass;
            tutorialCompletedThisSession.Remove(id); guideCompletedThisSession.Remove(id);
            if (data.companionTutorial) tutorialCompletedThisSession.Add(id);
            if (data.companionGuide) guideCompletedThisSession.Add(id);
            progressByClass.RemoveAll(p => p.characterClass == id);
            if (!string.IsNullOrEmpty(data.companionsJson))
                foreach (var entry in JsonUtility.FromJson<SavedCompanions>(data.companionsJson).entries)
                    if (entry.characterClass == id && entry.progress != null) progressByClass.Add(entry);
            selectedCompanionIndex = 0; nextPlayerSearch = 0f;
            RefreshSoulCount();
        }
    }
    public static partial class MapRegionDiscovery
    {
        private static string[] CurrentEntries(HashSet<string> source) => source.Where(s => s.StartsWith(CharacterSelectionState.SelectedClass + ".", StringComparison.Ordinal)).ToArray();
        private static void ReplaceEntries(HashSet<string> target, string[] values)
        {
            string prefix = CharacterSelectionState.SelectedClass + ".";
            target.RemoveWhere(s => s.StartsWith(prefix, StringComparison.Ordinal));
            foreach (var value in values ?? Array.Empty<string>()) if (value.StartsWith(prefix, StringComparison.Ordinal)) target.Add(value);
        }
        internal static void CaptureSave(SaveGameData data)
        {
            data.visitedRegions = CurrentEntries(visitedRegions); data.visitedScenes = CurrentEntries(visitedScenes);
            data.knownScenes = CurrentEntries(knownScenes); data.presentedScenes = CurrentEntries(presentedScenes);
        }
        internal static void RestoreSave(SaveGameData data)
        {
            ReplaceEntries(visitedRegions, data.visitedRegions); ReplaceEntries(visitedScenes, data.visitedScenes);
            ReplaceEntries(knownScenes, data.knownScenes); ReplaceEntries(presentedScenes, data.presentedScenes);
            Changed?.Invoke();
        }
    }
    public static partial class MonsterCodexDiscovery
    {
        internal static string[] CaptureSave() => discoveriesThisSession.ToArray();
        internal static void RestoreSave(string[] values)
        {
            discoveriesThisSession.Clear(); discoveriesThisSession.UnionWith(values ?? Array.Empty<string>()); Changed?.Invoke();
        }
    }
}
