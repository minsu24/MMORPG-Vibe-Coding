using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using EasternFantasy.UI;

if (EditorApplication.isPlaying) throw new Exception("Run layout verification outside Play Mode.");
var prefabPath = EasternFantasy.Editor.SkullMonkBossHealthSetup.PrefabPath;
var contents = PrefabUtility.LoadPrefabContents(prefabPath);
try
{
    var ui = contents.GetComponent<SkullMonkBossHealthUI>();
    var sprite = ui.panel.GetComponent<Image>().sprite;
    ui.panel.sizeDelta = new Vector2(SkullMonkBossHealthUI.MaximumPanelWidth,
        SkullMonkBossHealthUI.MaximumPanelWidth * sprite.rect.height / sprite.rect.width);
    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
}
finally { PrefabUtility.UnloadPrefabContents(contents); }

var tutorialPath = "Assets/_Project/Scenes/TutorialMap/MovementPrototype.unity";
var bossPath = "Assets/_Project/Scenes/OldTemple/OldTemple - Center.unity";
var tutorial = SceneManager.GetSceneByPath(tutorialPath);
var bossScene = SceneManager.GetSceneByPath(bossPath);
bool openedTutorial = !tutorial.isLoaded, openedBoss = !bossScene.isLoaded;
if (openedTutorial) tutorial = EditorSceneManager.OpenScene(tutorialPath, OpenSceneMode.Additive);
if (openedBoss) bossScene = EditorSceneManager.OpenScene(bossPath, OpenSceneMode.Additive);
var preview = EditorSceneManager.NewPreviewScene();
var objects = new System.Collections.Generic.List<GameObject>();
var previousRT = RenderTexture.active;
RenderTexture rt = null;
Texture2D image = null;
try
{
    var sourceHUD = tutorial.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HP_MP_EXP_UI>(true)).Single();
    var boss = bossScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MON_SkullMonk>(true)).Single();
    boss.HP = boss.maxHP * .5f;
    var cameraObject = new GameObject("HP layout preview camera"); objects.Add(cameraObject);
    SceneManager.MoveGameObjectToScene(cameraObject, preview);
    var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 9f;
    camera.transform.position = new Vector3(18.56f, -3f, -30f);
    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
    camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(preview) | EditorSceneManager.GetSceneCullingMask(bossScene);
    var hudCanvasObject = new GameObject("Player HUD preview", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
    objects.Add(hudCanvasObject); SceneManager.MoveGameObjectToScene(hudCanvasObject, preview);
    var hudCanvas = hudCanvasObject.GetComponent<Canvas>(); hudCanvas.renderMode = RenderMode.ScreenSpaceCamera;
    hudCanvas.worldCamera = camera; hudCanvas.planeDistance = 1f; hudCanvas.sortingOrder = 99;
    var hudScaler = hudCanvasObject.GetComponent<CanvasScaler>(); hudScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    hudScaler.referenceResolution = new Vector2(1920, 1080); hudScaler.matchWidthOrHeight = 0f;
    var playerHUD = UnityEngine.Object.Instantiate(sourceHUD.gameObject, hudCanvas.transform, false);
    var hudData = new SerializedObject(playerHUD.GetComponent<HP_MP_EXP_UI>());
    ((TMPro.TMP_Text)hudData.FindProperty("textHP").objectReferenceValue).text = "100 / 100";
    ((TMPro.TMP_Text)hudData.FindProperty("textMP").objectReferenceValue).text = "100 / 100";
    if (openedTutorial) { EditorSceneManager.CloseScene(tutorial, true); openedTutorial = false; }
    var playerHP = playerHUD.transform.Find("HPBar") as RectTransform;
    var bossObject = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
    objects.Add(bossObject); SceneManager.MoveGameObjectToScene(bossObject, preview);
    var bossUI = bossObject.GetComponent<SkullMonkBossHealthUI>(); bossUI.Bind(boss);
    var bossCanvas = bossObject.GetComponent<Canvas>(); bossCanvas.renderMode = RenderMode.ScreenSpaceCamera;
    bossCanvas.worldCamera = camera; bossCanvas.planeDistance = 1f;
    var bossScaler = bossObject.GetComponent<CanvasScaler>();
    Rect ScreenBounds(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        var lower = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        var upper = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        return Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y);
    }
    var rows = new System.Collections.Generic.List<object>();
    foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080), new Vector2Int(2560,1440), new Vector2Int(1920,1200), new Vector2Int(1024,768), new Vector2Int(2560,1080) })
    {
        rt = new RenderTexture(size.x, size.y, 24); camera.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        hudScaler.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
        bossScaler.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
        Canvas.ForceUpdateCanvases(); bossUI.Refresh(true); Canvas.ForceUpdateCanvases();
        var hpBounds = ScreenBounds(playerHP); var bossBounds = ScreenBounds(bossUI.panel);
        if (hpBounds.Overlaps(bossBounds)) throw new Exception("HP bars overlap at " + size + ": " + hpBounds + " vs " + bossBounds);
        camera.Render(); RenderTexture.active = rt;
        image = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
        System.IO.File.WriteAllBytes("Artifacts/SkullMonkHealth/layout-" + size.x + "x" + size.y + ".png", image.EncodeToPNG());
        rows.Add(new { resolution = size.ToString(), playerHP = hpBounds.ToString(), bossHP = bossBounds.ToString(), overlap = false });
        UnityEngine.Object.DestroyImmediate(image); image = null;
        RenderTexture.active = previousRT; camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(rt); rt = null;
    }
    var report = new { passed = true, layouts = rows };
    System.IO.File.WriteAllText("Artifacts/SkullMonkHealth/layout-validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
    return report;
}
finally
{
    RenderTexture.active = previousRT;
    foreach (var obj in objects) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
    if (rt != null) UnityEngine.Object.DestroyImmediate(rt);
    if (image != null) UnityEngine.Object.DestroyImmediate(image);
    EditorSceneManager.ClosePreviewScene(preview);
    if (openedBoss) EditorSceneManager.CloseScene(bossScene, true);
    if (openedTutorial) EditorSceneManager.CloseScene(tutorial, true);
}
