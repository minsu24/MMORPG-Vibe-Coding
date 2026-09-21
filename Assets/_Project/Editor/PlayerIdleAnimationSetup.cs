using System;
using System.Linq;
using EasternFantasy.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class PlayerIdleAnimationSetup
    {
        private const string IdleSheetPath = "Assets/_Project/Art/Player/PlayerIdle8Frames.png";
        private const string WalkSheetPath = "Assets/_Project/Art/Player/PlayerWalk8Frames.png";
        private const string AttackSheetPath = "Assets/_Project/Art/Player/PlayerAttack.png";
        private const string AnimationFolder = "Assets/_Project/Animations/Player";
        private const string IdleClipPath = AnimationFolder + "/PlayerIdle.anim";
        private const string WalkClipPath = AnimationFolder + "/PlayerWalk.anim";
        private const string AttackClipPath = AnimationFolder + "/PlayerAttack.anim";
        private const string ControllerPath = AnimationFolder + "/Player.controller";
        private const string PrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        private const int FrameCount = 8;
        private const float IdlePixelsPerUnit = 256f;
        private const float WalkPixelsPerUnit = 240f;
        private const float IdleFramesPerSecond = 8f;
        private const float WalkFramesPerSecond = 10f;
        private const float AttackFramesPerSecond = 10f;
        private const float AttackPivotY = 0.60f;

        [InitializeOnLoadMethod]
        private static void ApplyWhenWalkAnimationIsMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkClipPath) != null) return;
            Apply();
        }

        [MenuItem("Eastern Fantasy/Apply Player Animations")]
        public static void Apply()
        {
            Sprite[] idleFrames = ConfigureSpriteSheet(IdleSheetPath, "PlayerIdle", IdlePixelsPerUnit);
            Sprite[] walkFrames = ConfigureSpriteSheet(WalkSheetPath, "PlayerWalk", WalkPixelsPerUnit);
            ConfigureSpritePivots(AttackSheetPath, AttackPivotY);
            Sprite[] attackFrames = LoadSpriteFrames(AttackSheetPath);

            EnsureFolder("Assets/_Project/Animations");
            EnsureFolder(AnimationFolder);
            AnimationClip idleClip = CreateOrReplaceClip(IdleClipPath, "PlayerIdle", idleFrames, IdleFramesPerSecond, true);
            AnimationClip walkClip = CreateOrReplaceClip(WalkClipPath, "PlayerWalk", walkFrames, WalkFramesPerSecond, true);
            AnimationClip attackClip = CreateOrReplaceClip(AttackClipPath, "PlayerAttack", attackFrames, AttackFramesPerSecond, false);
            AnimatorController controller = CreateOrReplaceController(idleClip, walkClip, attackClip);
            ApplyToPlayerPrefab(idleFrames[0], controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Applied {idleFrames.Length}-frame idle, {walkFrames.Length}-frame walk, and {attackFrames.Length}-frame attack animations to {PrefabPath}.");
        }

        private static Sprite[] LoadSpriteFrames(string sheetPath)
        {
            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(sheetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            if (frames.Length == 0)
                throw new InvalidOperationException($"No sprite frames found at {sheetPath}.");
            return frames;
        }

        private static void ConfigureSpritePivots(string sheetPath, float pivotY)
        {
            var importer = AssetImporter.GetAtPath(sheetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Sprite sheet not found at {sheetPath}.");

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            SpriteRect[] rects = provider.GetSpriteRects();
            for (int i = 0; i < rects.Length; i++)
            {
                rects[i].alignment = SpriteAlignment.Custom;
                rects[i].pivot = new Vector2(0.5f, pivotY);
            }

            provider.SetSpriteRects(rects);
            provider.Apply();
            AssetDatabase.ForceReserializeAssets(
                new[] { sheetPath }, ForceReserializeAssetsOptions.ReserializeMetadata);
            AssetDatabase.ImportAsset(
                sheetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        private static Sprite[] ConfigureSpriteSheet(string sheetPath, string framePrefix, float pixelsPerUnit)
        {
            var importer = AssetImporter.GetAtPath(sheetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Sprite sheet not found at {sheetPath}.");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.SaveAndReimport();

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var rects = new SpriteRect[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                int startX = Mathf.FloorToInt(i * texture.width / (float)FrameCount);
                int endX = Mathf.FloorToInt((i + 1) * texture.width / (float)FrameCount);
                rects[i] = new SpriteRect
                {
                    name = $"{framePrefix}_{i:D2}",
                    rect = new Rect(startX, 0, endX - startX, texture.height),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = GUID.Generate()
                };
            }

            provider.SetSpriteRects(rects);
            ISpriteNameFileIdDataProvider nameFileIdProvider =
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameFileIdProvider.SetNameFileIdPairs(
                rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            AssetDatabase.ForceReserializeAssets(
                new[] { sheetPath }, ForceReserializeAssetsOptions.ReserializeMetadata);
            AssetDatabase.ImportAsset(
                sheetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(sheetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            if (frames.Length != FrameCount)
                throw new InvalidOperationException($"Expected {FrameCount} frames at {sheetPath}, found {frames.Length}.");
            return frames;
        }

        private static AnimationClip CreateOrReplaceClip(
            string clipPath,
            string clipName,
            Sprite[] frames,
            float framesPerSecond,
            bool loopTime)
        {
            AssetDatabase.DeleteAsset(clipPath);
            var clip = new AnimationClip { name = clipName, frameRate = framesPerSecond };
            var binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            var keys = frames.Select((sprite, index) => new ObjectReferenceKeyframe
            {
                time = index / framesPerSecond,
                value = sprite
            }).ToArray();

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loopTime;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, clipPath);
            return clip;
        }

        private static AnimatorController CreateOrReplaceController(
            AnimationClip idleClip,
            AnimationClip walkClip,
            AnimationClip attackClip)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = stateMachine.AddState("PlayerIdle");
            idleState.motion = idleClip;
            AnimatorState walkState = stateMachine.AddState("PlayerWalk");
            walkState.motion = walkClip;
            AnimatorState attackState = stateMachine.AddState("PlayerAttack");
            attackState.motion = attackClip;
            stateMachine.defaultState = idleState;

            AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
            ConfigureTransition(toWalk, AnimatorConditionMode.If);
            AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
            ConfigureTransition(toIdle, AnimatorConditionMode.IfNot);

            AnimatorStateTransition toAttack = stateMachine.AddAnyStateTransition(attackState);
            toAttack.hasExitTime = false;
            toAttack.hasFixedDuration = true;
            toAttack.duration = 0f;
            toAttack.canTransitionToSelf = false;
            toAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

            AnimatorStateTransition attackFinished = attackState.AddTransition(idleState);
            attackFinished.hasExitTime = true;
            attackFinished.exitTime = 1f;
            attackFinished.hasFixedDuration = true;
            attackFinished.duration = 0f;
            attackFinished.canTransitionToSelf = false;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureTransition(AnimatorStateTransition transition, AnimatorConditionMode condition)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(condition, 0f, "IsMoving");
        }

        private static void ApplyToPlayerPrefab(Sprite defaultSprite, RuntimeAnimatorController controller)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

                var visual = new GameObject("CharacterVisual");
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = new Vector3(0f, 0.25f, 0f);

                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = defaultSprite;
                renderer.sortingOrder = 10;

                Animator animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;

                PlayerIdleAnimation behaviour = root.GetComponent<PlayerIdleAnimation>();
                if (behaviour == null) behaviour = root.AddComponent<PlayerIdleAnimation>();
                var serialized = new SerializedObject(behaviour);
                serialized.FindProperty("animator").objectReferenceValue = animator;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
