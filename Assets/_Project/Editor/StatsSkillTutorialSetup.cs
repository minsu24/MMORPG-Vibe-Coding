using System.Linq;
using EasternFantasy.Dialogue;
using EasternFantasy.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.EditorTools
{
    public static class StatsSkillTutorialSetup
    {
        public static void Apply()
        {
            const string path = "Assets/_Project/Scenes/MovementPrototype.unity";
            var previousScene = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try {
                var objects = scene.GetRootGameObjects();
                var goblin = objects.SelectMany(g => g.GetComponentsInChildren<MON_Goblin>(true)).Single();
                var component = objects.SelectMany(g => g.GetComponentsInChildren<StatsSkillTutorialUI>(true)).FirstOrDefault();
                if (component == null) { var root = new GameObject("StatsSkillTutorial"); SceneManager.MoveGameObjectToScene(root, scene); component = root.AddComponent<StatsSkillTutorialUI>(); }
                if (component.GetComponent<InventoryEquipmentTutorialUI>() == null) component.gameObject.AddComponent<InventoryEquipmentTutorialUI>();
                var data = new SerializedObject(component);
                data.FindProperty("tutorialGoblin").objectReferenceValue = goblin;
                data.FindProperty("defeatedDialogue").objectReferenceValue = new SerializedObject(goblin).FindProperty("defeatedDialogue").objectReferenceValue;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, path);
            }
            finally {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            }
        }
    }
}
