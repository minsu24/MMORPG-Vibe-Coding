using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class SkullMonkWalkSetup
    {
        public const string Sheet = "Assets/_Project/Art/Old Temple/SkullMonkWalk_Generated8.png";
        public const string ClipPath = "Assets/_Project/Animations/Old Temple/SkullMonkWalk.anim";
        public const string ControllerPath = "Assets/_Project/Animations/Old Temple/SkullMonk.controller";
        public const string TemplePath = "Assets/_Project/Scenes/OldTemple/TempleGate.unity";
        public const string Backup = "Assets/_Project/Animations/Old Temple/Backups/SkullMonkWalk_BeforeGenerated";
        private const string BackupClip = Backup + "/SkullMonkWalk_Original.anim";
        private const string BackupController = Backup + "/SkullMonk_Original.controller";
        private const string BackupObject = Backup + "/Skull_Original.prefab";
        private static readonly EditorCurveBinding Binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            Folder(parent); AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
        private static GameObject Skull(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).First(t => t.name == "Skull").gameObject;
        private static IEnumerable<AnimatorState> States(AnimatorStateMachine machine)
        {
            foreach (var state in machine.states) yield return state.state;
            foreach (var child in machine.stateMachines)
                foreach (var state in States(child.stateMachine)) yield return state;
        }

        [MenuItem("Eastern Fantasy/Monsters/Apply Generated Skull Monk Walk")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run outside Play Mode.");
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            var scene = SceneManager.GetSceneByPath(TemplePath);
            bool opened = !scene.isLoaded;
            var previous = SceneManager.GetActiveScene();
            if (opened) scene = EditorSceneManager.OpenScene(TemplePath, OpenSceneMode.Additive);
            try
            {
                var actor = Skull(scene);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
                PreserveOriginal(scene, actor, clip, controller);
                var original = AssetDatabase.LoadAssetAtPath<AnimationClip>(BackupClip);
                var reference = (Sprite)AnimationUtility.GetObjectReferenceCurve(original, Binding).First().value;
                var frames = Slice(reference);
                Undo.RecordObject(clip, "Apply generated Skull monk walk");
                clip.frameRate = 8f;
                AnimationUtility.SetObjectReferenceCurve(clip, Binding, frames.Select((sprite, index) =>
                    new ObjectReferenceKeyframe { time = index / 8f, value = sprite }).ToArray());
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.startTime = 0f; settings.stopTime = 1f; settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                var walk = States(controller.layers[0].stateMachine).First(state => state.name == "SkullMonkWalk");
                walk.motion = clip;
                controller.layers[0].stateMachine.defaultState = walk;
                actor.GetComponent<Animator>().runtimeAnimatorController = controller;
                actor.GetComponent<SpriteRenderer>().sprite = frames[0];
                EditorUtility.SetDirty(clip); EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(walk); EditorUtility.SetDirty(controller.layers[0].stateMachine);
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject = actor;
                Debug.Log("Skull monk: generated 8-frame, 1-second looping walk applied. Original assets and unsaved scene snapshot are in " + Backup);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static void PreserveOriginal(Scene scene, GameObject actor, AnimationClip clip, AnimatorController controller)
        {
            Folder(Backup);
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(BackupClip) != null)
            {
                if (AssetDatabase.LoadAssetAtPath<AnimatorController>(BackupController) == null
                    || AssetDatabase.LoadAssetAtPath<GameObject>(BackupObject) == null)
                    throw new InvalidOperationException("The original backup is incomplete; leaving it untouched.");
                return;
            }
            // A scene copy also preserves the user's currently unsaved Skull object and other scene edits.
            if (!EditorSceneManager.SaveScene(scene, Backup + "/TempleGate_Original.unity", true))
                throw new InvalidOperationException("Could not save the scene backup; leaving the animation unchanged.");
            var clipCopy = UnityEngine.Object.Instantiate(clip); clipCopy.name = "SkullMonkWalk_Original";
            AssetDatabase.CreateAsset(clipCopy, BackupClip);
            AssetDatabase.SaveAssetIfDirty(controller);
            if (!AssetDatabase.CopyAsset(ControllerPath, BackupController)) throw new InvalidOperationException("Could not back up Skull controller.");
            var controllerCopy = AssetDatabase.LoadAssetAtPath<AnimatorController>(BackupController);
            foreach (var state in controllerCopy.layers.SelectMany(layer => States(layer.stateMachine)))
                if (state.motion == clip) { state.motion = clipCopy; EditorUtility.SetDirty(state); }
            EditorUtility.SetDirty(controllerCopy);
            PrefabUtility.SaveAsPrefabAsset(actor, BackupObject);
            var root = PrefabUtility.LoadPrefabContents(BackupObject);
            try
            {
                root.GetComponent<Animator>().runtimeAnimatorController = controllerCopy;
                PrefabUtility.SaveAsPrefabAsset(root, BackupObject);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }

        private static Sprite[] Slice(Sprite reference)
        {
            AssetDatabase.ImportAsset(Sheet);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Sheet);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = reference.pixelsPerUnit;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var regions = new List<RectInt>();
            int[] footRows;
            try
            {
                ImageConversion.LoadImage(texture, File.ReadAllBytes(Sheet));
                var pixels = texture.GetPixels32();
                int start = -1, last = -1;
                for (int x = 0; x <= texture.width + 8; x++)
                {
                    bool occupied = false;
                    if (x < texture.width)
                        for (int y = 0; y < texture.height; y++) if (pixels[y * texture.width + x].a >= 128) { occupied = true; break; }
                    if (occupied) { if (start < 0) start = x; last = x; }
                    else if (start >= 0 && x - last >= 8)
                    {
                        int bottom = texture.height, top = 0;
                        for (int xx = start; xx <= last; xx++)
                            for (int y = 0; y < texture.height; y++)
                                if (pixels[y * texture.width + xx].a >= 128) { bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                        regions.Add(new RectInt(start, bottom, last - start + 1, top - bottom + 1));
                        start = -1;
                    }
                }
                if (regions.Count != 8) throw new InvalidOperationException("Expected eight isolated sprites; detected " + regions.Count);
                footRows = regions.Select(region => region.yMin).OrderBy(y => y).ToArray();
                int bottomRow = Mathf.Max(0, regions.Min(region => region.yMin) - 8);
                int topRow = Mathf.Min(texture.height, regions.Max(region => region.yMax) + 8);
                float baseline = (footRows[3] + footRows[4]) * .5f;
                var factory = new SpriteDataProviderFactories(); factory.Init();
                var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
                var existing = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);
                var slices = new SpriteRect[8];
                var pairs = new SpriteNameFileIdPair[8];
                float ppu = reference.pixelsPerUnit;
                float groundOffset = reference.bounds.min.y;
                for (int index = 0; index < regions.Count; index++)
                {
                    var region = regions[index];
                    int left = index == 0 ? Mathf.Max(0, region.xMin - 8) : (regions[index - 1].xMax + region.xMin) / 2;
                    int right = index == 7 ? Mathf.Min(texture.width, region.xMax + 8) : (region.xMax + regions[index + 1].xMin) / 2;
                    string name = "SkullMonkWalkGenerated_" + index.ToString("00");
                    var id = existing.TryGetValue(name, out var value) ? value : GUID.Generate();
                    slices[index] = new SpriteRect { name = name, spriteID = id, rect = new Rect(left, bottomRow, right - left, topRow - bottomRow),
                        alignment = SpriteAlignment.Custom, pivot = new Vector2((region.center.x - left) / (right - left),
                            (baseline - bottomRow - groundOffset * ppu) / (topRow - bottomRow)) };
                    pairs[index] = new SpriteNameFileIdPair(name, id);
                }
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = ppu; importer.maxTextureSize = 4096;
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                provider.SetSpriteRects(slices);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
                provider.Apply(); importer.SaveAndReimport();
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            return AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
        }

        [MenuItem("Eastern Fantasy/Monsters/Restore Skull Monk Original Walk")]
        public static void Restore()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run outside Play Mode.");
            var backup = AssetDatabase.LoadAssetAtPath<AnimationClip>(BackupClip);
            if (backup == null) throw new InvalidOperationException("Original walk backup is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            EditorUtility.CopySerialized(backup, clip); clip.name = "SkullMonkWalk"; EditorUtility.SetDirty(clip);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            var saved = AssetDatabase.LoadAssetAtPath<AnimatorController>(BackupController);
            var originalDefault = saved.layers[0].stateMachine.defaultState.name;
            controller.layers[0].stateMachine.defaultState = States(controller.layers[0].stateMachine).First(state => state.name == originalDefault);
            EditorUtility.SetDirty(controller.layers[0].stateMachine); EditorUtility.SetDirty(controller);
            var scene = SceneManager.GetSceneByPath(TemplePath);
            bool opened = !scene.isLoaded;
            var previous = SceneManager.GetActiveScene();
            if (opened) scene = EditorSceneManager.OpenScene(TemplePath, OpenSceneMode.Additive);
            try
            {
                var actor = Skull(scene);
                actor.GetComponent<Animator>().runtimeAnimatorController = controller;
                actor.GetComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<GameObject>(BackupObject).GetComponent<SpriteRenderer>().sprite;
                EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
