using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.EditorTools
{
    public static class FrogTongueVisualSetup
    {
        private const string Folder = "Assets/_Project/Animations/TutoMonster/Frog/";
        private const string Art = "Assets/_Project/Art/Tutorial Map/TutorialMonster/Frog/";

        [MenuItem("Eastern Fantasy/Enemies/Align Frog Tongue Attack")]
        public static void Run()
        {
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var idle = (Sprite)AnimationUtility.GetObjectReferenceCurve(
                AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "Idle.anim"), binding)[0].value;
            var idleBounds = PetCompanionPrefabSetup.VisibleBounds(idle);
            float height = idleBounds.height / idle.pixelsPerUnit;
            float feet = (idle.pivot.y - idleBounds.y) / idle.pixelsPerUnit;
            float bodyAnchor = (idle.pivot.x - idleBounds.x) / idle.pixelsPerUnit;
            var regular = AssetDatabase.LoadAllAssetsAtPath(Art + "두꺼비 혀.png").OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
            var extended = AssetDatabase.LoadAllAssetsAtPath(Art + "두꺼비 혀 길게.png").OfType<Sprite>().OrderBy(s => s.rect.x).ToArray()[1];
            Sprite[] sources = { regular[0], regular[1], regular[2], regular[3], extended, regular[4], regular[5] };
            float regularPpu = PetCompanionPrefabSetup.VisibleBounds(regular[0]).height / height;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "tongueAttack.anim");
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            for (int i = 0; i < sources.Length; i++)
            {
                var source = sources[i]; var bounds = PetCompanionPrefabSetup.VisibleBounds(source);
                float ppu = i == 4 ? bounds.height / height : regularPpu;
                // Anchor the body, not the center of the rectangle that includes the tongue.
                var pivot = new Vector2((bounds.x + bodyAnchor * ppu) / source.rect.width,
                    (bounds.y + feet * ppu) / source.rect.height);
                var generated = Sprite.Create(source.texture, source.rect, pivot, ppu, 0, SpriteMeshType.FullRect);
                generated.name = "Frog_TongueFrame" + (i + 1);
                string path = Folder + generated.name + ".asset";
                var saved = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (saved == null) { saved = generated; AssetDatabase.CreateAsset(saved, path); }
                else { EditorUtility.CopySerialized(generated, saved); Object.DestroyImmediate(generated); EditorUtility.SetDirty(saved); }
                keys[i].value = saved;
            }
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            EditorUtility.SetDirty(clip);
            string scenePath = "Assets/_Project/Scenes/TutorialMap/Marshland.unity";
            var scene = SceneManager.GetSceneByPath(scenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                foreach (var frog in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MON_Frog>(true)))
                {
                    var data = new SerializedObject(frog);
                    data.FindProperty("attackRange").floatValue = 4.4f;
                    data.FindProperty("hitDelay").floatValue = .5f;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            AssetDatabase.SaveAssets();
        }
    }
}
