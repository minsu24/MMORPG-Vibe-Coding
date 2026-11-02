using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class SpiderRangedAttackSetup
    {
        private const string Folder = "Assets/_Project/Animations/TutoMonster/Spider";
        private const string Art = "Assets/_Project/Art/Tutorial Map/TutorialMonster/Spider/";

        [MenuItem("Eastern Fantasy/Enemies/Setup Spider Ranged Attack")]
        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/Spider.prefab");
            var idle = prefab.GetComponent<SpriteRenderer>().sprite;
            var idleVisible = PetCompanionPrefabSetup.VisibleBounds(idle);
            float height = idleVisible.height / idle.pixelsPerUnit;
            float foot = (idle.pivot.y - idleVisible.y) / idle.pixelsPerUnit;
            var attack = Frames("거미 공격.png", "Spider_AttackFrame", 8, height, foot, true);
            var projectile = Frames("거미 투사체 수정.png", "Spider_SpitFrame", 6, 1.1f, 0f, false);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/Spider_Attack.anim");
            if (clip == null) { clip = new AnimationClip { name = "Spider_Attack" }; AssetDatabase.CreateAsset(clip, Folder + "/Spider_Attack.anim"); }
            clip.frameRate = 25f;
            var keys = new ObjectReferenceKeyframe[9];
            for (int i = 0; i < 8; i++) keys[i] = new ObjectReferenceKeyframe { time = i * .1f, value = attack[i] };
            keys[8] = new ObjectReferenceKeyframe { time = .84f, value = attack[7] };
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AnimationUtility.SetAnimationEvents(clip, new[] { new AnimationEvent { time = .4f, functionName = "EmitSpiderProjectile" } });
            var controller = (AnimatorController)prefab.GetComponent<Animator>().runtimeAnimatorController;
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Spider_Attack");
            if (state == null) state = machine.AddState("Spider_Attack", new Vector3(520, 180));
            state.motion = clip;
            state.writeDefaultValues = true;
            var root = new GameObject("Spider Spit Projectile");
            try
            {
                root.layer = LayerMask.NameToLayer("Ignore Raycast");
                var artwork = root.AddComponent<SpriteRenderer>(); artwork.sprite = projectile[0];
                artwork.sharedMaterial = prefab.GetComponent<SpriteRenderer>().sharedMaterial;
                var body = root.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0; body.freezeRotation = true;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                var collider = root.AddComponent<CircleCollider2D>(); collider.isTrigger = true; collider.radius = .14f;
                var spit = root.AddComponent<SpiderSpitProjectile>();
                var data = new SerializedObject(spit);
                SetFrames(data, "flightFrames", projectile.Take(3).ToArray());
                SetFrames(data, "impactFrames", projectile.Skip(3).ToArray());
                data.ApplyModifiedPropertiesWithoutUndo();
                var saved = PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Project/Prefabs/Enemy/SpiderSpitProjectile.prefab");
                data = new SerializedObject(prefab.GetComponent<MON_Spider>());
                data.FindProperty("spitProjectilePrefab").objectReferenceValue = saved.GetComponent<SpiderSpitProjectile>();
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(prefab);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            EditorUtility.SetDirty(clip); EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        private static void SetFrames(SerializedObject data, string name, Sprite[] frames)
        {
            var field = data.FindProperty(name); field.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) field.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

        private static Sprite[] Frames(string file, string prefix, int count, float size, float feet, bool attack)
        {
            string path = Art + file;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var raw = new Texture2D(2, 2); raw.LoadImage(System.IO.File.ReadAllBytes(path));
            try
            {
                var pixels = raw.GetPixels32();
                var cuts = new int[count + 1]; cuts[count] = raw.width;
                for (int i = 1; i < count; i++)
                {
                    int mid = raw.width * i / count, best = mid, minimum = int.MaxValue;
                    for (int x = mid - 45; x <= mid + 45; x++)
                    {
                        int alpha = 0;
                        for (int y = 0; y < raw.height; y++) if (pixels[y * raw.width + x].a >= 16) alpha++;
                        if (alpha < minimum || (alpha == minimum && Math.Abs(x - mid) < Math.Abs(best - mid)))
                        { minimum = alpha; best = x; }
                    }
                    cuts[i] = best;
                }
                var rects = new Rect[count];
                for (int i = 0; i < count; i++)
                {
                    int xMin = cuts[i + 1], xMax = cuts[i], yMin = raw.height, yMax = 0;
                    for (int y = 0; y < raw.height; y++) for (int x = cuts[i]; x < cuts[i + 1]; x++)
                        if (pixels[y * raw.width + x].a >= 16)
                        { xMin = Math.Min(xMin, x); xMax = Math.Max(xMax, x); yMin = Math.Min(yMin, y); yMax = Math.Max(yMax, y); }
                    rects[i] = new Rect(Math.Max(cuts[i], xMin - 2), Math.Max(0, yMin - 2),
                        Math.Min(cuts[i + 1] - Math.Max(cuts[i], xMin - 2), xMax - xMin + 5), yMax - yMin + 5);
                }
                float ppu = attack ? rects[0].height / size : rects.Take(3).Max(r => r.width) / size;
                var sprites = new Sprite[count];
                for (int i = 0; i < count; i++)
                {
                    Vector2 pivot = attack ? new Vector2(.5f, (2f + feet * ppu) / rects[i].height)
                        : i < 3 ? new Vector2(1f - rects[0].height * .5f / rects[i].width, .5f) : new Vector2(.5f, .5f);
                    var generated = Sprite.Create(texture, rects[i], pivot, ppu, 0, SpriteMeshType.FullRect);
                    generated.name = prefix + (i + 1);
                    string spritePath = Folder + "/" + generated.name + ".asset";
                    sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (sprites[i] == null) { sprites[i] = generated; AssetDatabase.CreateAsset(generated, spritePath); }
                    else { EditorUtility.CopySerialized(generated, sprites[i]); UnityEngine.Object.DestroyImmediate(generated); EditorUtility.SetDirty(sprites[i]); }
                }
                return sprites;
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
        }
    }
}
