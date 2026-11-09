using System;
using System.Collections.Generic;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Saving;
using UnityEngine;

namespace EasternFantasy.UI
{
    public static class SoulstoneProgress
    {
        private sealed class State { public bool activated, tutorial, villageAwakened; }
        private static readonly Dictionary<CharacterClassId, State> states = new Dictionary<CharacterClassId, State>();
        private static State Current
        {
            get
            {
                var id = CharacterSelectionState.SelectedClass;
                if (!states.TryGetValue(id, out var state)) states[id] = state = new State();
                return state;
            }
        }
        public static bool IsActivated => Current.activated;
        public static bool TutorialCompleted => Current.tutorial;
        public static bool VillageAwakened => Current.villageAwakened;
        public static event Action<bool> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { states.Clear(); Changed = null; }

        public static void Activate()
        {
            if (Current.activated) return;
            Current.activated = true; Changed?.Invoke(true); SaveGameService.SaveNow(out _);
        }
        public static void FinishVillageAwakening()
        {
            Current.villageAwakened = true; SaveGameService.SaveNow(out _);
        }
        public static void CompleteTutorial()
        {
            Current.tutorial = true; SaveGameService.SaveNow(out _);
        }
        internal static void CaptureSave(SaveGameData data)
        {
            data.soulstoneActivated = IsActivated;
            data.soulstoneTutorial = TutorialCompleted;
            data.soulstoneVillageAwakened = VillageAwakened;
        }
        internal static void RestoreSave(SaveGameData data)
        {
            SoulstoneTutorialUI.Instance?.Cancel();
            SoulstoneWindowUI.Instance?.Close();
            states[CharacterSelectionState.SelectedClass] = new State {
                activated = data.soulstoneActivated || data.soulstoneTutorial,
                tutorial = data.soulstoneTutorial, villageAwakened = data.soulstoneVillageAwakened
            };
            Changed?.Invoke(false);
        }
    }
}
