using System;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class FrogJumpVisualSetup
    {
        private const string Folder = "Assets/_Project/Animations/TutoMonster/Frog/";
        private const string Sheet = "Assets/_Project/Art/Tutorial Map/TutorialMonster/Frog/두꺼비 점프 제거.png";

        [MenuItem("Eastern Fantasy/Enemies/Align Frog Jump Visuals")]
        public static void Run()
        {
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "Idle.anim");
            var reference = (Sprite)AnimationUtility.GetObjectReferenceCurve(idle, binding)[0].value;
            Rect referenceBounds = PetCompanionPrefabSetup.VisibleBounds(reference);
            float referenceHeight = referenceBounds.height / reference.pixelsPerUnit;
            float feetBelowOrigin = (reference.pivot.y - referenceBounds.y) / reference.pixelsPerUnit;
            // Read the original imported frames so running setup twice does not rescale again.
            var originals = new Sprite[7];
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Sheet))
            {
                var sprite = asset as Sprite;
                if (sprite == null) continue;
                int index;
                if (int.TryParse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1), out index)
                    && index >= 0 && index < originals.Length) originals[index] = sprite;
            }
            foreach (var sprite in originals) if (sprite == null) throw new InvalidOperationException("Missing frog jump source frames.");
            // The final standing recovery pose is comparable to idle. Keep one
            // scale across the whole jump, preserving the drawn stretch and crouch.
            float ppu = PetCompanionPrefabSetup.VisibleBounds(originals[6]).height / referenceHeight;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "Jump.anim");
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            var corrected = new Sprite[7];
            for (int i = 0; i < originals.Length; i++)
            {
                var source = originals[i];
                var bounds = PetCompanionPrefabSetup.VisibleBounds(source);
                // Dust extends farther left than the body on the impact drawing.
                float bodyCenter = i == 3 ? source.rect.width * .53f : bounds.center.x;
                Vector2 pivot = new Vector2(bodyCenter / source.rect.width,
                    (bounds.y + feetBelowOrigin * ppu) / source.rect.height);
                var generated = Sprite.Create(source.texture, source.rect, pivot, ppu, 0, SpriteMeshType.FullRect);
                generated.name = "Frog_JumpFrame" + (i + 1);
                string path = Folder + generated.name + ".asset";
                corrected[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (corrected[i] == null) { corrected[i] = generated; AssetDatabase.CreateAsset(generated, path); }
                else { EditorUtility.CopySerialized(generated, corrected[i]); UnityEngine.Object.DestroyImmediate(generated); EditorUtility.SetDirty(corrected[i]); }
            }
            for (int i = 0; i < keys.Length; i++)
                keys[i].value = corrected[Mathf.Min(i, corrected.Length - 1)];
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            Debug.Log("Frog jump scale and foot baseline now match idle; timing and root transforms are unchanged.");
        }
    }
}
