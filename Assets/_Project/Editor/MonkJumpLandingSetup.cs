using System;
using System.Linq;
using EasternFantasy.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class MonkJumpLandingSetup
    {
        public static void Apply()
        {
            const string sheet = "Assets/_Project/Art/Monk/수도승 점프 수정 최종.png";
            var frames = AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
            if (frames.Length != 6) throw new InvalidOperationException("Expected six frames in the updated monk jump sheet.");
            // Match the landing pose's height to idle; the new sheet has a different resolution.
            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Monk/MonkIdle.anim");
            var idleFrame = (Sprite)AnimationUtility.GetObjectReferenceCurve(idleClip, AnimationUtility.GetObjectReferenceCurveBindings(idleClip).First())[0].value;
            var importer = (TextureImporter)AssetImporter.GetAtPath(sheet);
            // World bounds account for platform texture downscaling as well as source pixels.
            float pixelsPerUnit = importer.spritePixelsPerUnit * frames[5].bounds.size.y / idleFrame.bounds.size.y;
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit)) {
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.SaveAndReimport();
                frames = AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
            }
            if (frames.Length != 6) throw new InvalidOperationException("Expected six frames in the updated monk jump sheet.");
            var jump = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Monk/MonkJump.anim");
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            float interval = 1f / jump.frameRate;
            AnimationUtility.SetObjectReferenceCurve(jump, binding, frames.Take(4).Select((sprite, i) => new ObjectReferenceKeyframe { time = i * interval, value = sprite }).ToArray());
            SetDuration(jump, 4 * interval);
            const string landPath = "Assets/_Project/Animations/Monk/MonkLand.anim";
            var land = AssetDatabase.LoadAssetAtPath<AnimationClip>(landPath);
            if (land == null) { land = new AnimationClip { name = "MonkLand", frameRate = jump.frameRate }; AssetDatabase.CreateAsset(land, landPath); }
            AnimationUtility.SetObjectReferenceCurve(land, binding, new[] {
                new ObjectReferenceKeyframe { time = 0, value = frames[5] }
            });
            SetDuration(land, interval);
            const string fallPath = "Assets/_Project/Animations/Monk/MonkFall.anim";
            var fall = AssetDatabase.LoadAssetAtPath<AnimationClip>(fallPath);
            if (fall == null) { fall = new AnimationClip { name = "MonkFall", frameRate = jump.frameRate }; AssetDatabase.CreateAsset(fall, fallPath); }
            AnimationUtility.SetObjectReferenceCurve(fall, binding, new[] {
                new ObjectReferenceKeyframe { time = 0, value = frames[4] }
            });
            SetDuration(fall, interval);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GUIDToAssetPath("12e416eec1164b842b6b6987e62deb5c"));
            var machine = controller.layers[0].stateMachine;
            var jumpState = machine.states.Single(s => s.state.name == "MonkJump").state;
            // Rise uses frames 1-4; velocity selects frame 5, and real contact selects frame 6.
            foreach (var transition in jumpState.transitions) jumpState.RemoveTransition(transition);
            var falling = machine.states.FirstOrDefault(s => s.state.name == "MonkFall").state;
            if (falling == null) falling = machine.AddState("MonkFall", new Vector3(1050, -100));
            falling.motion = fall;
            foreach (var transition in falling.transitions) falling.RemoveTransition(transition);
            var landing = machine.states.FirstOrDefault(s => s.state.name == "MonkLand").state;
            if (landing == null) landing = machine.AddState("MonkLand", new Vector3(1050, 0));
            landing.motion = land;
            foreach (var transition in landing.transitions) landing.RemoveTransition(transition);
            var jumpAgain = landing.AddTransition(jumpState); jumpAgain.hasExitTime = false; jumpAgain.duration = 0;
            jumpAgain.AddCondition(AnimatorConditionMode.If, 0, "isJumping");
            foreach (string stateName in new[] { "MonkMove", "MonkIdle" }) {
                var transition = landing.AddTransition(machine.states.Single(s => s.state.name == stateName).state);
                transition.hasExitTime = true; transition.exitTime = 1; transition.duration = 0;
                transition.AddCondition(AnimatorConditionMode.IfNot, 0, "isJumping");
                transition.AddCondition(stateName == "MonkMove" ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "IsMoving");
            }
            EditorUtility.SetDirty(controller);
            const string prefabPath = "Assets/_Project/Prefabs/Player/MonkPlayer.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try {
                var component = prefab.GetComponent<MonkJumpAnimation>();
                if (component == null) component = prefab.AddComponent<MonkJumpAnimation>();
                var data = new SerializedObject(component);
                data.FindProperty("animator").objectReferenceValue = prefab.GetComponentInChildren<Animator>(true);
                data.FindProperty("groundLayers").intValue = new SerializedObject(prefab.GetComponent<PlayerMovement2D>()).FindProperty("groundLayers").intValue;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            AssetDatabase.SaveAssets();
        }

        private static void SetDuration(AnimationClip clip, float duration)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false; settings.startTime = 0; settings.stopTime = duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }
    }
}
