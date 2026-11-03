using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class GinsengDeathSetup
{
    private const string Art = "Assets/_Project/Art/Old Temple/Monster/인삼 몬스터 사망.png";
    private const string Folder = "Assets/_Project/Animation/Old Temple/Ginseng";

    [MenuItem("Eastern Fantasy/Monsters/Set Up Ginseng Death")]
    public static void Apply()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Art);
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var previous = provider.GetSpriteRects();
        int[] centers = { 163, 435, 701, 963, 1224, 1488, 1751, 2018 };
        var rects = new SpriteRect[8];
        var pairs = new SpriteNameFileIdPair[8];
        for (int i = 0; i < rects.Length; i++)
        {
            string name = "GinsengDeath_" + i.ToString("00");
            var old = previous.FirstOrDefault(r => r.name == name);
            var id = old != null ? old.spriteID : GUID.Generate();
            rects[i] = new SpriteRect { name = name, rect = new Rect(centers[i] - 132, 164, 264, 424),
                spriteID = id, alignment = SpriteAlignment.Custom, pivot = new Vector2(0.5f, 6f / 424f) };
            pairs[i] = new SpriteNameFileIdPair(name, id);
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 200;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        var textureSettings = new TextureImporterSettings(); importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(textureSettings);
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
        provider.Apply(); importer.SaveAndReimport();
        var frames = AssetDatabase.LoadAllAssetsAtPath(Art).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (frames.Length != 8) throw new InvalidOperationException("Expected eight death poses.");
        EnsureFolder(Folder);
        string path = Folder + "/GinsengDeath.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        bool create = clip == null;
        if (create) clip = new AnimationClip { name = "GinsengDeath" };
        clip.frameRate = 50;
        float[] times = { 0, 0.12f, 0.26f, 0.42f, 0.58f, 0.74f, 0.90f, 1.06f, 1.38f };
        var keys = new ObjectReferenceKeyframe[times.Length];
        for (int i = 0; i < keys.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = times[i], value = frames[Mathf.Min(i, 7)] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        // Let all supplied poses play before fading the remaining leaves away.
        clip.SetCurve("", typeof(SpriteRenderer), "m_Color.a", new AnimationCurve(
            new Keyframe(0, 1), new Keyframe(1.1f, 1), new Keyframe(1.4f, 0)));
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.startTime = 0;
        settings.stopTime = 1.4f;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        if (create) AssetDatabase.CreateAsset(clip, path);
        else { EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip); }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/Ginseng.prefab");
        var controller = prefab.GetComponent<Animator>().runtimeAnimatorController as AnimatorController;
        if (controller == null) throw new InvalidOperationException("Ginseng Animator is missing.");
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(s => s.state).First(s => s.name == "GinsengDefeated");
        state.motion = clip;
        state.speed = 1;
        state.speedParameterActive = false;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }
}
