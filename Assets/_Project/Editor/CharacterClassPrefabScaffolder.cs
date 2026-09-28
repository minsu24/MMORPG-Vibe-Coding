using EasternFantasy.CharacterSelection;
using EasternFantasy.Player;
using EasternFantasy.Skill;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class CharacterClassPrefabScaffolder
    {
        private const string BasePlayerPrefabPath =
            "Assets/_Project/Prefabs/Player/Player.prefab";
        private const string PlayerPrefabFolder =
            "Assets/_Project/Prefabs/Player";

        [MenuItem("Eastern Fantasy/Character/Create Player Prefab For Selected Class")]
        public static void CreateForSelectedClass()
        {
            CharacterClassDefinition definition =
                Selection.activeObject as CharacterClassDefinition;
            if (definition == null)
            {
                Debug.LogError("Select a CharacterClassDefinition asset first.");
                return;
            }

            if (definition.ClassId == CharacterClassId.Dosa)
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(
                    BasePlayerPrefabPath);
                Debug.Log("Dosa already uses the base Player prefab.", definition);
                return;
            }

            string targetPath =
                $"{PlayerPrefabFolder}/{definition.ClassId}Player.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
            if (existing != null)
            {
                AssignPrefab(definition, existing);
                Selection.activeObject = existing;
                Debug.Log($"Using existing class prefab at '{targetPath}'.", existing);
                return;
            }

            if (!AssetDatabase.CopyAsset(BasePlayerPrefabPath, targetPath))
            {
                Debug.LogError($"Could not create class prefab at '{targetPath}'.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(targetPath);
            try
            {
                root.name = $"{definition.ClassId}Player";

                PlayerClassRuntime runtime = root.GetComponent<PlayerClassRuntime>();
                if (runtime == null)
                    runtime = root.AddComponent<PlayerClassRuntime>();

                SerializedObject runtimeData = new SerializedObject(runtime);
                runtimeData.FindProperty("representedClass").enumValueIndex =
                    (int)definition.ClassId;
                runtimeData.ApplyModifiedPropertiesWithoutUndo();

                // New classes must opt into their own combat implementation.
                PlayerProjectileShooter dosaAttack =
                    root.GetComponent<PlayerProjectileShooter>();
                if (dosaAttack != null)
                    dosaAttack.enabled = false;

                PlayerActiveSkillCaster dosaCaster =
                    root.GetComponent<PlayerActiveSkillCaster>();
                if (dosaCaster != null)
                    dosaCaster.enabled = false;

                PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            GameObject created = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
            AssignPrefab(definition, created);
            AssetDatabase.SaveAssets();
            Selection.activeObject = created;

            Debug.Log(
                $"Created '{definition.DisplayName}' player prefab. Add a component " +
                "implementing IPlayerBasicAttack and, for active skills, " +
                "IPlayerSkillCaster. Enable Playable on the class definition when ready.",
                created);
        }

        [MenuItem(
            "Eastern Fantasy/Character/Create Player Prefab For Selected Class",
            true)]
        private static bool CanCreateForSelectedClass()
        {
            return Selection.activeObject is CharacterClassDefinition;
        }

        private static void AssignPrefab(
            CharacterClassDefinition definition,
            GameObject prefab)
        {
            SerializedObject data = new SerializedObject(definition);
            data.FindProperty("playerPrefab").objectReferenceValue = prefab;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }
    }
}
