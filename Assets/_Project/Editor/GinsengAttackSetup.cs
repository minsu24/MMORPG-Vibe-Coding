using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class GinsengAttackSetup
{
    private const string Art = "Assets/_Project/Art/Old Temple/Monster/인삼 몬스터 공격.png";
    private const string Folder = "Assets/_Project/Animation/Old Temple/Ginseng";

    [MenuItem("Eastern Fantasy/Monsters/Set Up Ginseng Root Attack")]
    public static void Apply()
    {
        EnsureFolder(Folder);
        var frames = Slice();
        string path = Folder + "/GinsengAttack.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        bool create = clip == null;
        if (create) clip = new AnimationClip { name = "GinsengAttack" };
        clip.frameRate = 50;
        float[] times = { 0, 0.10f, 0.20f, 0.30f, 0.40f, 0.52f, 0.64f, 0.76f, 0.86f };
        var keys = new ObjectReferenceKeyframe[times.Length];
        for (int i = 0; i < keys.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = times[i], value = frames[Mathf.Min(i, 7)] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.startTime = 0;
        settings.stopTime = 0.88f;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AnimationUtility.SetAnimationEvents(clip, new[] {
            new AnimationEvent { time = 0.40f, functionName = "GinsengAttackHit" },
            new AnimationEvent { time = 0.86f, functionName = "GinsengAttackFinished" }
        });
        if (create) AssetDatabase.CreateAsset(clip, path);
        else { EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip); }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/Ginseng.prefab");
        var controller = prefab != null ? prefab.GetComponent<Animator>().runtimeAnimatorController as AnimatorController : null;
        if (controller == null) throw new InvalidOperationException("Build the Ginseng base prefab first.");
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "GinsengAttack") ?? machine.AddState("GinsengAttack");
        state.motion = clip;
        state.speed = 1;
        state.speedParameterActive = false;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        const string prefabPath = "Assets/_Project/Prefabs/Enemy/Ginseng.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var data = new SerializedObject(root.GetComponent<MON_Ginseng>());
            data.FindProperty("_attackPower").floatValue = 10;
            data.FindProperty("attackRange").floatValue = 2f;
            data.FindProperty("attackReach").floatValue = 2f;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }

    private static Sprite[] Slice()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Art);
        var factories = new SpriteDataProviderFactories(); factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var old = provider.GetSpriteRects();
        var texture = new Texture2D(2, 2);
        Texture2D output = null;
        try
        {
            texture.LoadImage(System.IO.File.ReadAllBytes(Art));
            if (texture.width == 3584 && texture.height == 424 && old.Length == 8)
                return AssetDatabase.LoadAllAssetsAtPath(Art).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            int w = texture.width, h = texture.height, length = w * h;
            var pixels = texture.GetPixels32();
            var labels = Enumerable.Repeat(-1, length).ToArray();
            var queue = new int[length];
            var sizes = new List<int>();
            var lefts = new List<int>();
            for (int start = 0; start < length; start++)
            {
                if (labels[start] >= 0 || pixels[start].a < 16) continue;
                int id = sizes.Count, head = 0, tail = 0, left = w;
                labels[start] = id; queue[tail++] = start;
                while (head < tail)
                {
                    int n = queue[head++], x = n % w, y = n / w;
                    left = Math.Min(left, x);
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int xx = x + dx, yy = y + dy;
                        if (xx < 0 || xx >= w || yy < 0 || yy >= h) continue;
                        int k = yy * w + xx;
                        if (labels[k] < 0 && pixels[k].a >= 16) { labels[k] = id; queue[tail++] = k; }
                    }
                }
                sizes.Add(tail); lefts.Add(left);
            }
            var major = Enumerable.Range(0, sizes.Count).Where(i => sizes[i] > 6000).OrderBy(i => lefts[i]).ToArray();
            if (major.Length != 8) throw new InvalidOperationException("Expected eight distinct attack poses.");
            var assignment = new int[length];
            int read = 0, write = 0;
            for (int i = 0; i < length; i++)
            {
                assignment[i] = Array.IndexOf(major, labels[i]);
                if (assignment[i] >= 0) queue[write++] = i;
            }
            // Assign detached tips and antialiased edges to the nearest pose, never a neighbouring crop.
            while (read < write)
            {
                int n = queue[read++], x = n % w, y = n / w;
                for (int dir = 0; dir < 4; dir++)
                {
                    int xx = x + (dir == 0 ? -1 : dir == 1 ? 1 : 0), yy = y + (dir == 2 ? -1 : dir == 3 ? 1 : 0);
                    if (xx < 0 || xx >= w || yy < 0 || yy >= h) continue;
                    int k = yy * w + xx;
                    if (assignment[k] < 0) { assignment[k] = assignment[n]; queue[write++] = k; }
                }
            }
            var anchors = new int[8];
            for (int pose = 0; pose < 8; pose++)
            {
                var columns = new int[w];
                for (int y = 420; y < 445; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = pixels[y * w + x];
                    if (assignment[y * w + x] == pose && p.a > 180 && p.r > 145 && p.g > 100 && p.r > p.g * 1.03f) columns[x]++;
                }
                int maximum = columns.Max();
                var headColumns = Enumerable.Range(0, w).Where(x => columns[x] > maximum * 0.8f).ToArray();
                if (headColumns.Length == 0) throw new InvalidOperationException("Missing head anchor.");
                anchors[pose] = Mathf.RoundToInt((float)headColumns.Average());
            }
            const int frameWidth = 448, frameHeight = 424, anchorX = 272, baseY = 186;
            var result = new Color32[frameWidth * 8 * frameHeight];
            for (int i = 0; i < length; i++)
            {
                if (pixels[i].a == 0) continue;
                int pose = assignment[i], x = i % w - anchors[pose] + anchorX, y = i / w - baseY;
                if (x < 0 || x >= frameWidth || y < 0 || y >= frameHeight)
                {
                    if (pixels[i].a >= 16) throw new InvalidOperationException("Attack artwork would be clipped.");
                    continue;
                }
                result[y * frameWidth * 8 + pose * frameWidth + x] = pixels[i];
            }
            output = new Texture2D(frameWidth * 8, frameHeight, TextureFormat.RGBA32, false);
            output.SetPixels32(result); output.Apply();
            System.IO.File.WriteAllBytes(Art, output.EncodeToPNG());
            var rects = new SpriteRect[8];
            var pairs = new SpriteNameFileIdPair[8];
            for (int i = 0; i < 8; i++)
            {
                string name = "GinsengAttack_" + i.ToString("00");
                var existing = old.FirstOrDefault(r => r.name == name);
                var id = existing != null ? existing.spriteID : GUID.Generate();
                rects[i] = new SpriteRect { name = name, rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight),
                    spriteID = id, alignment = SpriteAlignment.Custom, pivot = new Vector2(anchorX / (float)frameWidth, 4f / frameHeight) };
                pairs[i] = new SpriteNameFileIdPair(name, id);
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 200;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
            provider.Apply(); importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(Art).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
            if (output != null) UnityEngine.Object.DestroyImmediate(output);
        }
    }
}
