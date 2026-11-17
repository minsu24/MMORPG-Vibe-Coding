using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var scenePath = "Assets/_Project/Scenes/OldTemple/OldTemple - Center.unity";
var scene = SceneManager.GetSceneByPath(scenePath);
bool opened = !scene.isLoaded;
if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
var temporary = new System.Collections.Generic.List<GameObject>();
RenderTexture rt = null;
Texture2D texture = null;
var previousRT = RenderTexture.active;
try
{
    var boss = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MON_SkullMonk>(true)).Single();
    var data = new SerializedObject(boss);
    var explosions = Enumerable.Range(0, 8).Select(i => (Sprite)data.FindProperty("prayerExplosionFrames").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    var dissolves = Enumerable.Range(0, 5).Select(i => (Sprite)data.FindProperty("prayerDissolveFrames").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    var floor = (Collider2D)data.FindProperty("waveFloor").objectReferenceValue;
    float ground = floor.bounds.max.y;
    var flags = BindingFlags.NonPublic | BindingFlags.Instance;
    float[] ages = { -.2f, .12f, .48f, .86f };
    var samples = new System.Collections.Generic.List<object>();
    for (int i = 0; i < ages.Length; i++)
    {
        var blast = SkullMonkPrayerBlast.Create(boss, new Vector2(9f + i * 6f, ground), 3f, 18f,
            .95f, 0f, 0, explosions, dissolves, boss.GetComponent<SpriteRenderer>());
        temporary.Add(blast.gameObject);
        typeof(SkullMonkPrayerBlast).GetField("age", flags).SetValue(blast, .95f + ages[i]);
        typeof(SkullMonkPrayerBlast).GetMethod("Draw", flags).Invoke(blast, null);
        var visual = blast.GetComponentInChildren<SpriteRenderer>();
        samples.Add(new { age = ages[i], ground, visualY = visual.transform.position.y, scaleY = visual.transform.localScale.y });
    }
    var cameraObject = new GameObject("Temporary prayer ground preview");
    temporary.Add(cameraObject);
    SceneManager.MoveGameObjectToScene(cameraObject, scene);
    var camera = cameraObject.AddComponent<Camera>();
    camera.orthographic = true;
    camera.orthographicSize = 11.5f;
    camera.transform.position = new Vector3(18.56f, 2.4f, -30f);
    camera.clearFlags = CameraClearFlags.SolidColor;
    camera.backgroundColor = Color.black;
    camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
    rt = new RenderTexture(1800, 1080, 24);
    texture = new Texture2D(1800, 1080, TextureFormat.RGBA32, false);
    camera.targetTexture = rt;
    camera.Render();
    RenderTexture.active = rt;
    texture.ReadPixels(new Rect(0, 0, 1800, 1080), 0, 0);
    texture.Apply();
    string label = "high-resolution";
    System.IO.Directory.CreateDirectory("Artifacts/SkullMonkPatternTuning");
    System.IO.File.WriteAllBytes("Artifacts/SkullMonkPatternTuning/prayer-" + label + ".png", texture.EncodeToPNG());
    return new { label, samples };
}
finally
{
    RenderTexture.active = previousRT;
    foreach (var obj in temporary) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
    if (rt != null) UnityEngine.Object.DestroyImmediate(rt);
    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
    if (opened) EditorSceneManager.CloseScene(scene, true);
}
