using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GiantAttackSetup
{
    private const string Root = "Assets/_Project/";

    [MenuItem("Tools/Giant/Configure Attacks In Current Scene")]
    public static void Configure()
    {
        foreach (MON_Giant giant in Object.FindObjectsByType<MON_Giant>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(giant, "Configure Giant Attacks");
            SerializedObject serialized = new SerializedObject(giant);
            SetSprites(serialized.FindProperty("dustFrames"), Root + "Art/Giant/6단계 먼지 폭발 스프라이트 시트.png");
            SetSprites(serialized.FindProperty("rockFrames"), Root + "Art/Giant/투명 배경 바위 투사체 6프레임 스프라이트 시트.png");
            GameObject boundary = GameObject.Find("CameraCollider");
            if (boundary != null) serialized.FindProperty("arenaBounds").objectReferenceValue = boundary.GetComponent<Collider2D>();
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(giant);
            EditorSceneManager.MarkSceneDirty(giant.gameObject.scene);
        }
        ConfigureClip("GiantSmash", "GiantSmashImpact", 1f, false);
        ConfigureClip("GiantThrow", "GiantThrowRelease", 1.5f, false);
        ConfigureClip("GiantSprint", null, 0f, true);
        AssetDatabase.SaveAssets();
    }

    private static void SetSprites(SerializedProperty property, string path)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
        property.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }

    private static void ConfigureClip(string name, string eventName, float eventTime, bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "Animations/Giant/" + name + ".anim");
        if (clip == null) throw new System.InvalidOperationException("Missing giant animation: " + name);
        Undo.RecordObject(clip, "Configure Giant Animation Events");
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        if (eventName != null)
        {
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip).Where(e => e.functionName != eventName)
                .Concat(new[] { new AnimationEvent { functionName = eventName, time = eventTime } }).OrderBy(e => e.time).ToArray();
            AnimationUtility.SetAnimationEvents(clip, events);
        }
        EditorUtility.SetDirty(clip);
    }
}
