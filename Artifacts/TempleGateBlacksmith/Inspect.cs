using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var path = "Assets/_Project/Scenes/OldTemple/TempleGate.unity";
var scene = SceneManager.GetSceneByPath(path);
bool opened = !scene.isLoaded;
if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
GameObject cameraObject = null;
RenderTexture rt = null;
Texture2D texture = null;
var previous = RenderTexture.active;
try
{
    var transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
    var floor = transforms.First(t => t.name == "Floor").GetComponent<Collider2D>();
    cameraObject = new GameObject("Blacksmith placement inspection");
    SceneManager.MoveGameObjectToScene(cameraObject, scene);
    var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 13f;
    camera.transform.position = new Vector3(floor.bounds.center.x, -8f, -40f);
    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
    rt = new RenderTexture(1800,720,24); camera.targetTexture = rt;
    texture = new Texture2D(1800,720,TextureFormat.RGBA32,false);
    camera.Render(); RenderTexture.active = rt;
    texture.ReadPixels(new Rect(0,0,1800,720),0,0); texture.Apply();
    System.IO.File.WriteAllBytes("Artifacts/TempleGateBlacksmith/before.png",texture.EncodeToPNG());
    return new {
        colliders = transforms.Where(t => t.name == "Floor" || t.name.StartsWith("Wall") || t.name.Contains("Portal") || t.name == "Traveler").Select(t => new {t.name, position=t.position.ToString(), bounds=t.GetComponent<Collider2D>()?.bounds.ToString()}).ToArray(),
        roots = scene.GetRootGameObjects().Select(r => r.name).ToArray(),
        npcs = transforms.Where(t=>t.GetComponent<EasternFantasy.Shop.Shopkeeper>()!=null || t.GetComponent<EasternFantasy.EquipmentUpgrade.EquipmentUpgradeBlacksmith>()!=null).Select(t=>new{t.name,position=t.position.ToString()}).ToArray()
    };
}
finally
{
    RenderTexture.active=previous;
    if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);
    if(rt!=null)UnityEngine.Object.DestroyImmediate(rt);
    if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);
    if(opened)EditorSceneManager.CloseScene(scene,true);
}
