using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class HumanFacedBirdSetup
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Enemy/HumanFacedBird.prefab";
        public const string AnimationFolder = "Assets/_Project/Animations/Old Temple/Human-faced bird/";
        private const string ArtFolder = "Assets/_Project/Art/Old Temple/Monster/Human-faced bird/";
        // Match the original idle standing height before applying the requested 30% prefab enlargement.
        private const float ReferenceBodyHeight = 1.97f;
        private const float PrefabScale = 1.3f;
        private static readonly string[] Sheets = { "인면조 대기 1", "인면조 걷기", "인면조 공격", "인면조 사망" };
        private static readonly string[] Actions = { "Idle", "Walk", "Attack", "Dead" };

        [MenuItem("Eastern Fantasy/Monsters/Setup Human Faced Bird")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            Slice(Sheets[0], EvenCuts(8));
            Slice(Sheets[1], EvenCuts(8));
            // Unequal frame widths follow the supplied poses, including the spread wings.
            Slice(Sheets[2], new[] { 0, 248, 512, 784, 1080, 1413, 1677, 1931, 2172 });
            Slice(Sheets[3], new[] { 0, 269, 548, 859, 1200, 1505, 1842, 2172 });
            var clips = new AnimationClip[4];
            for (int i = 0; i < clips.Length; i++) clips[i] = MakeClip(Actions[i], Sheets[i], i < 2);
            var controller = MakeController(clips);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) MakePrefab(controller, clips[2].length);
            ApplyAppearance();
            AssetDatabase.SaveAssets();
        }

        private static int[] EvenCuts(int count) => Enumerable.Range(0, count + 1)
            .Select(i => Mathf.RoundToInt(i * 2172f / count)).ToArray();

        private static void Slice(string sheet, int[] cuts)
        {
            string path = ArtFolder + sheet + ".png";
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                ImageConversion.LoadImage(raw, File.ReadAllBytes(path));
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var factory = new SpriteDataProviderFactories(); factory.Init();
                var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
                provider.InitSpriteEditorDataProvider();
                var previous = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
                var rects = new SpriteRect[cuts.Length - 1];
                var pairs = new SpriteNameFileIdPair[rects.Length];
                var outlines = provider.GetDataProvider<ISpriteOutlineDataProvider>();
                float pixelsPerUnit = 200f;
                for (int i = 0; i < rects.Length; i++)
                {
                    int left = Mathf.RoundToInt(cuts[i] * raw.width / 2172f);
                    int right = Mathf.RoundToInt(cuts[i + 1] * raw.width / 2172f);
                    var rect = new Rect(left, 0, right - left, raw.height);
                    var outline = PrimaryOutline(raw, rect);
                    // Frame zero is the comparable upright pose in all four source sheets.
                    // Keep one scale per sheet so crouching, spread wings and death motion remain natural.
                    if (i == 0) pixelsPerUnit = (outline.Max(v => v.y) - outline.Min(v => v.y)) / ReferenceBodyHeight;
                    string name = sheet + "_" + i;
                    var id = previous.TryGetValue(name, out var existing) ? existing : GUID.Generate();
                    rects[i] = new SpriteRect { name = name, spriteID = id,
                        rect = rect, alignment = SpriteAlignment.Custom,
                        pivot = new Vector2(.5f + (outline.Min(v => v.x) + outline.Max(v => v.x)) * .5f / rect.width,
                            .5f + outline.Min(v => v.y) / rect.height) };
                    pairs[i] = new SpriteNameFileIdPair(name, id);
                    // Neighboring spread wings overlap these rectangular cells. Render only this bird's outline.
                    outlines.SetOutlines(id, new System.Collections.Generic.List<Vector2[]> { outline });
                    outlines.SetTessellationDetail(id, .75f);
                }
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.maxTextureSize = 4096;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.Tight; importer.SetTextureSettings(settings);
                provider.SetSpriteRects(rects);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
                provider.Apply(); importer.SaveAndReimport();
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
        }

        private static Vector2[] PrimaryOutline(Texture2D texture, Rect rect)
        {
            // Unity's sprite editor uses this outline generator internally; verify its signature before calling.
            var method = typeof(UnityEditor.Sprites.SpriteUtility).GetMethods(System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .First(m => m.Name == "GenerateOutline" && m.GetParameters().Length == 7);
            object[] arguments = { texture, rect, .75f, (byte)128, false, null, false };
            method.Invoke(null, arguments);
            return ((Vector2[][])arguments[5]).OrderByDescending(PolygonArea).First();
        }

        private static float PolygonArea(Vector2[] polygon)
        {
            float area = 0f;
            for (int i = 0; i < polygon.Length; i++)
            {
                var next = polygon[(i + 1) % polygon.Length];
                area += polygon[i].x * next.y - next.x * polygon[i].y;
            }
            return Mathf.Abs(area);
        }

        private static Sprite[] Frames(string sheet) => AssetDatabase.LoadAllAssetsAtPath(ArtFolder + sheet + ".png")
            .OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();

        private static AnimationClip MakeClip(string action, string sheet, bool loop)
        {
            string path = AnimationFolder + "HumanFacedBird" + action + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            var frames = Frames(sheet);
            clip.frameRate = 8f;
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
                frames.Select((sprite, i) => new ObjectReferenceKeyframe { time = i / 8f, value = sprite }).ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.startTime = 0f; settings.stopTime = frames.Length / 8f; settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            // Common enemy facing assumes left-facing art. Idle/death source art faces right.
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), "m_FlipX"),
                AnimationCurve.Constant(0f, settings.stopTime, action == "Idle" || action == "Dead" ? 1f : 0f));
            AnimationUtility.SetAnimationEvents(clip, action == "Attack" ? new[] {
                new AnimationEvent { time = 4f / 8f, functionName = "HumanFacedBirdClawHit" },
                new AnimationEvent { time = settings.stopTime - .01f, functionName = "HumanFacedBirdAttackFinished" }
            } : Array.Empty<AnimationEvent>());
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController MakeController(AnimationClip[] clips)
        {
            string path = AnimationFolder + "HumanFacedBird.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.parameters = new[] {
                new AnimatorControllerParameter { name = "isMoving", type = AnimatorControllerParameterType.Bool },
                new AnimatorControllerParameter { name = "isDead", type = AnimatorControllerParameterType.Trigger },
                new AnimatorControllerParameter { name = "walkCycleSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f }
            };
            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states) machine.RemoveState(child.state);
            foreach (var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
            var states = new AnimatorState[4];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = machine.AddState("HumanFacedBird" + Actions[i], new Vector3(240 + i * 220, 120));
                states[i].motion = clips[i];
            }
            machine.defaultState = states[0];
            states[1].speedParameter = "walkCycleSpeed"; states[1].speedParameterActive = true;
            Transition(states[0], states[1], true);
            Transition(states[1], states[0], false);
            var death = machine.AddAnyStateTransition(states[3]);
            death.hasExitTime = false; death.duration = 0f; death.canTransitionToSelf = false;
            death.AddCondition(AnimatorConditionMode.If, 0, "isDead");
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void Transition(AnimatorState from, AnimatorState to, bool moving)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false; transition.duration = 0f;
            transition.AddCondition(moving ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "isMoving");
        }

        private static void MakePrefab(AnimatorController controller, float attackDuration)
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/Enemy/Ginseng.prefab");
            try
            {
                root.name = "HumanFacedBird";
                var old = root.GetComponent<MON_Ginseng>();
                var source = new SerializedObject(old);
                var bird = root.AddComponent<MON_HumanFacedBird>();
                var data = new SerializedObject(bird);
                foreach (string property in new[] { "_maxHP", "_attackPower", "_moveSpeed", "detectionShape", "_detectRange",
                    "detectionHalfHeight", "patrolRange", "patrolMinWait", "patrolMaxWait", "patrolMinSpeedFactor", "_reward_EXP",
                    "_rewardYeopjeon", "currencyPickupPrefab", "_playerLayer", "hitFlashMaterial", "hitFlashDuration",
                    "horizontalKnockbackPower", "verticalKnockbackPower", "damageTextPrefab", "textSpawnPoint" })
                    data.CopyFromSerializedProperty(source.FindProperty(property));
                data.FindProperty("monsterDisplayName").stringValue = "인면조";
                data.FindProperty("attackDuration").floatValue = .7f;
                data.FindProperty("questDefinition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EasternFantasy.Quest.QuestDefinition>(
                    "Assets/_Project/Data/Quest/HumanFacedBirdHunt/HumanFacedBirdHunt.asset");
                data.ApplyModifiedPropertiesWithoutUndo();
                UnityEngine.Object.DestroyImmediate(old);
                var animator = root.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                root.GetComponent<SpriteRenderer>().sprite = Frames(Sheets[0])[0];
                var body = root.GetComponent<Rigidbody2D>();
                body.gravityScale = 1f; body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                var collider = root.GetComponent<CapsuleCollider2D>();
                collider.offset = new Vector2(0f, .88f); collider.size = new Vector2(.85f, 1.76f);
                collider.excludeLayers &= ~LayerMask.GetMask("Player");
                var anchor = bird.textSpawnPoint;
                if (anchor != null) anchor.localPosition = new Vector3(0f, 2.25f, 0f);
                var loot = root.GetComponent<EasternFantasy.Inventory.EnemyLootDrop>();
                if (loot != null)
                {
                    var lootData = new SerializedObject(loot);
                    lootData.FindProperty("spawnHeight").floatValue = .95f;
                    lootData.FindProperty("firstDropOffset").floatValue = .9f;
                    var feather = AssetDatabase.LoadAssetAtPath<EasternFantasy.Inventory.ItemDefinition>("Assets/_Project/Data/Items/HumanFacedBirdFeather.asset");
                    if (feather == null) throw new InvalidOperationException("Missing the human-faced bird's feather item.");
                    var drops = lootData.FindProperty("drops");
                    drops.arraySize = 1;
                    var drop = drops.GetArrayElementAtIndex(0);
                    drop.FindPropertyRelative("item").objectReferenceValue = feather;
                    drop.FindPropertyRelative("dropChance").floatValue = 1f;
                    drop.FindPropertyRelative("minimumQuantity").intValue = 1;
                    drop.FindPropertyRelative("maximumQuantity").intValue = 1;
                    lootData.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ApplyAppearance()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                root.transform.localScale = Vector3.one * PrefabScale;
                root.GetComponent<SpriteRenderer>().flipX = true;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
