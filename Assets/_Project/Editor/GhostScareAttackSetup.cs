using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class GhostScareAttackSetup
    {
        private const string Folder = "Assets/_Project/Animations/TutoMonster/Ghost";
        private const string Art = "Assets/_Project/Art/Tutorial Map/TutorialMonster/Ghost/";

        [MenuItem("Eastern Fantasy/Enemies/Setup Ghost Scare Attack")]
        public static void Run()
        {
            var source = AssetDatabase.LoadAllAssetsAtPath(Art + "유령 공격.png")
                .OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
            var effects = AssetDatabase.LoadAllAssetsAtPath(Art + "유령 공격 이펙트.png")
                .OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
            if (source.Length != 8 || effects.Length != 4)
                throw new InvalidOperationException("Expected eight ghost attack and four effect frames.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/Ghost.prefab");
            var idle = prefab.GetComponent<SpriteRenderer>().sprite;
            var idleBounds = PetCompanionPrefabSetup.VisibleBounds(idle);
            var first = PetCompanionPrefabSetup.VisibleBounds(source[0]);
            float ppu = idle.pixelsPerUnit * first.height / idleBounds.height;
            float feet = (idle.pivot.y - idleBounds.y) / idle.pixelsPerUnit;
            var sprites = new Sprite[8];
            for (int i = 0; i < sprites.Length; i++)
            {
                // Keep one scale and a stable lower edge as the ghost changes pose.
                var visible = PetCompanionPrefabSetup.VisibleBounds(source[i]);
                sprites[i] = SaveSprite(source[i], "Ghost_AttackFrame" + (i + 1), ppu,
                    new Vector2(.5f, (visible.y + feet * ppu) / source[i].rect.height));
            }
            float effectPpu = effects.Max(s => PetCompanionPrefabSetup.VisibleBounds(s).width) / 2.4f;
            for (int i = 0; i < effects.Length; i++)
                effects[i] = SaveSprite(effects[i], "Ghost_EffectFrame" + (i + 1), effectPpu, new Vector2(.5f, .5f));
            // The flight settles into frames 3-5 while it follows its target.
            var fly = Clip("Ghost_Fly", new[] { sprites[0], sprites[1], sprites[2], sprites[3], sprites[4], sprites[2], sprites[3], sprites[4] },
                new[] { 0f, .08f, .16f, .24f, .32f, .40f, .48f, .56f }, true);
            var scare = Clip("Ghost_Scare", new[] { sprites[5], sprites[6], sprites[7], sprites[7] },
                new[] { 0f, .12f, .40f, .60f }, false);
            AnimationUtility.SetAnimationEvents(scare, new[] { new AnimationEvent { time = .12f, functionName = "EmitScareAttack" } });
            var controller = (AnimatorController)prefab.GetComponent<Animator>().runtimeAnimatorController;
            AddState(controller, fly); AddState(controller, scare);
            var data = new SerializedObject(prefab.GetComponent<MON_Ghost>());
            var frames = data.FindProperty("scareEffectFrames"); frames.arraySize = effects.Length;
            for (int i = 0; i < effects.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = effects[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log("Ghost scare attack configured: frame 7 emits effect and existing contact damage.");
        }

        private static Sprite SaveSprite(Sprite source, string name, float ppu, Vector2 pivot)
        {
            string path = Folder + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var sprite = Sprite.Create(source.texture, source.rect, pivot, ppu, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            if (existing == null) { AssetDatabase.CreateAsset(sprite, path); return sprite; }
            EditorUtility.CopySerialized(sprite, existing);
            UnityEngine.Object.DestroyImmediate(sprite);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static AnimationClip Clip(string name, Sprite[] frames, float[] times, bool loop)
        {
            string path = Folder + "/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip { name = name }; AssetDatabase.CreateAsset(clip, path); }
            clip.frameRate = 25f;
            var keys = frames.Select((sprite, i) => new ObjectReferenceKeyframe { time = times[i], value = sprite }).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void AddState(AnimatorController controller, AnimationClip clip)
        {
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == clip.name);
            if (state == null) state = machine.AddState(clip.name);
            state.motion = clip;
            state.writeDefaultValues = true;
            EditorUtility.SetDirty(controller);
        }
    }
}
