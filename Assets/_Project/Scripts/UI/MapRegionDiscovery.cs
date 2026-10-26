using System;
using System.Collections.Generic;
using EasternFantasy.CharacterSelection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.UI
{
    public static class MapRegionDiscovery
    {
        public const string Tutorial = "tutorial";
        public const string GiantFarm = "giant-farm";
        public static event Action Changed;
        // Session memory only. A future save system can own persistence separately.
        private static readonly HashSet<string> visitedRegions = new HashSet<string>();
        private static readonly HashSet<string> visitedScenes = new HashSet<string>();
        private static readonly HashSet<string> knownScenes = new HashSet<string>();
        private static readonly HashSet<string> presentedScenes = new HashSet<string>();
        private static string Key(string id) => CharacterSelectionState.SelectedClass + "." + id;
        public static bool IsVisited(string region) => visitedRegions.Contains(Key(region));
        public static bool IsSceneVisited(string scene) => visitedScenes.Contains(Key(scene));
        public static bool IsSceneNameKnown(string scene) => IsSceneVisited(scene) || knownScenes.Contains(Key(scene));
        public static bool HasPresentedScene(string scene) => presentedScenes.Contains(Key(scene));
        public static void MarkScenePresented(string scene)
        {
            if (IsSceneVisited(scene)) presentedScenes.Add(Key(scene));
        }

        public static void RevealQuestDestinations(EasternFantasy.Quest.QuestDefinition quest)
        {
            if (quest == null) return;
            bool changed = false;
            foreach (string scene in quest.RevealedMapScenes)
            {
                if (RegionForScene(scene) == null || IsSceneNameKnown(scene)) continue;
                knownScenes.Add(Key(scene)); changed = true;
            }
            if (!changed) return;
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            visitedRegions.Clear(); visitedScenes.Clear();
            knownScenes.Clear(); presentedScenes.Clear();
            Changed = null;
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            SceneManager.activeSceneChanged += OnSceneChanged;
            RecordSceneEntry(SceneManager.GetActiveScene().name);
        }

        private static void OnSceneChanged(Scene previous, Scene next) => RecordSceneEntry(next.name);

        private static string RegionForScene(string scene)
        {
            switch (scene)
            {
                case "MovementPrototype": case "PrologueVilage": case "Cemetery":
                case "SpiderCave": case "Marshland": case "BrokenSeal": return Tutorial;
                case "GiantFarm_01": case "GiantFarm_02": case "GiantFarm_03_Boss": return GiantFarm;
                default: return null;
            }
        }
        public static void RecordSceneEntry(string scene)
        {
            string region = RegionForScene(scene);
            if (region == null) return;
            bool changed = !IsVisited(region) || !IsSceneVisited(scene);
            if (!changed) return;
            visitedRegions.Add(Key(region));
            visitedScenes.Add(Key(scene));
            Changed?.Invoke();
        }
    }
}
