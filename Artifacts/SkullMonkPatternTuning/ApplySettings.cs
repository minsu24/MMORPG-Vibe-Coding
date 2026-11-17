using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var path = "Assets/_Project/Scenes/OldTemple/OldTemple - Center.unity";
var scene = SceneManager.GetSceneByPath(path);
bool opened = !scene.isLoaded;
if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
try
{
    var boss = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MON_SkullMonk>(true)).Single();
    var data = new SerializedObject(boss);
    Undo.RecordObject(boss, "Shorten giant staff warning");
    data.FindProperty("giantStaffWarningSeconds").floatValue = 2.5f;
    data.ApplyModifiedProperties();
    EditorSceneManager.MarkSceneDirty(scene);
    if (!EditorSceneManager.SaveScene(scene)) throw new System.Exception("Could not save boss scene");
    return new { warningSeconds = data.FindProperty("giantStaffWarningSeconds").floatValue };
}
finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
