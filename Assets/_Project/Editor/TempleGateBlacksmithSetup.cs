using System;
using System.IO;
using System.Linq;
using EasternFantasy.Dialogue;
using EasternFantasy.EquipmentUpgrade;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class TempleGateBlacksmithSetup
    {
        public const string ScenePath = "Assets/_Project/Scenes/OldTemple/TempleGate.unity";
        public const string ObjectName = "GoblinBlacksmith";
        public const string Art = "Assets/_Project/Art/Old Temple/NPC/";
        public const string AnimationFolder = "Assets/_Project/Animations/Old Temple/GoblinBlacksmith";
        public const string DataFolder = "Assets/_Project/Data/OldTempleNPC";
        private const float PixelsPerUnit = 136f;

        [MenuItem("Eastern Fantasy/Old Temple/Configure Temple Gate Blacksmith")]
        public static string Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before configuring the scene.");
            EnsureFolder(AnimationFolder); EnsureFolder(DataFolder);
            var idle = Slice(Art + "도깨비 대장장이 대기.png");
            var hammer = Slice(Art + "도깨비 대장장이 망치질.png");
            var idleClip = MakeClip(BlacksmithAmbientAnimation.IdleState, idle, 1.2f, true);
            var hammerClip = MakeClip(BlacksmithAmbientAnimation.HammerState, hammer, 1.1f, false);
            string controllerPath = AnimationFolder + "/GoblinBlacksmith.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            var idleState = State(machine, BlacksmithAmbientAnimation.IdleState, idleClip);
            State(machine, BlacksmithAmbientAnimation.HammerState, hammerClip);
            machine.defaultState = idleState;
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(machine);
            var dialogue = Greeting();
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                var traveler = all.Single(t => t.name == "Traveler").GetComponent<SpriteRenderer>();
                var floor = all.Single(t => t.name == "Floor").GetComponent<Collider2D>();
                var root = scene.GetRootGameObjects().SingleOrDefault(r => r.name == ObjectName);
                bool created = root == null;
                if (created)
                {
                    root = new GameObject(ObjectName);
                    SceneManager.MoveGameObjectToScene(root, scene);
                    Undo.RegisterCreatedObjectUndo(root, "Add TempleGate blacksmith");
                    root.transform.position = new Vector3(17f, floor.bounds.max.y + .015f, 0f);
                }
                var renderer = GetOrAdd<SpriteRenderer>(root);
                renderer.sharedMaterial = ForgeToneMaterial();
                renderer.sortingLayerID = traveler.sortingLayerID;
                renderer.sortingOrder = traveler.sortingOrder;
                renderer.sprite = idle[0];
                var animator = GetOrAdd<Animator>(root);
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var trigger = GetOrAdd<CapsuleCollider2D>(root);
                trigger.isTrigger = true;
                trigger.size = new Vector2(1.6f, 2.5f);
                trigger.offset = new Vector2(.15f, 1.25f);
                var smith = GetOrAdd<EquipmentUpgradeBlacksmith>(root);
                var smithData = new SerializedObject(smith);
                smithData.FindProperty("greetingDialogue").objectReferenceValue = dialogue;
                smithData.FindProperty("upgradeWindowPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/EquipmentUpgradeWindow.prefab");
                smithData.FindProperty("dialogueUiPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/SkullMonkDialogueUI.prefab");
                smithData.ApplyModifiedPropertiesWithoutUndo();
                var ambient = GetOrAdd<BlacksmithAmbientAnimation>(root);
                var ambientData = new SerializedObject(ambient);
                ambientData.FindProperty("hammerInterval").vector2Value = new Vector2(6f, 12f);
                ambientData.FindProperty("hammerDuration").floatValue = hammerClip.length;
                ambientData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(root);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save TempleGate.");
                AssetDatabase.SaveAssets();
                Selection.activeGameObject = root;
                return "TempleGate/GoblinBlacksmith: scene object, 8 idle + 8 hammer frames, 6-12 second intervals, T interaction and existing equipment-upgrade UI.";
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        [MenuItem("Eastern Fantasy/Old Temple/Apply Blacksmith Forge Tone")]
        public static string ApplyForgeTone()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop play mode before editing the blacksmith.");
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("Save TempleGate before editing the blacksmith.");
                var blacksmith = scene.GetRootGameObjects().Single(root => root.name == ObjectName);
                var renderer = blacksmith.GetComponent<SpriteRenderer>();
                renderer.sharedMaterial = ForgeToneMaterial();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save TempleGate.");
                AssetDatabase.SaveAssets();
                return "Applied forge-side warmth and muted shadow reds to the TempleGate blacksmith.";
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        private static Material ForgeToneMaterial()
        {
            const string path = AnimationFolder + "/GoblinBlacksmithForgeTone.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Eastern Fantasy/Village Harmony");
            if (shader == null) throw new InvalidOperationException("Missing sprite color grading shader.");
            material = new Material(shader);
            material.SetFloat("_ForgeShade", 1f);
            material.EnableKeyword("_FORGE_SHADE");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static T GetOrAdd<T>(GameObject root) where T : Component
        {
            var component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }

        private static Sprite[] Slice(string path)
        {
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(raw, File.ReadAllBytes(path))) throw new InvalidOperationException("Cannot load " + path);
                var pixels = raw.GetPixels32();
                int[] bottoms = new int[8];
                for (int i = 0; i < 8; i++)
                {
                    int left = Mathf.RoundToInt(i * raw.width / 8f);
                    int right = Mathf.RoundToInt((i + 1) * raw.width / 8f);
                    int bottom = raw.height;
                    // The stationary right foot provides one common baseline for the complete sheet.
                    for (int x = left + (right - left) * 3 / 5; x < right; x++)
                        for (int y = 0; y < raw.height / 2; y++)
                            if (pixels[y * raw.width + x].a >= 96) { bottom = Mathf.Min(bottom, y); break; }
                    if (bottom == raw.height) throw new InvalidOperationException("Missing foot in frame " + i);
                    bottoms[i] = bottom;
                }
                Array.Sort(bottoms);
                float baseline = bottoms[4] + 1f;
                var cells = Enumerable.Range(0, 8).Select(i =>
                {
                    int left = Mathf.RoundToInt(i * raw.width / 8f), right = Mathf.RoundToInt((i + 1) * raw.width / 8f);
                    return new Rect(left, 0, right - left, raw.height);
                }).ToArray();
                SkullMonkMeleeSetup.Slice(path, cells, PixelsPerUnit, cells.Select(r => new Vector2(.5f, baseline / raw.height)).ToArray());
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.filterMode = FilterMode.Bilinear; importer.npotScale = TextureImporterNPOTScale.None;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
            var frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (frames.Length != 8) throw new InvalidOperationException("Expected eight frames: " + path);
            return frames;
        }

        private static AnimationClip MakeClip(string name, Sprite[] frames, float duration, bool loop)
        {
            string path = AnimationFolder + "/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.name = name; clip.frameRate = 12f;
            var keys = frames.Select((s, i) => new ObjectReferenceKeyframe { time = i * duration / frames.Length, value = s }).ToList();
            keys.Add(new ObjectReferenceKeyframe { time = duration, value = loop ? frames[0] : frames[frames.Length - 1] });
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop;
            settings.startTime = 0f; settings.stopTime = duration; AnimationUtility.SetAnimationClipSettings(clip, settings);
            if (name == BlacksmithAmbientAnimation.HammerState)
                AnimationUtility.SetAnimationEvents(clip, new[] {
                    new AnimationEvent { time = duration * 5f / 8f, functionName = "PlayHammerImpact" }
                });
            EditorUtility.SetDirty(clip); return clip;
        }

        private static AnimatorState State(AnimatorStateMachine machine, string name, AnimationClip clip)
        {
            var state = machine.states.FirstOrDefault(s => s.state.name == name).state;
            if (state == null) state = machine.AddState(name);
            state.motion = clip; EditorUtility.SetDirty(state); return state;
        }

        private static DialogueSequence Greeting()
        {
            var portraitPath = Art + "도깨비 대장장이 일러스트.png";
            AssetDatabase.ImportAsset(portraitPath);
            var portrait = AssetDatabase.LoadAllAssetsAtPath(portraitPath).OfType<Sprite>().Single();
            var character = Asset<DialogueCharacter>(DataFolder + "/GoblinBlacksmithCharacter.asset");
            var data = new SerializedObject(character);
            data.FindProperty("displayName").stringValue = "도깨비 대장장이";
            data.FindProperty("portrait").objectReferenceValue = portrait;
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(character);
            var dialogue = Asset<DialogueSequence>(DataFolder + "/GoblinBlacksmithGreeting.asset");
            data = new SerializedObject(dialogue);
            data.FindProperty("partnerCharacter").objectReferenceValue = character;
            data.FindProperty("playerCharacter").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DialogueCharacter>("Assets/_Project/Data/Tutorial_DIalogue/DialoguePlayer.asset");
            var lines = data.FindProperty("lines"); lines.arraySize = 1;
            lines.GetArrayElementAtIndex(0).FindPropertyRelative("speaker").enumValueIndex = (int)DialogueSpeakerSide.Partner;
            lines.GetArrayElementAtIndex(0).FindPropertyRelative("text").stringValue = "장비를 더 단단하게 벼리고 싶나? 승급할 장비와 재료, 엽전을 가져오게.";
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(dialogue);
            return dialogue;
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
