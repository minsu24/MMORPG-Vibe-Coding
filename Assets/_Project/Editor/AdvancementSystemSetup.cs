using EasternFantasy.Advancement;
using EasternFantasy.Dialogue;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class AdvancementSystemSetup
    {
        private const string DataFolder = "Assets/_Project/Data/Advancement/Dosa";
        private const string DialoguePath = DataFolder + "/DosaFirstAdvancementDialogue.asset";
        private const string AdvancementPath = DataFolder + "/DosaFirstAdvancement.asset";
        private const string PlayerCharacterPath =
            "Assets/_Project/Data/Tutorial_DIalogue/DialoguePlayer.asset";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

        [MenuItem("Eastern Fantasy/Setup Dosa First Advancement")]
        public static void Apply()
        {
            EnsureFolder(DataFolder);
            DialogueSequence dialogue = CreateDialogue();
            AdvancementDefinition advancement = CreateAdvancement(dialogue);
            ApplyToPlayerPrefab(advancement);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dosa level 10 advancement is ready.");
        }

        private static DialogueSequence CreateDialogue()
        {
            DialogueSequence dialogue =
                AssetDatabase.LoadAssetAtPath<DialogueSequence>(DialoguePath);
            if (dialogue != null)
                return dialogue;

            dialogue = ScriptableObject.CreateInstance<DialogueSequence>();
            SerializedObject data = new SerializedObject(dialogue);
            data.FindProperty("playerCharacter").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DialogueCharacter>(PlayerCharacterPath);
            data.FindProperty("partnerCharacter").objectReferenceValue = null;

            SerializedProperty lines = data.FindProperty("lines");
            lines.arraySize = 3;
            SetLine(lines.GetArrayElementAtIndex(0), "이 기운은… 전과 다르다.");
            SetLine(lines.GetArrayElementAtIndex(1), "부적에 깃든 흐름이 전보다 선명하게 느껴진다.");
            SetLine(lines.GetArrayElementAtIndex(2), "이제 한 단계 더 나아갈 때다.");
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(dialogue, DialoguePath);
            return dialogue;
        }

        private static void SetLine(SerializedProperty line, string text)
        {
            line.FindPropertyRelative("speaker").enumValueIndex =
                (int)DialogueSpeakerSide.Player;
            line.FindPropertyRelative("text").stringValue = text;
        }

        private static AdvancementDefinition CreateAdvancement(DialogueSequence dialogue)
        {
            AdvancementDefinition advancement =
                AssetDatabase.LoadAssetAtPath<AdvancementDefinition>(AdvancementPath);
            if (advancement != null)
                return advancement;

            advancement = ScriptableObject.CreateInstance<AdvancementDefinition>();
            SerializedObject data = new SerializedObject(advancement);
            data.FindProperty("displayName").stringValue = "도사 1차 전직";
            data.FindProperty("requiredLevel").intValue = 10;
            data.FindProperty("resultingRank").enumValueIndex = (int)AdvancementRank.First;
            data.FindProperty("unlockedSkillTier").enumValueIndex = 1;
            data.FindProperty("dialogue").objectReferenceValue = dialogue;
            data.FindProperty("attackBonus").floatValue = 5f;
            data.FindProperty("defenseBonus").floatValue = 3f;
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(advancement, AdvancementPath);
            return advancement;
        }

        private static void ApplyToPlayerPrefab(AdvancementDefinition advancement)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                PlayerAdvancementSystem system = root.GetComponent<PlayerAdvancementSystem>();
                if (system == null)
                    system = root.AddComponent<PlayerAdvancementSystem>();

                SerializedObject data = new SerializedObject(system);
                data.FindProperty("automaticAdvancement").objectReferenceValue = advancement;
                data.FindProperty("startAutomatically").boolValue = true;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }
    }
}
