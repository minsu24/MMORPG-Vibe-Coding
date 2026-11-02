using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class GoblinLeapAttackSetup
    {
        private const string Folder = "Assets/_Project/Animations/TutoMonster/Goblin";
        private const string Art = "Assets/_Project/Art/Tutorial Map/TutorialMonster/Goblin/";

        [MenuItem("Eastern Fantasy/Enemies/Setup Goblin Leap Attack")]
        public static void Run()
        {
            var a = AssetDatabase.LoadAllAssetsAtPath(Art + "도깨비 공격 2-1468.png")
                .OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
            var b = AssetDatabase.LoadAllAssetsAtPath(Art + "도깨비 공격 2-257.png")
                .OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
            if (a.Length != 4 || b.Length != 3)
                throw new InvalidOperationException("Expected supplied frames 1,2,4,5,6,7,8.");
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/Goblin_Idle.anim");
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var idleSprite = (Sprite)AnimationUtility.GetObjectReferenceCurve(idle, binding)[0].value;
            var idleBounds = PetCompanionPrefabSetup.VisibleBounds(idleSprite);
            var firstBounds = PetCompanionPrefabSetup.VisibleBounds(a[0]);
            float pixelsPerUnit = idleSprite.pixelsPerUnit * firstBounds.height / idleBounds.height;
            float feetBelowOrigin = (idleSprite.pivot.y - idleBounds.y) / idleSprite.pixelsPerUnit;
            Sprite[] originals = { a[0], b[0], a[1], b[1], a[2], b[2], a[3] };
            int[] frameNumbers = { 1, 2, 4, 5, 6, 7, 8 };
            float[] times = { 0f, .12f, .32f, .48f, .62f, .8f, 1f };
            var keys = new ObjectReferenceKeyframe[8];
            for (int i = 0; i < originals.Length; i++)
            {
                var original = originals[i];
                string spritePath = Folder + "/Goblin_AttackFrame" + frameNumbers[i] + ".asset";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                // Match visible body height, not the transparent sprite rectangle.
                // Raised clubs remain taller; every pose keeps the same body scale.
                var visible = PetCompanionPrefabSetup.VisibleBounds(original);
                float pivotX = i == 5 ? .35f : .5f;
                var pivot = new Vector2(pivotX,
                    (visible.y + feetBelowOrigin * pixelsPerUnit) / original.rect.height);
                var generated = Sprite.Create(original.texture, original.rect, pivot,
                    pixelsPerUnit, 0, SpriteMeshType.FullRect);
                generated.name = "Goblin_AttackFrame" + frameNumbers[i];
                if (sprite == null)
                {
                    sprite = generated;
                    AssetDatabase.CreateAsset(sprite, spritePath);
                }
                else
                {
                    EditorUtility.CopySerialized(generated, sprite);
                    UnityEngine.Object.DestroyImmediate(generated);
                    EditorUtility.SetDirty(sprite);
                }
                keys[i] = new ObjectReferenceKeyframe { time = times[i], value = sprite };
            }
            keys[7] = new ObjectReferenceKeyframe { time = 1.2f, value = keys[6].value };
            string path = Folder + "/Goblin_Attack.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.frameRate = 25f;
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            var events = AnimationUtility.GetAnimationEvents(clip)
                .Where(e => e.functionName != "PlayGoblinSwingSound").ToList();
            events.Add(new AnimationEvent { time = .62f, functionName = "PlayGoblinSwingSound" });
            AnimationUtility.SetAnimationEvents(clip, events.ToArray());
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "/Goblin.controller");
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Goblin_Attack");
            if (state == null) state = machine.AddState("Goblin_Attack", new Vector3(560, 200));
            state.motion = clip;
            state.writeDefaultValues = true;
            EditorUtility.SetDirty(clip);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("Goblin leap attack uses supplied frames 1,2,4,5,6,7,8 and the existing attack power.");
        }
    }
}
