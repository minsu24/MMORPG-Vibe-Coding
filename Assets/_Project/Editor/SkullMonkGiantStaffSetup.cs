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
    public static class SkullMonkGiantStaffSetup
    {
        private const string Art = SkullMonkWaveSetup.Art, Anim = SkullMonkWaveSetup.Anim;

        [MenuItem("Eastern Fantasy/Skull Monk/Configure Giant Staff Judgment")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            Copy("해골 승려 거대 석장 던짐.png", "SkullMonkGiantStaffThrow.png");
            Copy("해골 승려 거대 석장 하강 명령.png", "SkullMonkGiantStaffCommand.png");
            Copy("거대 석장 낙하.png", "SkullMonkGiantStaffFall.png");
            Copy("거대 석장 충격 이펙트.png", "SkullMonkGiantStaffImpact.png");
            Copy("해골 승려 대기 석장 없음.png", "SkullMonkNoStaffIdle.png");
            Copy("해골 승려 석장 재생성.png", "SkullMonkStaffRegenerate.png");
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "SkullMonkIdle.anim");
            var reference = (Sprite)AnimationUtility.GetObjectReferenceCurve(idle, binding)[0].value;
            var cells = Enumerable.Range(0, 8).Select(i => new Rect(Mathf.Round(i * 271.5f), 0,
                Mathf.Round((i + 1) * 271.5f) - Mathf.Round(i * 271.5f), 724)).ToArray();
            // All frames share the back heel and floor, while separate sheet scales
            // compensate for the source artist's different character sizes.
            Character("SkullMonkGiantStaffThrow.png", cells, reference, 372f,
                new[] { 69f, 70f, 70f, 70f, 70f, 70f, 70f, 70f }, 61f);
            Character("SkullMonkGiantStaffCommand.png", cells, reference, 408f,
                new[] { 79f, 79f, 79f, 79f, 79f, 79f, 79f, 79f }, 122f);
            Character("SkullMonkNoStaffIdle.png", cells, reference, 464f,
                new[] { 82f, 82f, 82f, 82f, 82f, 82f, 82f, 82f }, 99f);
            Character("SkullMonkStaffRegenerate.png", cells, reference, 464f,
                new[] { 80f, 80f, 80f, 80f, 80f, 80f, 80f, 80f }, 99f);
            FullRect(Art + "SkullMonkStaffRegenerate.png");

            int[] edges = { 0, 417, 965, 1540, 2172 };
            var falling = Enumerable.Range(0, 4).Select(i => new Rect(edges[i], 160, edges[i + 1] - edges[i], 430)).ToArray();
            float[] staffGroundPivots = { .125f, .16f, .28f, .28f };
            SkullMonkMeleeSetup.Slice(Art + "SkullMonkGiantStaffFall.png", falling, 100f,
                falling.Select((r, i) => new Vector2(.5f, staffGroundPivots[i])).ToArray());
            FullRect(Art + "SkullMonkGiantStaffFall.png");
            var impacts = Enumerable.Range(0, 8).Select(i => new Rect(i % 4 * 543, i < 4 ? 362 : 0, 543, 362)).ToArray();
            SkullMonkMeleeSetup.Slice(Art + "SkullMonkGiantStaffImpact.png", impacts, 100f,
                impacts.Select(r => new Vector2(.5f, .31f)).ToArray());
            FullRect(Art + "SkullMonkGiantStaffImpact.png");

            // Stop at the released, raised hand. The command sheet supplies its descent.
            Clip("SkullMonkGiantStaffThrow", Frames("SkullMonkGiantStaffThrow.png"), new[] { 0, 1, 2, 3, 4, 5 },
                new[] { 0f, .10f, .20f, .30f, .40f, MON_SkullMonk.GiantStaffReleaseTime }, MON_SkullMonk.GiantStaffThrowTime, false);
            Clip("SkullMonkGiantStaffCommand", Frames("SkullMonkGiantStaffCommand.png"), Enumerable.Range(0, 8).ToArray(),
                new[] { 0f, .12f, .24f, .34f, .42f, MON_SkullMonk.GiantStaffCommandDropTime, .58f, .70f }, MON_SkullMonk.GiantStaffCommandTime, false);
            Clip("SkullMonkNoStaffIdle", Frames("SkullMonkNoStaffIdle.png"), Enumerable.Range(0, 8).ToArray(),
                Enumerable.Range(0, 8).Select(i => i * .14f).ToArray(), 1.12f, true);
            var regenerate = Clip("SkullMonkStaffRegenerate", Frames("SkullMonkStaffRegenerate.png"), Enumerable.Range(0, 8).ToArray(),
                new[] { 0f, .16f, .32f, .48f, .64f, .80f, .98f, 1.12f }, MON_SkullMonk.GiantStaffRegenerateTime, false);
            var keys = AnimationUtility.GetObjectReferenceCurve(regenerate, binding).ToList();
            keys.Add(new ObjectReferenceKeyframe { time = 1.24f, value = reference });
            AnimationUtility.SetObjectReferenceCurve(regenerate, binding, keys.ToArray());

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim + "SkullMonk.controller");
            var machine = controller.layers[0].stateMachine;
            foreach (string name in new[] { "SkullMonkGiantStaffThrow", "SkullMonkGiantStaffCommand", "SkullMonkNoStaffIdle", "SkullMonkStaffRegenerate" })
            {
                var state = machine.states.FirstOrDefault(s => s.state.name == name).state;
                if (state == null) state = machine.AddState(name);
                state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + name + ".anim");
                EditorUtility.SetDirty(state);
            }
            var scene = SceneManager.GetSceneByPath(SkullMonkWaveSetup.ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(SkullMonkWaveSetup.ScenePath, OpenSceneMode.Additive);
            var boss = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MON_SkullMonk>(true)).Single();
            var data = new SerializedObject(boss);
            Assign(data, "giantStaffFallFrames", Frames("SkullMonkGiantStaffFall.png"));
            var paintedImpact = AssetDatabase.LoadAllAssetsAtPath(SkullMonkGiantStaffImpactSetup.SpritePath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            Assign(data, "giantStaffImpactFrames", paintedImpact.Length == 8 ? paintedImpact : Frames("SkullMonkGiantStaffImpact.png"));
            data.FindProperty("giantStaffImpactHeight").floatValue = paintedImpact.Length == 8 ? 2.4f : 5.5f;
            var highResolution = AssetDatabase.LoadAllAssetsAtPath(SkullMonkGiantStaffHighResolutionSetup.SpritePath).OfType<Sprite>().FirstOrDefault();
            if (highResolution != null) data.FindProperty("giantStaffHighResolutionSprite").objectReferenceValue = highResolution;
            data.FindProperty("giantStaffWarningSeconds").floatValue = 2.5f;
            data.FindProperty("giantStaffEdgeSafety").floatValue = 5f;
            data.FindProperty("giantStaffMaxWidth").floatValue = 26f;
            data.FindProperty("giantStaffGroundVisualOffset").floatValue = .4f;
            data.FindProperty("giantStaffCooldown").floatValue = 24f;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(machine); EditorUtility.SetDirty(regenerate);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Giant staff judgment configured: 2.5-second shadow, safe edges, 80% maximum HP impact.");
        }

        private static void Character(string file, Rect[] cells, Sprite reference, float bodyPixels, float[] heels, float sole)
        {
            float ppu = bodyPixels / reference.bounds.size.y;
            SkullMonkMeleeSetup.Slice(Art + file, cells, ppu, cells.Select((r, i) => new Vector2(
                (heels[i] + .68f * ppu) / r.width, (sole - reference.bounds.min.y * ppu) / r.height)).ToArray(), true);
        }
        private static void Copy(string from, string to)
        {
            if (!File.Exists(Art + to)) File.Copy(Art + from, Art + to);
            AssetDatabase.ImportAsset(Art + to);
        }
        private static Sprite[] Frames(string file) => SkullMonkWaveSetup.Frames(file);
        private static void Assign(SerializedObject obj, string name, Sprite[] frames)
        {
            var property = obj.FindProperty(name); property.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }
        private static void FullRect(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
        }
        private static AnimationClip Clip(string name, Sprite[] sprites, int[] order, float[] times, float duration, bool loop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + name + ".anim");
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, Anim + name + ".anim"); }
            clip.name = name; clip.frameRate = 60;
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
                order.Select((frame, i) => new ObjectReferenceKeyframe { time = times[i], value = sprites[frame] }).ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.startTime = 0; settings.stopTime = duration; settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings); AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
            EditorUtility.SetDirty(clip); return clip;
        }
    }
}
