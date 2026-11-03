using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SmogMountainAtmosphereSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/OldTemple/SmogMountain.unity";
    private const string ArtPath = "Assets/_Project/Art/Old Temple/Map/안개 낀 산길 장식.png";

    [MenuItem("Eastern Fantasy/Smog Mountain/Set Up Moving Decorations")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var original = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        // Preserve unrelated unsaved work when invoked from another scene.
        if (!opened && scene.isDirty) throw new InvalidOperationException("Save SmogMountain before rebuilding its decorations.");
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            SliceArt();
            var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Moving Mountain Decorations");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var root = new GameObject("Moving Mountain Decorations");
            SceneManager.MoveGameObjectToScene(root, scene);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(ArtPath).OfType<Sprite>().ToArray();
            var grass = sprites.Where(s => s.name.StartsWith("MountainGrass_")).OrderBy(s => s.name).ToArray();
            var tree = sprites.Where(s => s.name.StartsWith("MountainTree_")).OrderBy(s => s.name).ToArray();
            var leaves = sprites.Where(s => s.name.StartsWith("MountainLeaves_")).OrderBy(s => s.name).ToArray();
            var cloud = sprites.Where(s => s.name.StartsWith("MountainCloud_")).OrderBy(s => s.name).ToArray();
            float groundY = scene.GetRootGameObjects().First(g => g.name == "Floor")
                .GetComponent<Collider2D>().bounds.max.y + 0.02f;
            float[] grassX = { -10.3f, -5.5f, -0.2f, 5.4f, 12.8f, 18.2f, 24.8f };
            for (int i = 0; i < grassX.Length; i++)
                Add(root, "Windblown Grass " + (i + 1), SmogMountainDecoration.Motion.Grass,
                    grass, i % 3, new Vector2(grassX[i], groundY), 0.82f + (i % 3) * 0.09f, 3, 1f, i * 1.7f);
            Add(root, "Left Mountain Pine", SmogMountainDecoration.Motion.Tree, tree, 0,
                new Vector2(-6.2f, groundY), 1.1f, 3, 1f, 0.4f);
            Add(root, "Right Mountain Pine", SmogMountainDecoration.Motion.Tree, tree, 2,
                new Vector2(20.2f, groundY), 1.12f, 3, 1f, 2.6f);
            for (int i = 0; i < 5; i++)
                Add(root, "Drifting Maple Leaves " + (i + 1), SmogMountainDecoration.Motion.Leaves,
                    leaves, i % 3, new Vector2(-6f + i * 7f, 0.6f + (i % 2) * 0.5f),
                    0.24f + (i % 2) * 0.04f, 3, 0.65f, i * 1.22f + 0.3f);
            for (int i = 0; i < 4; i++)
                Add(root, "Slow Mountain Cloud " + (i + 1), SmogMountainDecoration.Motion.Cloud,
                    cloud, i % 3, new Vector2(-5f + i * 8f, i % 2 == 0 ? 2.9f : 0.8f),
                    1.3f + (i % 2) * 0.2f, 2, 0.43f, i * 1.8f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void Add(GameObject root, string name, SmogMountainDecoration.Motion motion,
        Sprite[] frames, int initial, Vector2 position, float scale, int order, float opacity, float phase)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, root.scene);
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = position;
        go.transform.localScale = Vector3.one * scale;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = frames[initial];
        renderer.sortingOrder = order;
        renderer.color = new Color(1, 1, 1, opacity);
        var movement = go.AddComponent<SmogMountainDecoration>();
        movement.motion = motion;
        movement.frames = frames;
        movement.phase = phase;
        movement.opacity = opacity;
    }

    private static void SliceArt()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath);
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var existing = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
        var rects = new List<SpriteRect>();
        var pairs = new List<SpriteNameFileIdPair>();
        string[] groups = { "MountainGrass", "MountainLeaves", "MountainTree", "MountainCloud" };
        int[] rowY = { 681, 520, 200, 0 };
        int[] rowHeight = { 260, 146, 303, 195 };
        float[] grassBase = { 273, 800, 1330 };
        float[] treeBase = { 156, 686, 1226 };
        for (int row = 0; row < 4; row++)
        for (int col = 0; col < 3; col++)
        {
            int x = col * 557, width = col == 2 ? 558 : 557;
            string name = groups[row] + "_" + col;
            var id = existing.TryGetValue(name, out var old) ? old : GUID.Generate();
            Vector2 pivot = row == 0 ? new Vector2((grassBase[col] - x) / width, 18f / rowHeight[row])
                : row == 2 ? new Vector2((treeBase[col] - x) / width, 20f / rowHeight[row])
                : Vector2.one * 0.5f;
            rects.Add(new SpriteRect { name = name, rect = new Rect(x, rowY[row], width, rowHeight[row]),
                alignment = SpriteAlignment.Custom, pivot = pivot, spriteID = id });
            pairs.Add(new SpriteNameFileIdPair(name, id));
        }
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        provider.SetSpriteRects(rects.ToArray());
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
        provider.Apply();
        importer.SaveAndReimport();
    }

    public static string RenderPreview(float time, string filename)
    {
        var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
        RenderTexture target = null;
        Texture2D image = null;
        Camera camera = null;
        var previous = RenderTexture.active;
        try
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var decoration in root.GetComponentsInChildren<SmogMountainDecoration>()) decoration.Sample(time);
            camera = scene.GetRootGameObjects().First(r => r.name == "Main Camera").GetComponent<Camera>();
            camera.scene = scene;
            camera.transform.position = new Vector3(7.5555f, 0.82625f, -22.1594f);
            camera.orthographicSize = 5.882f;
            target = new RenderTexture(2560, 805, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image = new Texture2D(2560, 805, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 2560, 805), 0, 0);
            image.Apply();
            System.IO.Directory.CreateDirectory("Artifacts/SmogMountainDecorations");
            string path = "Artifacts/SmogMountainDecorations/" + filename + ".png";
            System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            return path;
        }
        finally
        {
            RenderTexture.active = previous;
            if (camera != null) camera.targetTexture = null;
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
