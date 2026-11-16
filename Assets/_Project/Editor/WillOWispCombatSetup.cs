using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class WillOWispCombatSetup
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Enemy/will-o'-the-wisp.prefab";
        public const string ProjectilePath = "Assets/_Project/Prefabs/Enemy/WillOWispProjectile.prefab";
        public const string AnimationFolder = "Assets/_Project/Animations/Old Temple/will-o'-the-wisp/";
        private const string ArtFolder = "Assets/_Project/Art/Old Temple/Monster/will-o'-the-wisp/";
        private static readonly EditorCurveBinding SpriteBinding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        [MenuItem("Eastern Fantasy/Monsters/Setup Will O Wisp Combat")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            RepairMoveSlices();
            ConfigureAnimations();
            ConfigureController();
            var projectile = CreateProjectile();
            // Preserve the component's local file ID so scene references and inherited settings survive.
            string text = File.ReadAllText(PrefabPath);
            string oldGuid = AssetDatabase.AssetPathToGUID("Assets/_Project/Scripts/MON_Ginseng.cs");
            string newGuid = AssetDatabase.AssetPathToGUID("Assets/_Project/Scripts/MON_WillOWisp.cs");
            text = text.Replace("guid: " + oldGuid, "guid: " + newGuid)
                .Replace("Assembly-CSharp::MON_Ginseng", "Assembly-CSharp::MON_WillOWisp");
            File.WriteAllText(PrefabPath, text);
            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                root.transform.localScale = Vector3.one * .5f;
                var monster = root.GetComponent<MON_WillOWisp>();
                var data = new SerializedObject(monster);
                data.FindProperty("monsterDisplayName").stringValue = "도깨비불";
                data.FindProperty("projectilePrefab").objectReferenceValue = projectile;
                data.FindProperty("attackRange").floatValue = 6f;
                data.FindProperty("attackDuration").floatValue = Clip("Attack").length;
                data.FindProperty("deathDuration").floatValue = Clip("Dead").length + .2f;
                data.FindProperty("_detectRange").floatValue = 8f;
                data.FindProperty("detectionHalfHeight").floatValue = 3.5f;
                data.FindProperty("calmRoamRadius").vector2Value = new Vector2(3.5f, .15f);
                data.FindProperty("blinkPause").vector2Value = new Vector2(1.5f, 3f);
                data.FindProperty("idleHoverSpeed").floatValue = .25f;
                data.ApplyModifiedPropertiesWithoutUndo();
                var body = root.GetComponent<Rigidbody2D>();
                body.gravityScale = 0f;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                var collider = root.GetComponent<CapsuleCollider2D>();
                collider.offset = new Vector2(0f, -.35f);
                collider.size = new Vector2(1.7f, 1.7f);
                collider.excludeLayers |= LayerMask.GetMask("Player");
                var animator = root.GetComponent<Animator>();
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimationFolder + "will-o'-the-wisp.controller");
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                root.GetComponent<SpriteRenderer>().sprite = Sprites("도깨비불 대기")[0];
                var anchor = root.transform.Find("DamageTextAnchor");
                if (anchor != null) anchor.localPosition = new Vector3(0f, 1.6f, 0f);
                var loot = root.GetComponent<EasternFantasy.Inventory.EnemyLootDrop>();
                if (loot != null)
                {
                    var lootData = new SerializedObject(loot);
                    lootData.FindProperty("drops").arraySize = 0; // Remove the copied ginseng-stem drop.
                    lootData.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }

        private static AnimationClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationFolder + "Will-o " + name + ".anim");
        private static Sprite[] Sprites(string name) => AssetDatabase.LoadAllAssetsAtPath(ArtFolder + name + ".png")
            .OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();

        private static void ConfigureAnimations()
        {
            string[] names = { "Idle", "Move", "Attack", "Dead" };
            string[] sheets = { "도깨비불 대기", "도깨비 불 이동", "도깨비 불 공격", "도깨비 불 사망" };
            for (int i = 0; i < names.Length; i++)
            {
                var clip = Clip(names[i]);
                var sprites = Sprites(sheets[i]);
                if (sprites.Length != 8) throw new InvalidOperationException("Expected eight " + names[i] + " sprites.");
                clip.frameRate = 6f;
                AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, sprites.Select((s, frame) =>
                    new ObjectReferenceKeyframe { time = frame / 6f, value = s }).ToArray());
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.startTime = 0f;
                settings.stopTime = sprites.Length / 6f;
                settings.loopTime = i < 2;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                if (names[i] == "Attack")
                    AnimationUtility.SetAnimationEvents(clip, new[]
                    {
                        new AnimationEvent { time = 4f / 6f, functionName = "EmitWispVolley" },
                        new AnimationEvent { time = settings.stopTime - .01f, functionName = "FinishWispAttack" }
                    });
                EditorUtility.SetDirty(clip);
            }
        }

        private static void ConfigureController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimationFolder + "will-o'-the-wisp.controller");
            controller.parameters = new[]
            {
                new AnimatorControllerParameter { name = "isMoving", type = AnimatorControllerParameterType.Bool },
                new AnimatorControllerParameter { name = "isAttacking", type = AnimatorControllerParameterType.Bool },
                new AnimatorControllerParameter { name = "isDead", type = AnimatorControllerParameterType.Trigger }
            };
            var machine = controller.layers[0].stateMachine;
            var idle = machine.states.First(s => s.state.name == "Will-o Idle").state;
            var move = machine.states.First(s => s.state.name == "Will-o Move").state;
            var attack = machine.states.First(s => s.state.name == "Will-o Attack").state;
            var dead = machine.states.First(s => s.state.name == "Will-o Dead").state;
            foreach (var child in machine.states)
                foreach (var transition in child.state.transitions) child.state.RemoveTransition(transition);
            foreach (var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
            machine.defaultState = idle;
            Transition(idle, move, "isMoving", true).AddCondition(AnimatorConditionMode.IfNot, 0f, "isAttacking");
            Transition(move, idle, "isMoving", false).AddCondition(AnimatorConditionMode.IfNot, 0f, "isAttacking");
            Transition(idle, attack, "isAttacking", true);
            Transition(move, attack, "isAttacking", true);
            Transition(attack, idle, "isAttacking", false);
            var death = machine.AddAnyStateTransition(dead);
            death.hasExitTime = false;
            death.duration = 0f;
            death.canTransitionToSelf = false;
            death.AddCondition(AnimatorConditionMode.If, 0f, "isDead");
            EditorUtility.SetDirty(machine);
            EditorUtility.SetDirty(controller);
        }

        private static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to, string parameter, bool value)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
            return transition;
        }

        private static WillOWispProjectile CreateProjectile()
        {
            var frames = Sprites("도깨비 불 투사체");
            if (frames.Length != 7) throw new InvalidOperationException("Expected seven fireball sprites.");
            var root = new GameObject("WillOWispProjectile", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D));
            try
            {
                root.layer = LayerMask.NameToLayer("Ignore Raycast");
                root.transform.localScale = Vector3.one * .28f;
                root.GetComponent<SpriteRenderer>().sprite = frames[0];
                root.GetComponent<SpriteRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<SpriteRenderer>().sharedMaterial;
                var body = root.GetComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                var collider = root.GetComponent<CircleCollider2D>();
                collider.radius = .8f;
                collider.isTrigger = true;
                var data = new SerializedObject(root.AddComponent<WillOWispProjectile>());
                var flight = data.FindProperty("flightFrames");
                flight.arraySize = 3;
                for (int i = 0; i < 3; i++) flight.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                var impact = data.FindProperty("impactFrames");
                impact.arraySize = 4;
                for (int i = 0; i < 4; i++) impact.GetArrayElementAtIndex(i).objectReferenceValue = frames[i + 3];
                data.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(root, ProjectilePath).GetComponent<WillOWispProjectile>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void RepairMoveSlices()
        {
            string path = ArtFolder + "도깨비 불 이동.png";
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                ImageConversion.LoadImage(raw, File.ReadAllBytes(path));
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var factories = new SpriteDataProviderFactories(); factories.Init();
                var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
                var previous = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
                var pixels = raw.GetPixels32();
                var rects = new SpriteRect[8];
                var pairs = new SpriteNameFileIdPair[8];
                float ppu = 0f;
                for (int i = 0; i < 8; i++)
                {
                    int left = i * raw.width / 8, right = (i + 1) * raw.width / 8;
                    int minX = right, maxX = left, minY = raw.height, maxY = 0;
                    for (int y = 0; y < raw.height; y++) for (int x = left; x < right; x++)
                    {
                        if (pixels[y * raw.width + x].a < 128) continue;
                        minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                    }
                    if (minY >= maxY) throw new InvalidOperationException("Empty wisp movement cell " + i);
                    if (i == 0) ppu = (maxY - minY + 1) / 2.75f;
                    string name = "도깨비 불 이동_" + i;
                    var id = previous.TryGetValue(name, out var old) ? old : GUID.Generate();
                    rects[i] = new SpriteRect { name = name, spriteID = id, rect = new Rect(left, 0, right - left, raw.height),
                        alignment = SpriteAlignment.Custom, pivot = new Vector2(((minX + maxX + 1) * .5f - left) / (right - left),
                            (minY + maxY + 1) * .5f / raw.height) };
                    pairs[i] = new SpriteNameFileIdPair(name, id);
                }
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = ppu;
                importer.maxTextureSize = 4096;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                provider.SetSpriteRects(rects);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
                provider.Apply(); importer.SaveAndReimport();
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
        }
    }
}
