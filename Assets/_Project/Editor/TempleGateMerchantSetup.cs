using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using EasternFantasy.Shop;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class TempleGateMerchantSetup
    {
        public const string ScenePath = TempleGateBlacksmithSetup.ScenePath;
        public const string ObjectName = "MonkeyMerchant";
        public const string AnimationFolder = "Assets/_Project/Animations/Old Temple/MonkeyMerchant";
        private const string Art = TempleGateBlacksmithSetup.Art;
        private const string DataFolder = TempleGateBlacksmithSetup.DataFolder;

        [MenuItem("Eastern Fantasy/Old Temple/Repair Monkey Merchant Sprite Frames")]
        public static string RepairSpriteFrames()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before repairing sprites.");
            var idle = Slice(Art + "원숭이 상인 대기.png");
            var pipe = Slice(Art + "원숭이 상인 곰방대.png");
            MakeClip(MonkeyMerchantAmbientAnimation.IdleState, idle, 1.6f, true);
            MakeClip(MonkeyMerchantAmbientAnimation.PipeState, PipeWithSmokeHandoff(pipe, idle[0]), 2.4f, false);
            var scene = SceneManager.GetSceneByPath(ScenePath); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var root = scene.GetRootGameObjects().Single(g => g.name == ObjectName);
                root.GetComponent<SpriteRenderer>().sprite = idle[0];
                var data = new SerializedObject(root.GetComponent<MonkeyMerchantAmbientAnimation>());
                data.FindProperty("pipeDuration").floatValue = 2.4f;
                MerchantPipeSmokeSetup.Configure(data, pipe[7]); data.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Selection.activeGameObject = root;
                return "Repaired all 16 merchant frames: silhouette boundaries, isolated meshes, individually planted foot pivots, smoke offset updated.";
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        [MenuItem("Eastern Fantasy/Old Temple/Configure Temple Gate Monkey Merchant")]
        public static string Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before configuring the scene.");
            EnsureFolder(AnimationFolder); EnsureFolder(DataFolder);
            var idle = Slice(Art + "원숭이 상인 대기.png");
            var pipe = Slice(Art + "원숭이 상인 곰방대.png");
            var idleClip = MakeClip(MonkeyMerchantAmbientAnimation.IdleState, idle, 1.6f, true);
            var pipeClip = MakeClip(MonkeyMerchantAmbientAnimation.PipeState, PipeWithSmokeHandoff(pipe, idle[0]), 2.4f, false);
            string controllerPath = AnimationFolder + "/MonkeyMerchant.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            machine.defaultState = State(machine, MonkeyMerchantAmbientAnimation.IdleState, idleClip);
            State(machine, MonkeyMerchantAmbientAnimation.PipeState, pipeClip);
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(machine);
            var greeting = Greeting();
            var stock = VillageStock();
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                var traveler = all.Single(t => t.name == "Traveler").GetComponent<SpriteRenderer>();
                var floor = all.Single(t => t.name == "Floor").GetComponent<Collider2D>();
                var root = scene.GetRootGameObjects().SingleOrDefault(r => r.name == ObjectName);
                if (root == null)
                {
                    root = new GameObject(ObjectName); SceneManager.MoveGameObjectToScene(root, scene);
                    Undo.RegisterCreatedObjectUndo(root, "Add TempleGate monkey merchant");
                    root.transform.position = new Vector3(2.5f, floor.bounds.max.y + .015f, 0f);
                }
                var renderer = GetOrAdd<SpriteRenderer>(root);
                renderer.sharedMaterial = traveler.sharedMaterial; renderer.sortingLayerID = traveler.sortingLayerID;
                renderer.sortingOrder = traveler.sortingOrder; renderer.sprite = idle[0];
                var animator = GetOrAdd<Animator>(root); animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var trigger = GetOrAdd<CapsuleCollider2D>(root);
                trigger.isTrigger = true; trigger.size = new Vector2(1.6f, 2.4f); trigger.offset = new Vector2(0f, 1.2f);
                var merchant = GetOrAdd<Shopkeeper>(root);
                var data = new SerializedObject(merchant);
                data.FindProperty("greetingDialogue").objectReferenceValue = greeting;
                data.FindProperty("shopWindowPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/ShopWindow.prefab");
                data.FindProperty("dialogueUiPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/SkullMonkDialogueUI.prefab");
                var list = data.FindProperty("stock"); list.arraySize = stock.Length;
                for (int i = 0; i < stock.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = stock[i];
                data.ApplyModifiedPropertiesWithoutUndo();
                var ambient = GetOrAdd<MonkeyMerchantAmbientAnimation>(root);
                data = new SerializedObject(ambient);
                data.FindProperty("pipeInterval").vector2Value = new Vector2(8f, 14f);
                data.FindProperty("pipeDuration").floatValue = 2.4f;
                MerchantPipeSmokeSetup.Configure(data, pipe[7]);
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(root); EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save TempleGate.");
                AssetDatabase.SaveAssets(); Selection.activeGameObject = root;
                return "TempleGate/MonkeyMerchant: scene object, 8 idle and 8 pipe frames, existing shop UI and village stock, T greeting with monkey portrait.";
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        private static T GetOrAdd<T>(GameObject root) where T : Component
        {
            var component = root.GetComponent<T>(); return component != null ? component : root.AddComponent<T>();
        }

        private static Sprite[] Slice(string path)
        {
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(raw, File.ReadAllBytes(path))) throw new InvalidOperationException("Cannot load " + path);
                var pixels = raw.GetPixels32();
                var visited = new bool[pixels.Length];
                var bodies = new List<List<Vector2Int>>();
                for (int index = 0; index < pixels.Length; index++)
                {
                    if (visited[index] || pixels[index].a < 32) continue;
                    var queue = new Queue<int>(); var body = new List<Vector2Int>();
                    queue.Enqueue(index); visited[index] = true;
                    while (queue.Count > 0)
                    {
                        int point = queue.Dequeue(), x = point % raw.width, y = point / raw.width;
                        body.Add(new Vector2Int(x, y));
                        for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || nx >= raw.width || ny < 0 || ny >= raw.height) continue;
                            int next = ny * raw.width + nx;
                            if (visited[next] || pixels[next].a < 32) continue;
                            visited[next] = true; queue.Enqueue(next);
                        }
                    }
                    if (body.Count > 10000) bodies.Add(body);
                }
                bodies = bodies.OrderBy(body => body.Min(p => p.x)).ToList();
                if (bodies.Count != 8) throw new InvalidOperationException("Expected eight separate merchant silhouettes: " + path);
                var cells = new Rect[8]; var pivots = new Vector2[8];
                for (int i = 0; i < bodies.Count; i++)
                {
                    var body = bodies[i];
                    int left = body.Min(p => p.x), right = body.Max(p => p.x), bottom = body.Min(p => p.y), top = body.Max(p => p.y);
                    // Anchor the seated right foot, which remains planted in both motions.
                    var foot = body.Where(p => p.x > left + (right - left) * .65f && p.y < bottom + 36 && pixels[p.y * raw.width + p.x].a >= 128).ToArray();
                    float footX = (float)foot.Average(p => p.x), footY = foot.Min(p => p.y);
                    int xMin = Mathf.Max(0, left - 2), yMin = Mathf.Max(0, bottom - 2);
                    cells[i] = new Rect(xMin, yMin, Mathf.Min(raw.width, right + 3) - xMin, Mathf.Min(raw.height, top + 3) - yMin);
                    pivots[i] = new Vector2((footX - 70f - xMin) / cells[i].width, (footY - yMin) / cells[i].height);
                }
                // Tight outlines omit the tiny neighboring fragments where silhouettes overlap in X.
                SkullMonkMeleeSetup.Slice(path, cells, 136f, pivots, true);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.filterMode = FilterMode.Bilinear; importer.npotScale = TextureImporterNPOTScale.None;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.Tight; importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
            var frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (frames.Length != 8) throw new InvalidOperationException("Expected eight frames: " + path);
            return frames;
        }

        private static Sprite[] PipeWithSmokeHandoff(Sprite[] frames, Sprite idle)
        {
            var result = (Sprite[])frames.Clone();
            // At frame eight the independent smoke starts and the merchant returns to idle.
            // Do not display the sheet's baked cloud before the separate animation starts.
            result[7] = idle;
            return result;
        }

        private static AnimationClip MakeClip(string name, Sprite[] frames, float duration, bool loop)
        {
            string path = AnimationFolder + "/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            // Both .2-second idle frames and .3-second pipe frames land exactly on this grid.
            clip.name = name; clip.frameRate = 10f;
            var keys = frames.Select((s, i) => new ObjectReferenceKeyframe { time = i * duration / frames.Length, value = s }).ToList();
            keys.Add(new ObjectReferenceKeyframe { time = duration, value = loop ? frames[0] : frames[frames.Length - 1] });
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop;
            settings.startTime = 0f; settings.stopTime = duration; AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip); return clip;
        }

        private static AnimatorState State(AnimatorStateMachine machine, string name, AnimationClip clip)
        {
            var state = machine.states.FirstOrDefault(s => s.state.name == name).state;
            if (state == null) state = machine.AddState(name);
            state.motion = clip; EditorUtility.SetDirty(state); return state;
        }

        private static ItemDefinition[] VillageStock()
        {
            const string path = "Assets/_Project/Scenes/TutorialMap/PrologueVilage.unity";
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var shop = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Shopkeeper>(true)).First(s => s.Stock != null && s.Stock.Length > 0);
                return shop.Stock.ToArray();
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        private static DialogueSequence Greeting()
        {
            string path = Art + "원숭이 상인 일러스트.png"; AssetDatabase.ImportAsset(path);
            var portrait = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderByDescending(s => s.rect.width * s.rect.height).First();
            var character = Asset<DialogueCharacter>(DataFolder + "/MonkeyMerchantCharacter.asset");
            var data = new SerializedObject(character);
            data.FindProperty("displayName").stringValue = "원숭이 상인";
            data.FindProperty("portrait").objectReferenceValue = portrait;
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(character);
            var dialogue = Asset<DialogueSequence>(DataFolder + "/MonkeyMerchantGreeting.asset");
            data = new SerializedObject(dialogue);
            data.FindProperty("partnerCharacter").objectReferenceValue = character;
            data.FindProperty("playerCharacter").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DialogueCharacter>("Assets/_Project/Data/Tutorial_DIalogue/DialoguePlayer.asset");
            var lines = data.FindProperty("lines"); lines.arraySize = 1;
            lines.GetArrayElementAtIndex(0).FindPropertyRelative("speaker").enumValueIndex = (int)DialogueSpeakerSide.Partner;
            lines.GetArrayElementAtIndex(0).FindPropertyRelative("text").stringValue = "허허, 먼 길을 왔구먼. 필요한 물건이 있으면 살펴보게. 자네 물건도 사 주지.";
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(dialogue); return dialogue;
        }

        private static T Asset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(parent.Length + 1));
        }
    }
}
