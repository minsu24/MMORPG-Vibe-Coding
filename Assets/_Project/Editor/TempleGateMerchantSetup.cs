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
        private const float IdleDuration = 2.4f;
        // Keep the on-screen scale of the seated idle artwork as the reference.
        private const float SeatedWidth = 258.375f / 134.4f;
        private const float FootOffset = 70f / 136f;
        private const float FootBandHeight = 36f / 136f;

        [MenuItem("Eastern Fantasy/Old Temple/Repair Monkey Merchant Sprite Frames")]
        public static string RepairSpriteFrames()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before repairing sprites.");
            return MerchantPipeTransitionSetup.Apply();
        }

        [MenuItem("Eastern Fantasy/Old Temple/Configure Temple Gate Monkey Merchant")]
        public static string Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before configuring the scene.");
            EnsureFolder(AnimationFolder); EnsureFolder(DataFolder);
            var (idle, bridge, pipe) = MerchantPipeTransitionSetup.ImportCharacterPoses();
            var idleClip = MakeIdleClip(idle);
            string controllerPath = AnimationFolder + "/MonkeyMerchant.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            machine.defaultState = State(machine, MonkeyMerchantAmbientAnimation.IdleState, idleClip);
            MerchantPipeTransitionSetup.ConfigureClips(idle[0], pipe, bridge);
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
                    root.transform.localScale = new Vector3(.8f, .8f, 1f);
                }
                var renderer = GetOrAdd<SpriteRenderer>(root);
                renderer.sharedMaterial = MerchantPipeTransitionSetup.IdleToneMaterial(); renderer.sortingLayerID = traveler.sortingLayerID;
                renderer.sortingOrder = traveler.sortingOrder; renderer.sprite = idle[0];
                var animator = GetOrAdd<Animator>(root); animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var trigger = GetOrAdd<CapsuleCollider2D>(root);
                trigger.isTrigger = true; trigger.size = new Vector2(1.6f, 2.4f); trigger.offset = new Vector2(0f, 1.2f);
                var merchant = GetOrAdd<Shopkeeper>(root);
                var data = new SerializedObject(merchant);
                data.FindProperty("greetingDialogue").objectReferenceValue = greeting;
                data.FindProperty("unvisitedRegion").stringValue = EasternFantasy.UI.MapRegionDiscovery.GiantFarm;
                data.FindProperty("unvisitedRegionDialogue").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<DialogueSequence>(DataFolder + "/MonkeyMerchantFarmRumor.asset");
                data.FindProperty("shopWindowPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/ShopWindow.prefab");
                data.FindProperty("dialogueUiPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/SkullMonkDialogueUI.prefab");
                var list = data.FindProperty("stock"); list.arraySize = stock.Length;
                for (int i = 0; i < stock.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = stock[i];
                data.ApplyModifiedPropertiesWithoutUndo();
                var ambient = GetOrAdd<MonkeyMerchantAmbientAnimation>(root);
                data = new SerializedObject(ambient);
                data.FindProperty("pipeInterval").vector2Value = new Vector2(8f, 14f);
                data.FindProperty("pipeDuration").floatValue = MonkeyMerchantAmbientAnimation.PipeDuration;
                data.FindProperty("idleAnchorFrame").objectReferenceValue = idle[0];
                MerchantPipeSmokeSetup.Configure(data, pipe[MerchantPipeTransitionSetup.ExhalePoseIndex]);
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(root); EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save TempleGate.");
                AssetDatabase.SaveAssets(); Selection.activeGameObject = root;
                return "TempleGate/MonkeyMerchant: seated idle and pipe animations, detached smoke, existing shop UI and village stock, T greeting.";
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        private static T GetOrAdd<T>(GameObject root) where T : Component
        {
            var component = root.GetComponent<T>(); return component != null ? component : root.AddComponent<T>();
        }

        internal static Sprite[] Slice(string path, int frameCount = 8, int rows = 1)
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
                if (bodies.Count != frameCount)
                    throw new InvalidOperationException($"Expected {frameCount} separate merchant silhouettes: {path}");
                int columns = bodies.Count / rows;
                bodies = bodies.OrderByDescending(body => body.Min(p => p.y))
                    .Select((body, index) => (body, row: index / columns)).GroupBy(entry => entry.row, entry => entry.body)
                    .SelectMany(row => row.OrderBy(body => body.Min(p => p.x))).ToList();
                // Measure the planted lower body so the raised pipe and head poses do not affect the scale.
                float pixelsPerUnit = Mathf.Round(10f * bodies.Average(SeatedWidthInPixels) / SeatedWidth) / 10f;
                var cells = new Rect[frameCount]; var pivots = new Vector2[frameCount];
                for (int i = 0; i < bodies.Count; i++)
                {
                    var body = bodies[i];
                    int left = body.Min(p => p.x), right = body.Max(p => p.x), bottom = body.Min(p => p.y), top = body.Max(p => p.y);
                    // Anchor the seated right foot, which remains planted in both motions.
                    var foot = body.Where(p => p.x > left + (right - left) * .65f && p.y < bottom + FootBandHeight * pixelsPerUnit && pixels[p.y * raw.width + p.x].a >= 128).ToArray();
                    float footX = (float)foot.Average(p => p.x), footY = foot.Min(p => p.y);
                    int xMin = Mathf.Max(0, left - 2), yMin = Mathf.Max(0, bottom - 2);
                    cells[i] = new Rect(xMin, yMin, Mathf.Min(raw.width, right + 3) - xMin, Mathf.Min(raw.height, top + 3) - yMin);
                    pivots[i] = new Vector2((footX - FootOffset * pixelsPerUnit - xMin) / cells[i].width, (footY - yMin) / cells[i].height);
                }
                // Tight outlines omit the tiny neighboring fragments where silhouettes overlap in X.
                SkullMonkMeleeSetup.Slice(path, cells, pixelsPerUnit, pivots, true);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.filterMode = FilterMode.Bilinear; importer.npotScale = TextureImporterNPOTScale.None;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.Tight; importer.SetTextureSettings(settings); importer.SaveAndReimport();

                float SeatedWidthInPixels(List<Vector2Int> body)
                {
                    var opaque = body.Where(p => pixels[p.y * raw.width + p.x].a >= 96).ToArray();
                    int sole = opaque.Min(p => p.y);
                    int height = opaque.Max(p => p.y) - sole;
                    var lowerBody = opaque.Where(p => p.y >= sole + height * .15f && p.y <= sole + height * .42f);
                    return lowerBody.Max(p => p.x) - lowerBody.Min(p => p.x) + 1;
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
            var frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (frames.Length != frameCount) throw new InvalidOperationException($"Expected {frameCount} frames: {path}");
            return frames;
        }

        internal static AnimationClip MakeIdleClip(Sprite[] frames)
        {
            string name = MonkeyMerchantAmbientAnimation.IdleState;
            string path = AnimationFolder + "/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            // The .3-second sprite holds land exactly on this grid.
            clip.name = name; clip.frameRate = 10f;
            var keys = frames.Select((s, i) => new ObjectReferenceKeyframe { time = i * IdleDuration / frames.Length, value = s }).ToList();
            // Unity includes one sample after the final sprite key in the clip length.
            float lastSample = (Mathf.RoundToInt(IdleDuration * clip.frameRate) - 1) / clip.frameRate;
            keys.Add(new ObjectReferenceKeyframe { time = lastSample, value = frames[frames.Length - 1] });
            var spriteKeys = keys.ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), spriteKeys);
            MerchantPipeTransitionSetup.ApplyTone(clip, spriteKeys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
            settings.startTime = 0f; settings.stopTime = IdleDuration; AnimationUtility.SetAnimationClipSettings(clip, settings);
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
            const string itemFolder = "Assets/_Project/Data/Items/";
            var health = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemFolder + "HealthPotion.asset");
            var mana = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemFolder + "ManaPotion.asset");
            var advancedHealth = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemFolder + "AdvancedHealthPotion.asset");
            var advancedMana = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemFolder + "AdvancedManaPotion.asset");
            if (!health || !mana || !advancedHealth || !advancedMana)
                throw new InvalidOperationException("Missing a potion definition for the merchant stock.");

            const string path = "Assets/_Project/Scenes/TutorialMap/PrologueVilage.unity";
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var shop = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Shopkeeper>(true)).First(s => s.Stock != null && s.Stock.Length > 0);
                return shop.Stock.Select(item => item == health ? advancedHealth : item == mana ? advancedMana : item).ToArray();
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
