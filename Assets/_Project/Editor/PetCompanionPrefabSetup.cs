using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.Skill;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class PetCompanionPrefabSetup
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Companions/GoblinCompanion.prefab";
        public const string CatalogPath = "Assets/_Project/Resources/PetCompanionCatalog.asset";
        public const string AnimationFolder = "Assets/_Project/Animations/Companions/Goblin/";
        private const float GiantSizeMultiplier = 1.3f;
        private const string Art = "Assets/_Project/Art/TutorialMonster/Goblin/";

        [MenuItem("Eastern Fantasy/Companions/Setup Goblin Companion Prefab")]
        public static void Run()
        {
            sheetPixels.Clear();
            Directory.CreateDirectory(AnimationFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            AssetDatabase.Refresh();
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Goblin/Goblin_Idle.anim");
            var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Goblin/Goblin_Walk.anim");
            var idleKeys = SpriteKeys(idle);
            var walkKeys = SpriteKeys(walk);
            var a = Sprites("도깨비 공격 2-1468.png");
            var b = Sprites("도깨비 공격 2-257.png");
            if (a.Length != 4 || b.Length != 3) throw new InvalidOperationException("Expected four and three visible attack sprites.");
            Sprite[] attack = { a[0], b[0], a[1], b[1], a[2], b[2], a[3] };
            float[] times = { 0f, .1f, .3f, .4f, .5f, .6f, .7f };
            var attackKeys = attack.Select((sprite, i) => new ObjectReferenceKeyframe { time = times[i], value = sprite }).ToArray();
            float height = VisibleBounds((Sprite)idleKeys[0].value).height / ((Sprite)idleKeys[0].value).pixelsPerUnit;
            var idleClip = BuildClip("Companion_Idle", idleKeys, idle.frameRate, 1.2f, true, height, false);
            var walkClip = BuildClip("Companion_Walk", walkKeys, walk.frameRate, 1f, true, height, false);
            var attackClip = BuildClip("Companion_Attack", attackKeys, 10f, .8f, false, height, true);
            var grow = Interleave("도깨비 거대화 애니메이션135.png", "도깨비 거대화 애니메이션246.png");
            var strike = Interleave("도깨비 스킬 시전135.png", "도깨비 스킬 시전246.png");
            float growScale = height * grow[0].pixelsPerUnit / VisibleBounds(grow[0]).height;
            float giantHeight = VisibleBounds(grow[5]).height / grow[5].pixelsPerUnit * growScale * GiantSizeMultiplier;
            var growClip = BuildSkillClip("Companion_Grow", grow, height, growScale, true, GiantSizeMultiplier);
            var strikeClip = BuildSkillClip("Companion_SkillStrike", strike, height,
                giantHeight * strike[0].pixelsPerUnit / VisibleBounds(strike[0]).height, false);
            var shrinkClip = BuildSkillClip("Companion_Shrink", grow.Reverse().ToArray(), height, growScale, false, GiantSizeMultiplier, true);
            var skill = BuildSkillDefinition();
            string controllerPath = AnimationFolder + "GoblinCompanion.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states) machine.RemoveState(child.state);
            foreach (var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            foreach (var clip in new[] { idleClip, walkClip, attackClip, growClip, strikeClip, shrinkClip })
            {
                var state = machine.AddState(clip.name);
                state.motion = clip;
                state.writeDefaultValues = false;
                if (clip == idleClip) machine.defaultState = state;
            }
            var root = new GameObject("GoblinCompanion");
            try
            {
                root.transform.localScale = Vector3.one / 1.5f;
                var visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                var renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = (Sprite)idleKeys[0].value;
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("a97c105638bdf8b4a8650670310a4cd3"));
                renderer.sortingOrder = 5;
                var animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                var actor = root.AddComponent<PetCompanionActor>();
                var data = new SerializedObject(actor);
                data.FindProperty("activeSkill").objectReferenceValue = skill;
                data.FindProperty("visual").objectReferenceValue = renderer;
                data.FindProperty("animator").objectReferenceValue = animator;
                data.FindProperty("summonItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/SoulOfGoblin.asset");
                data.FindProperty("referenceVisualHeight").floatValue = height;
                var frames = data.FindProperty("previewFrames");
                frames.arraySize = idleKeys.Length;
                for (int i = 0; i < idleKeys.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = idleKeys[i].value;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            var catalog = AssetDatabase.LoadAssetAtPath<PetCompanionCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PetCompanionCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var settings = new SerializedObject(catalog);
            var prefabs = settings.FindProperty("companionPrefabs");
            var goblin = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<PetCompanionActor>();
            bool registered = false;
            for (int i = 0; i < prefabs.arraySize; i++) registered |= prefabs.GetArrayElementAtIndex(i).objectReferenceValue == goblin;
            if (!registered)
            {
                int index = prefabs.arraySize;
                prefabs.arraySize++;
                prefabs.GetArrayElementAtIndex(index).objectReferenceValue = goblin;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("Goblin companion prefab and normalized idle/walk/attack clips are ready; existing catalog entries preserved.");
        }

        public const string SkillPath = "Assets/_Project/Data/Skills/GoblinGiantStrike.asset";
        private static SkillDefinition BuildSkillDefinition()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SkillPath));
            AssetDatabase.Refresh();
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(SkillPath);
            if (skill == null) { skill = ScriptableObject.CreateInstance<SkillDefinition>(); AssetDatabase.CreateAsset(skill, SkillPath); }
            var data = new SerializedObject(skill);
            data.FindProperty("skillId").stringValue = "goblin_giant_strike";
            data.FindProperty("companionId").stringValue = "goblin";
            data.FindProperty("displayName").stringValue = "도깨비 거대 방망이";
            data.FindProperty("shortDescription").stringValue = "전투 대상에게 도깨비 공격력의 500% 피해";
            data.FindProperty("detailedDescription").stringValue = "장착한 도깨비가 전투 중인 적에게 다가가 거대화한 뒤 방망이를 휘두릅니다. 타격 순간 도깨비 공격력의 500% 피해를 한 번 입히고 원래 크기로 돌아옵니다.\n재사용 대기: 15초 · 자원 소모 없음";
            data.FindProperty("activationType").enumValueIndex = (int)SkillActivationType.Active;
            data.FindProperty("maximumLevel").intValue = 1;
            data.FindProperty("cooldownSeconds").floatValue = 15f;
            data.FindProperty("resourceCost").enumValueIndex = (int)SkillResourceCost.None;
            data.FindProperty("icon").objectReferenceValue = Sprites("도깨비 스킬 아이콘.png").Single(sprite => sprite.name.EndsWith("_1"));
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(skill);
            return skill;
        }
        private static Sprite[] Interleave(string odd, string even)
        {
            var a = Sprites(odd); var b = Sprites(even);
            if (a.Length != 3 || b.Length != 3) throw new InvalidOperationException("Skill sheets must contain three odd/even frames each.");
            return new[] { a[0], b[0], a[1], b[1], a[2], b[2] };
        }
        private static AnimationClip BuildSkillClip(string name, Sprite[] sprites, float normalHeight, float factor, bool tremble, float enlargement = 1f, bool shrinking = false)
        {
            string path = AnimationFolder + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.ClearCurves(); clip.name = name; clip.frameRate = 1f / .12f;
            var keys = new List<ObjectReferenceKeyframe>();
            var x = new AnimationCurve(); var y = new AnimationCurve(); var rotation = new AnimationCurve();
            var scale = new AnimationCurve();
            for (int i = 0; i <= sprites.Length; i++)
            {
                int frame = Mathf.Min(i, sprites.Length - 1);
                var sprite = sprites[frame];
                float growth = (float)frame / (sprites.Length - 1);
                if (shrinking) growth = 1f - growth;
                float frameFactor = factor * Mathf.Lerp(1f, enlargement, Mathf.SmoothStep(0f, 1f, growth));
                var bounds = VisibleBounds(sprite);
                float time = i * .12f;
                scale.AddKey(time, frameFactor);
                keys.Add(new ObjectReferenceKeyframe { time = time, value = sprite });
                float footX = FootCenter(sprite, bounds);
                x.AddKey(time, -(footX - sprite.pivot.x) / sprite.pixelsPerUnit * frameFactor);
                y.AddKey(time, -normalHeight * .5f - (bounds.y - sprite.pivot.y) / sprite.pixelsPerUnit * frameFactor);
                rotation.AddKey(time, tremble && i > 0 && i < sprites.Length - 1 ? (i % 2 == 0 ? -.8f : .8f) : 0f);
            }
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
            foreach (string axis in new[] { "x", "y", "z" })
            {
                for (int i = 0; i < scale.length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(scale, i, AnimationUtility.TangentMode.Constant);
                    AnimationUtility.SetKeyRightTangentMode(scale, i, AnimationUtility.TangentMode.Constant);
                }
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale." + axis), scale);
            }
            foreach (var pair in new[] { ("m_LocalPosition.x", x), ("m_LocalPosition.y", y), ("localEulerAnglesRaw.z", rotation) })
            {
                for (int i = 0; i < pair.Item2.length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(pair.Item2, i, AnimationUtility.TangentMode.Constant);
                    AnimationUtility.SetKeyRightTangentMode(pair.Item2, i, AnimationUtility.TangentMode.Constant);
                }
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), pair.Item1), pair.Item2);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings); EditorUtility.SetDirty(clip);
            return clip;
        }
        private static float FootCenter(Sprite sprite, Rect bounds)
        {
            var sheet = sheetPixels[AssetDatabase.GetAssetPath(sprite)];
            float rx = (float)sheet.width / sprite.texture.width, ry = (float)sheet.height / sprite.texture.height;
            int left = Mathf.FloorToInt((sprite.rect.x + bounds.x) * rx);
            int right = Mathf.Min(sheet.width, Mathf.CeilToInt((sprite.rect.x + bounds.xMax) * rx));
            int bottom = Mathf.Max(0, Mathf.FloorToInt((sprite.rect.y + bounds.y) * ry));
            int top = Mathf.Min(sheet.height, bottom + Mathf.Max(3, Mathf.RoundToInt(bounds.height * ry * .07f)));
            int min = right, max = left;
            for (int py = bottom; py < top; py++)
                for (int px = left; px < right; px++)
                {
                    var c = sheet.pixels[py * sheet.width + px];
                    // Ignore bright slash effects; use the grounded dark feet to anchor the body.
                    if (c.a < 32 || c.r > 175 || c.g > 135 || c.b > 135) continue;
                    min = Mathf.Min(min, px); max = Mathf.Max(max, px);
                }
            return max >= min ? (min + max + 1) * .5f / rx - sprite.rect.x : bounds.center.x;
        }

        private sealed class SheetPixels
        {
            public Color32[] pixels;
            public int width, height;
        }
        private static readonly Dictionary<string, SheetPixels> sheetPixels = new Dictionary<string, SheetPixels>();

        internal static Rect VisibleBounds(Sprite sprite)
        {
            string path = AssetDatabase.GetAssetPath(sprite);
            if (!sheetPixels.TryGetValue(path, out var sheet))
            {
                var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    raw.LoadImage(File.ReadAllBytes(path));
                    sheet = new SheetPixels { pixels = raw.GetPixels32(), width = raw.width, height = raw.height };
                    sheetPixels.Add(path, sheet);
                }
                finally { UnityEngine.Object.DestroyImmediate(raw); }
            }
            float ratioX = (float)sheet.width / sprite.texture.width;
            float ratioY = (float)sheet.height / sprite.texture.height;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(sprite.rect.xMin * ratioX));
            int x1 = Mathf.Min(sheet.width, Mathf.CeilToInt(sprite.rect.xMax * ratioX));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(sprite.rect.yMin * ratioY));
            int y1 = Mathf.Min(sheet.height, Mathf.CeilToInt(sprite.rect.yMax * ratioY));
            int minX = x1, minY = y1, maxX = x0 - 1, maxY = y0 - 1;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    if (sheet.pixels[y * sheet.width + x].a < 16) continue;
                    minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
                }
            if (maxX < minX || maxY < minY) throw new InvalidOperationException("Empty companion sprite: " + sprite.name);
            return new Rect(minX / ratioX - sprite.rect.x, minY / ratioY - sprite.rect.y,
                (maxX - minX + 1) / ratioX, (maxY - minY + 1) / ratioY);
        }

        private static Sprite[] Sprites(string name) => AssetDatabase.LoadAllAssetsAtPath(Art + name)
            .OfType<Sprite>().OrderBy(sprite => sprite.rect.x).ToArray();

        private static ObjectReferenceKeyframe[] SpriteKeys(AnimationClip clip)
        {
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).First(b => b.propertyName == "m_Sprite");
            return AnimationUtility.GetObjectReferenceCurve(clip, binding);
        }

        private static AnimationClip BuildClip(string name, ObjectReferenceKeyframe[] frames, float rate,
            float duration, bool loop, float referenceHeight, bool attack)
        {
            string path = AnimationFolder + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.ClearCurves();
            clip.name = name;
            clip.frameRate = rate;
            var sprites = frames.ToList();
            sprites.Add(new ObjectReferenceKeyframe { time = duration, value = loop ? frames[0].value : frames[frames.Length - 1].value });
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), sprites.ToArray());
            var referenceSprite = (Sprite)frames[0].value;
            Rect referenceBounds = VisibleBounds(referenceSprite);
            // Keep one body scale per motion: raised weapons and crouching must not resize the character.
            float factor = referenceHeight * referenceSprite.pixelsPerUnit / referenceBounds.height;
            float baseline = referenceSprite.rect.y + referenceBounds.y;
            foreach (string axis in new[] { "x", "y", "z" })
            {
                var scale = new AnimationCurve();
                var position = new AnimationCurve();
                foreach (var frame in sprites)
                {
                    var sprite = (Sprite)frame.value;
                    scale.AddKey(frame.time, factor);
                    // Keep idle/walk at one height; per-frame foot correction adds vertical jitter.
                    float y = attack
                        ? -referenceHeight * .5f + (sprite.rect.y + sprite.pivot.y - baseline) / sprite.pixelsPerUnit * factor
                        : -referenceHeight * .5f - (referenceBounds.y - referenceSprite.pivot.y) / referenceSprite.pixelsPerUnit * factor;
                    position.AddKey(frame.time, axis == "y" ? y : 0f);
                }
                for (int i = 0; i < scale.length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(scale, i, AnimationUtility.TangentMode.Constant);
                    AnimationUtility.SetKeyRightTangentMode(scale, i, AnimationUtility.TangentMode.Constant);
                    AnimationUtility.SetKeyLeftTangentMode(position, i, AnimationUtility.TangentMode.Constant);
                    AnimationUtility.SetKeyRightTangentMode(position, i, AnimationUtility.TangentMode.Constant);
                }
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale." + axis), scale);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition." + axis), position);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "localEulerAnglesRaw.z"), AnimationCurve.Constant(0f, duration, 0f));
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }
    }
}
