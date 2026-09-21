using System;
using System.Linq;
using EasternFantasy.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class PlayerProjectileSetup
    {
        private const string SheetPath = "Assets/_Project/Art/Player/Projectile.png";
        private const string AnimationFolder = "Assets/_Project/Animations/Player";
        private const string ClipPath = AnimationFolder + "/Projectile.anim";
        private const string ControllerPath = AnimationFolder + "/Projectile.controller";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Player";
        private const string ProjectilePrefabPath = PrefabFolder + "/Projectile.prefab";
        private const string PlayerPrefabPath = PrefabFolder + "/Player.prefab";
        private const int FrameCount = 4;
        private const float FramesPerSecond = 12f;

        [InitializeOnLoadMethod]
        private static void ApplyWhenPrefabIsMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath) != null) return;
            Apply();
        }

        [MenuItem("Eastern Fantasy/Apply Player Projectile")]
        public static void Apply()
        {
            Sprite[] frames = LoadFrames();
            EnsureFolder(AnimationFolder);
            EnsureFolder(PrefabFolder);

            AnimationClip clip = CreateOrReplaceClip(frames);
            AnimatorController controller = CreateOrReplaceController(clip);
            PlayerProjectile projectilePrefab = CreateOrReplaceProjectilePrefab(frames[0], controller);
            AssignToPlayerPrefab(projectilePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Applied {frames.Length}-frame projectile animation and prefab.");
        }

        private static Sprite[] LoadFrames()
        {
            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(SheetPath)
                .OfType<Sprite>()
                .Where(sprite => GetFrameIndex(sprite) >= 0 && GetFrameIndex(sprite) < FrameCount)
                .OrderBy(GetFrameIndex)
                .ToArray();
            if (frames.Length != FrameCount)
                throw new InvalidOperationException(
                    $"Expected {FrameCount} projectile frames at {SheetPath}, found {frames.Length}.");
            return frames;
        }

        private static int GetFrameIndex(Sprite sprite)
        {
            int separator = sprite.name.LastIndexOf('_');
            if (separator < 0 || separator == sprite.name.Length - 1)
                return -1;

            return int.TryParse(sprite.name.Substring(separator + 1), out int index)
                ? index
                : -1;
        }

        private static AnimationClip CreateOrReplaceClip(Sprite[] frames)
        {
            AssetDatabase.DeleteAsset(ClipPath);
            var clip = new AnimationClip { name = "Projectile", frameRate = FramesPerSecond };
            var binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            ObjectReferenceKeyframe[] keys = frames.Select((sprite, index) =>
                new ObjectReferenceKeyframe
                {
                    time = index / FramesPerSecond,
                    value = sprite
                }).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, ClipPath);
            return clip;
        }

        private static AnimatorController CreateOrReplaceController(AnimationClip clip)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorState state = controller.layers[0].stateMachine.AddState("Projectile");
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static PlayerProjectile CreateOrReplaceProjectilePrefab(
            Sprite defaultSprite,
            RuntimeAnimatorController controller)
        {
            var temporary = new GameObject("Projectile");
            try
            {
                SpriteRenderer renderer = temporary.AddComponent<SpriteRenderer>();
                renderer.sprite = defaultSprite;
                renderer.sortingOrder = 11;

                Animator animator = temporary.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;

                CircleCollider2D hitbox = temporary.AddComponent<CircleCollider2D>();
                hitbox.isTrigger = true;
                hitbox.radius = 0.45f;

                Rigidbody2D body = temporary.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                temporary.AddComponent<PlayerProjectile>();
                PrefabUtility.SaveAsPrefabAsset(temporary, ProjectilePrefabPath);
                AssetDatabase.ImportAsset(
                    ProjectilePrefabPath,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
                if (prefab == null)
                    throw new InvalidOperationException($"Failed to create prefab at {ProjectilePrefabPath}.");
                return prefab.GetComponent<PlayerProjectile>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }

        private static void AssignToPlayerPrefab(PlayerProjectile projectilePrefab)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                PlayerEntity playerEntity = root.GetComponent<PlayerEntity>();
                if (playerEntity == null) playerEntity = root.AddComponent<PlayerEntity>();

                PlayerProjectileShooter shooter = root.GetComponent<PlayerProjectileShooter>();
                if (shooter == null) shooter = root.AddComponent<PlayerProjectileShooter>();

                var serialized = new SerializedObject(shooter);
                serialized.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
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
