using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class SkullMonkMeleeSetup
    {
        const string Art = "Assets/_Project/Art/Old Temple/Monster/SkullMonk/";
        const string Anim = "Assets/_Project/Animations/Old Temple/SkullMonk/";
        const string ScenePath = "Assets/_Project/Scenes/OldTemple/OldTemple - Center.unity";

        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var actor = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "Skull").gameObject;
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "SkullMonkIdle.anim");
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var reference = (Sprite)AnimationUtility.GetObjectReferenceCurve(idle, binding)[0].value;
            float ppu = 340f / reference.bounds.size.y;
            // Supplied variant: four columns, two rows, read across the top row first.
            // The rear heel remains on the idle rear heel; the front foot supplies all forward reach.
            var rects = Enumerable.Range(0, 8).Select(i => new Rect((i % 4) * 543, i < 4 ? 362 : 0, 543, 362)).ToArray();
            float[] rearHeelX = { 173, 139, 104, 82, 78, 38, 21, 8 };
            float[] rearSoleY = { 16, 16, 16, 16, 20, 20, 20, 19 };
            const float idleRearHeelX = -.68f;
            Slice(Art + "SkullMonkMelee_StepForward.png", rects, ppu,
                rects.Select((r, i) => new Vector2((rearHeelX[i] - idleRearHeelX * ppu) / r.width,
                    (rearSoleY[i] - reference.bounds.min.y * ppu) / r.height)).ToArray(), true);
            string fx = Art + "SkullMonkMelee_Effect.png";
            if (!File.Exists(fx)) File.Copy(Art + "해골 승려 근접공격 이펙트.png", fx);
            AssetDatabase.ImportAsset(fx);
            // The source importer auto-sliced particles into over 100 fragments. Use its four actual poses.
            int[] cuts = { 0, 493, 1013, 1681, 2172 };
            var fxRects = Enumerable.Range(0, 4).Select(i => new Rect(cuts[i], 125, cuts[i + 1] - cuts[i], 465)).ToArray();
            Slice(fx, fxRects, 160f, Enumerable.Repeat(new Vector2(.08f, .5f), 4).ToArray());
            var sprites = Frames(Art + "SkullMonkMelee_StepForward.png");
            // Load and hold the raised-foot pose, then land + extend within 60 ms.
            // Recovery uses the already-retracted poses, never rewinds the lunge/raised foot.
            int[] sequence = { 0, 1, 2, 3, 4, 5, 6, 7, 2, 1, 0 };
            float[] times = { 0f, .10f, .20f, .26f, .43f, .45f, .47f, .49f, .72f, .86f, .98f };
            var attack = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "SkullMonkAttack.anim");
            if (attack == null) { attack = new AnimationClip(); AssetDatabase.CreateAsset(attack, Anim + "SkullMonkAttack.anim"); }
            attack.frameRate = 60f;
            AnimationUtility.SetObjectReferenceCurve(attack, binding, sequence.Select((n, i) => new ObjectReferenceKeyframe { time = times[i], value = sprites[n] }).ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(attack);
            settings.startTime = 0; settings.stopTime = 1.10f; settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(attack, settings);
            AnimationUtility.SetAnimationEvents(attack, new[] {
                new AnimationEvent { time = .43f, functionName = "SkullMonkPlantStep" },
                new AnimationEvent { time = .49f, functionName = "SkullMonkThrust" },
                new AnimationEvent { time = 1.09f, functionName = "SkullMonkAttackFinished" }
            });
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim + "SkullMonk.controller");
            if (!controller.parameters.Any(p => p.name == "isMoving")) controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
            if (!controller.parameters.Any(p => p.name == "isDead")) controller.AddParameter("isDead", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var idleState = machine.states.Single(s => s.state.name == "SkullMonkIdle").state;
            var walk = machine.states.Single(s => s.state.name == "SkullMonkWalk").state;
            var attackState = machine.states.FirstOrDefault(s => s.state.name == "SkullMonkAttack").state;
            if (attackState == null) attackState = machine.AddState("SkullMonkAttack", new Vector3(480, 200));
            attackState.motion = attack;
            machine.defaultState = idleState;
            Link(idleState, walk, true); Link(walk, idleState, false);
            var body = actor.GetComponent<Rigidbody2D>(); if (body == null) body = actor.AddComponent<Rigidbody2D>();
            body.gravityScale = 3; body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = actor.GetComponent<CapsuleCollider2D>(); if (collider == null) collider = actor.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(1.5f, reference.bounds.size.y - .1f);
            collider.offset = new Vector2(0, reference.bounds.center.y);
            actor.layer = LayerMask.NameToLayer("Enemy");
            var boss = actor.GetComponent<MON_SkullMonk>(); if (boss == null) boss = actor.AddComponent<MON_SkullMonk>();
            var data = new SerializedObject(boss);
            data.FindProperty("thrustOrigin").vector2Value = new Vector2(2.7f, .45f);
            data.FindProperty("thrustTravel").floatValue = .95f;
            // Preserve the original upper edge (+1.35); extend only the bottom to -3.05.
            data.FindProperty("hitOffset").vector2Value = new Vector2(3.5f, -.85f);
            data.FindProperty("hitSize").vector2Value = new Vector2(5.2f, 4.4f);
            data.FindProperty("attackDownwardRange").floatValue = 3.5f;
            data.FindProperty("monsterDisplayName").stringValue = "해골 승려";
            data.FindProperty("encounterId").stringValue = "Chapter1.SkullMonk";
            data.FindProperty("_maxHP").floatValue = 3000;
            data.FindProperty("_attackPower").floatValue = 30;
            data.FindProperty("_moveSpeed").floatValue = 1.5f;
            data.FindProperty("_detectRange").floatValue = 14;
            data.FindProperty("_playerLayer").intValue = LayerMask.GetMask("Player");
            data.FindProperty("horizontalKnockbackPower").floatValue = 0;
            data.FindProperty("verticalKnockbackPower").floatValue = 0;
            var frames = Frames(fx); var field = data.FindProperty("thrustEffectFrames"); field.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) field.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            actor.GetComponent<SpriteRenderer>().sprite = reference;
            actor.GetComponent<SpriteRenderer>().flipX = true;
            EditorUtility.SetDirty(attack); EditorUtility.SetDirty(controller); EditorUtility.SetDirty(machine);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = actor;
            Debug.Log("SkullMonk impact melee: 60ms drive, impact at 0.49s, planted heel, held extension, retracted recovery.");
        }
        static Sprite[] Frames(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        static void Link(AnimatorState from, AnimatorState to, bool moving)
        {
            if (from.transitions.Any(t => t.destinationState == to)) return;
            var t = from.AddTransition(to); t.hasExitTime = false; t.duration = 0;
            t.AddCondition(moving ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "isMoving");
        }
        internal static void Slice(string path, Rect[] cells, float ppu, Vector2[] pivots, bool isolate = false)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu; importer.maxTextureSize = 4096;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            var factory = new SpriteDataProviderFactories(); factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var old = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var rects = cells.Select((r, i) => new SpriteRect { name = "Frame_" + i.ToString("D2"), rect = r,
                spriteID = old.TryGetValue("Frame_" + i.ToString("D2"), out var id) ? id : GUID.Generate(),
                alignment = SpriteAlignment.Custom, pivot = pivots[i] }).ToArray();
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            if (isolate)
            {
                var raw = new Texture2D(2, 2); ImageConversion.LoadImage(raw, File.ReadAllBytes(path));
                try
                {
                    var generate = typeof(UnityEditor.Sprites.SpriteUtility).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                        .Single(m => m.Name == "GenerateOutline" && m.GetParameters().Length == 7);
                    var outlines = provider.GetDataProvider<ISpriteOutlineDataProvider>();
                    foreach (var rect in rects)
                    {
                        object[] args = { raw, rect.rect, .75f, (byte)80, false, null, false };
                        generate.Invoke(null, args);
                        var primary = ((Vector2[][])args[5]).OrderByDescending(Area).First();
                        outlines.SetOutlines(rect.spriteID, new System.Collections.Generic.List<Vector2[]> { primary });
                    }
                    var textureSettings = new TextureImporterSettings(); importer.ReadTextureSettings(textureSettings);
                    textureSettings.spriteMeshType = SpriteMeshType.Tight; importer.SetTextureSettings(textureSettings);
                }
                finally { UnityEngine.Object.DestroyImmediate(raw); }
            }
            provider.Apply(); importer.SaveAndReimport();
        }
        static float Area(Vector2[] polygon)
        {
            float sum = 0;
            for (int i = 0; i < polygon.Length; i++) { var a = polygon[i]; var b = polygon[(i + 1) % polygon.Length]; sum += a.x * b.y - b.x * a.y; }
            return Mathf.Abs(sum);
        }
    }
}
