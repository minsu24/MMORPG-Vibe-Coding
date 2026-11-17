using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var path = "Assets/_Project/Scenes/OldTemple/OldTemple - Center.unity";
var scene = SceneManager.GetSceneByPath(path);
bool opened = !scene.isLoaded;
if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
GameObject preview = null, cameraObject = null;
RenderTexture rt = null;
Texture2D capture = null;
var old = RenderTexture.active;
try
{
    var boss = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MON_SkullMonk>(true)).Single();
    var data = new SerializedObject(boss);
    // Save the revised artwork depth on the actual encounter.
    data.FindProperty("giantStaffGroundVisualOffset").floatValue = .4f;
    data.ApplyModifiedProperties();
    EditorSceneManager.MarkSceneDirty(scene);
    if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save boss scene");
    var floor = (Collider2D)data.FindProperty("waveFloor").objectReferenceValue;
    var arena = (Collider2D)data.FindProperty("waveArenaBounds").objectReferenceValue;
    var fall = Enumerable.Range(0, 4).Select(i => (Sprite)data.FindProperty("giantStaffFallFrames").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    var impact = Enumerable.Range(0, 8).Select(i => (Sprite)data.FindProperty("giantStaffImpactFrames").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    var hd = (Sprite)data.FindProperty("giantStaffHighResolutionSprite").objectReferenceValue;
    var ground = new Vector2(arena.bounds.center.x, floor.bounds.max.y);
    var attack = SkullMonkGiantStaff.Create(boss, ground,
        data.FindProperty("giantStaffMaxWidth").floatValue,
        data.FindProperty("giantStaffWarningSeconds").floatValue,
        0, fall, impact, boss.GetComponent<SpriteRenderer>(), Vector2.zero, hd,
        data.FindProperty("giantStaffImpactHeight").floatValue,
        data.FindProperty("giantStaffGroundVisualOffset").floatValue);
    preview = attack.gameObject;
    var flags = BindingFlags.NonPublic | BindingFlags.Instance;
    typeof(SkullMonkGiantStaff).GetField("released", flags).SetValue(attack, true);
    cameraObject = new GameObject("Temporary central staff preview camera");
    SceneManager.MoveGameObjectToScene(cameraObject, scene);
    var camera = cameraObject.AddComponent<Camera>();
    camera.orthographic = true;
    camera.orthographicSize = 9f;
    camera.transform.position = new Vector3(arena.bounds.center.x, -3f, -30f);
    camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
    camera.clearFlags = CameraClearFlags.SolidColor;
    camera.backgroundColor = Color.black;
    rt = new RenderTexture(1800, 720, 24);
    capture = new Texture2D(1800, 720, TextureFormat.RGBA32, false);
    camera.targetTexture = rt;
    float[] ages = { 2f, 2.8f, 2.881f, 3.2f };
    string[] names = { "center-warning", "center-falling", "center-landing", "center-impact" };
    var rows = new System.Collections.Generic.List<object>();
    for (int i = 0; i < ages.Length; i++)
    {
        typeof(SkullMonkGiantStaff).GetField("age", flags).SetValue(attack, ages[i]);
        typeof(SkullMonkGiantStaff).GetField("impacted", flags).SetValue(attack, ages[i] >= 2.88f);
        typeof(SkullMonkGiantStaff).GetMethod("Draw", flags).Invoke(attack, null);
        camera.Render();
        RenderTexture.active = rt;
        capture.ReadPixels(new Rect(0, 0, 1800, 720), 0, 0);
        capture.Apply();
        System.IO.File.WriteAllBytes("Artifacts/GiantStaff/" + names[i] + ".png", capture.EncodeToPNG());
        var staff = (SpriteRenderer)typeof(SkullMonkGiantStaff).GetField("staff", flags).GetValue(attack);
        var crack = (SpriteRenderer)typeof(SkullMonkGiantStaff).GetField("crack", flags).GetValue(attack);
        rows.Add(new { stage = names[i], centerX = staff.bounds.center.x, bottom = staff.bounds.min.y, height = staff.bounds.size.y, crackVisible = crack.enabled });
        if (i >= 2 && (Mathf.Abs(staff.bounds.center.x - arena.bounds.center.x) > .01f || staff.bounds.min.y < -7.3f))
            throw new Exception("Staff is off-center or extends into foreground railing");
    }
    return new { mapCenter = ground.x, width = attack.DamageWidth, warningSeconds = attack.WarningSeconds, samples = rows };
}
finally
{
    RenderTexture.active = old;
    if (preview != null) UnityEngine.Object.DestroyImmediate(preview);
    if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
    if (rt != null) UnityEngine.Object.DestroyImmediate(rt);
    if (capture != null) UnityEngine.Object.DestroyImmediate(capture);
    if (opened) EditorSceneManager.CloseScene(scene, true);
}
