using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class SkullMonkWalkValidation
    {
        private const string Report = "Artifacts/SkullMonkWalk/Validation.txt";
        private static readonly EditorCurveBinding Binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        private static void Check(bool value, string message)
        {
            File.AppendAllText(Report, (value ? "PASS " : "FAIL ") + message + "\n");
            if (!value) throw new InvalidOperationException(message);
        }
        public static string ValidateRestore()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run restore verification outside Play Mode.");
            Directory.CreateDirectory("Artifacts/SkullMonkWalk");
            File.WriteAllText(Report, "Native Skull monk slicing, original backup, restore and playback verification\n");
            string backupClip = SkullMonkWalkSetup.Backup + "/SkullMonkWalk_Original.anim";
            var bytes = File.ReadAllBytes(backupClip);
            var original = AssetDatabase.LoadAssetAtPath<AnimationClip>(backupClip);
            var oldKeys = AnimationUtility.GetObjectReferenceCurve(original, Binding);
            Check(oldKeys.Length == 4 && original.frameRate == 4f, "Original four-frame motion and original frame rate preserved");
            Check(oldKeys.All(key => AssetDatabase.GetAssetPath(key.value) != SkullMonkWalkSetup.Sheet), "Backup still references original sheets");
            var snapshot = EditorSceneManager.OpenPreviewScene(SkullMonkWalkSetup.Backup + "/TempleGate_Original.unity");
            try
            {
                Check(snapshot.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Any(t => t.name == "Skull"),
                    "Scene snapshot includes the previously unsaved Skull object");
            }
            finally { EditorSceneManager.ClosePreviewScene(snapshot); }
            try
            {
                SkullMonkWalkSetup.Restore();
                var restored = AssetDatabase.LoadAssetAtPath<AnimationClip>(SkullMonkWalkSetup.ClipPath);
                var restoredKeys = AnimationUtility.GetObjectReferenceCurve(restored, Binding);
                Check(restoredKeys.Length == oldKeys.Length && restoredKeys.Zip(oldKeys, (a, b) => a.value == b.value && a.time == b.time).All(value => value),
                    "Restore menu restores the exact original sprite sequence and timings");
                var saved = AssetDatabase.LoadAssetAtPath<AnimatorController>(SkullMonkWalkSetup.Backup + "/SkullMonk_Original.controller");
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(SkullMonkWalkSetup.ControllerPath);
                Check(controller.layers[0].stateMachine.defaultState.name == saved.layers[0].stateMachine.defaultState.name,
                    "Restore menu restores original default animation state");
            }
            finally { SkullMonkWalkSetup.Apply(); }
            Check(File.ReadAllBytes(backupClip).SequenceEqual(bytes), "Reapplying generated walk leaves the original backup unchanged");
            ValidateAssets();
            return "Original restore and generated reapply verified; ready for Play Mode verification.";
        }
        private static void ValidateAssets()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SkullMonkWalkSetup.ClipPath);
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, Binding);
            var frames = AssetDatabase.LoadAllAssetsAtPath(SkullMonkWalkSetup.Sheet).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
            Check(frames.Length == 8 && keys.Length == 8 && keys.Select(key => key.value).Distinct().Count() == 8, "Eight distinct sprite slices and eight animation frames");
            Check(Mathf.Abs(clip.length - 1f) < .001f && clip.frameRate == 8f && AnimationUtility.GetAnimationClipSettings(clip).loopTime,
                "Generated motion is an eight-FPS, one-second loop");
            for (int i = 0; i < 8; i++)
                Check(keys[i].value == frames[i] && Mathf.Abs(keys[i].time - i / 8f) < .001f, "Frame " + i + " has the correct sprite and timing");
            Check(frames.All(sprite => Mathf.Abs(sprite.pixelsPerUnit - frames[0].pixelsPerUnit) < .001f
                && Mathf.Abs(sprite.pivot.y - frames[0].pivot.y) < .001f && sprite.rect.height == frames[0].rect.height),
                "All frames share world scale and vertical ground anchor");
            for (int i = 1; i < frames.Length; i++) Check(frames[i - 1].rect.xMax <= frames[i].rect.xMin,
                "Frame slices do not overlap: " + (i - 1) + " and " + i);
        }
        public static string ValidatePlayMode()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run in Play Mode.");
            ValidateAssets();
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                    SkullMonkWalkSetup.Backup + "/Skull_Original.prefab"), scene);
                var animator = root.GetComponent<Animator>();
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SkullMonkWalkSetup.ControllerPath);
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Update(0f);
                var renderer = root.GetComponent<SpriteRenderer>();
                var keys = AnimationUtility.GetObjectReferenceCurve(AssetDatabase.LoadAssetAtPath<AnimationClip>(SkullMonkWalkSetup.ClipPath), Binding);
                var position = root.transform.position;
                var scale = root.transform.localScale;
                for (int i = 0; i < 8; i++)
                {
                    animator.Play("Base Layer.SkullMonkWalk", 0, (i + .05f) / 8f); animator.Update(0f);
                    Check(renderer.sprite == keys[i].value, "Play Mode Animator renders generated frame " + i);
                    Check(root.transform.position == position && root.transform.localScale == scale, "Frame " + i + " leaves Skull transform unchanged");
                }
                animator.Play("Base Layer.SkullMonkWalk", 0, 1.00625f); animator.Update(0f);
                Check(renderer.sprite == keys[0].value, "Animator wraps from final frame back to first frame");
                var controller = (AnimatorController)animator.runtimeAnimatorController;
                Check(controller.layers[0].stateMachine.defaultState.name == "SkullMonkWalk", "Skull starts with generated walk when playing");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            int count = File.ReadAllLines(Report).Count(line => line.StartsWith("PASS "));
            File.AppendAllText(Report, "COMPLETE " + count + " checks passed\n");
            return count + " checks passed, including native Play Mode animation and original restore.";
        }
    }
}
