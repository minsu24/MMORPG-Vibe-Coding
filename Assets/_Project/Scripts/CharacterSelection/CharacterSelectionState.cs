using UnityEngine;

namespace EasternFantasy.CharacterSelection
{
    public static class CharacterSelectionState
    {
        private const string SelectedClassKey = "Character.SelectedClass";
        private const string CreatedPrefix = "Character.Created.";

        public static CharacterClassId SelectedClass =>
            (CharacterClassId)PlayerPrefs.GetInt(
                SelectedClassKey,
                (int)CharacterClassId.Dosa);

        public static bool HasCharacter(CharacterClassId classId)
        {
            return PlayerPrefs.GetInt(CreatedPrefix + classId, 0) == 1;
        }

        public static void CreateOrSelect(CharacterClassId classId)
        {
            // Each class owns exactly one slot. Selecting an existing slot simply reuses it.
            PlayerPrefs.SetInt(CreatedPrefix + classId, 1);
            PlayerPrefs.SetInt(SelectedClassKey, (int)classId);
            PlayerPrefs.Save();
        }
    }
}
