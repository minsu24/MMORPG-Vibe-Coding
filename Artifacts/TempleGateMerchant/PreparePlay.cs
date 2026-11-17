using System;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
var path = EasternFantasy.Editor.TempleGateMerchantSetup.ScenePath;
var scene = SceneManager.GetSceneByPath(path);
if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
SceneManager.SetActiveScene(scene);
return "Opened the saved TempleGate scene for merchant validation.";
