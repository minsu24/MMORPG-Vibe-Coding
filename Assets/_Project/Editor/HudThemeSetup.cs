using System.Linq;
using EasternFantasy.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class HudThemeSetup
    {
        private const string ArtFolder = "Assets/_Project/Art/UI/HudTheme";
        [MenuItem("Eastern Fantasy/UI/Apply HUD Theme")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Apply the HUD theme outside Play mode.");
            var sheet = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/UI/상점 칸 UI.png").OfType<Sprite>().ToArray();
            var slot = sheet.First(s => s.name.EndsWith("_0"));
            var heading = sheet.First(s => s.name.EndsWith("_3"));
            if (!AssetDatabase.IsValidFolder(ArtFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art/UI", "HudTheme");
            string panelPath = ArtFolder + "/InkGoldPanel.asset";
            var panel = AssetDatabase.LoadAssetAtPath<Sprite>(panelPath);
            if (panel == null)
            {
                panel = Sprite.Create(slot.texture, slot.rect, new Vector2(.5f, .5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(78, 88, 78, 60));
                panel.name = "InkGoldPanel";
                AssetDatabase.CreateAsset(panel, panelPath);
            }
            foreach (string path in new[] { "Assets/_Project/Prefabs/UI/QuickSlotBar.prefab", "Assets/_Project/Prefabs/UI/Minimap.prefab" })
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var theme = root.GetComponent<HudThemeUI>();
                    if (theme == null) theme = root.AddComponent<HudThemeUI>();
                    theme.Configure(panel, slot, heading); theme.Apply();
                    var minimap = root.GetComponent<MinimapUI>();
                    if (minimap != null)
                    {
                        var sceneMap = root.GetComponent<MinimapSceneMap>();
                        if (sceneMap == null) sceneMap = root.AddComponent<MinimapSceneMap>();
                        var sceneryData = new SerializedObject(sceneMap);
                        sceneryData.FindProperty("inkShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Project/Effect/MinimapInk.shader");
                        sceneryData.ApplyModifiedPropertiesWithoutUndo();
                        var data = new SerializedObject(minimap);
                        data.FindProperty("terrainColor").colorValue = new Color(.26f, .40f, .36f, 1f);
                        data.ApplyModifiedPropertiesWithoutUndo();
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            // Some gameplay scenes own an independent quick bar instead of a prefab instance.
            foreach (string path in new[] { "Assets/_Project/Scenes/MovementPrototype.unity", "Assets/_Project/Scenes/PrologueVilage.unity" })
            {
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (var root in scene.GetRootGameObjects())
                    foreach (var bar in root.GetComponentsInChildren<QuickSlotBarUI>(true))
                    {
                        var theme = bar.GetComponent<HudThemeUI>();
                        if (theme == null) theme = Undo.AddComponent<HudThemeUI>(bar.gameObject);
                        theme.Configure(panel, slot, heading);
                        EditorUtility.SetDirty(theme);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(theme);
                        EditorSceneManager.MarkSceneDirty(scene);
                    }
                    if (scene.isDirty) EditorSceneManager.SaveScene(scene);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Applied ink and antique gold HUD styling to skill slots, consumables and minimap.");
        }
    }
}
