using System.Collections.Generic;
using System.Linq;
using EasternFantasy.Saving;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public sealed class SaveAssetCatalogBuilder : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => Rebuild();
        [InitializeOnLoadMethod]
        private static void Initialize() => EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) Rebuild();
        };
        [MenuItem("Eastern Fantasy/Save/Rebuild Asset Catalog")]
        public static void Rebuild()
        {
            const string path = "Assets/_Project/Resources/SaveAssetCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<SaveAssetCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SaveAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }
            var entries = new List<SaveAssetCatalog.Entry>();
            foreach (string type in new[] { "ItemDefinition", "SkillDefinition", "QuestDefinition", "DialogueSequence", "DungeonDefinition" })
                foreach (string guid in AssetDatabase.FindAssets("t:" + type, new[] { "Assets/_Project" }))
                    entries.Add(new SaveAssetCatalog.Entry { id = guid, asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)) });
            catalog.entries = entries.OrderBy(e => e.id).ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }
    }
}
