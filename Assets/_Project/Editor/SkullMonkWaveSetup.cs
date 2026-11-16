using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class SkullMonkWaveSetup
    {
        public const string Art = "Assets/_Project/Art/Old Temple/Monster/SkullMonk/";
        public const string Anim = "Assets/_Project/Animations/Old Temple/SkullMonk/";
        public const string ScenePath = "Assets/_Project/Scenes/OldTemple/OldTemple - Center.unity";
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "SkullMonkIdle.anim");
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var reference = (Sprite)AnimationUtility.GetObjectReferenceCurve(idle, binding)[0].value;
            float ppu = 426f / reference.bounds.size.y;
            Copy("해골 승려 파동공격1.png", "SkullMonkWavePlant.png");
            Copy("해골 승려 파동공격2.png", "SkullMonkWaveBow.png");
            Copy("파동 공격 석장 이펙트.png", "SkullMonkWaveSmoke.png");
            var cells = Enumerable.Range(0, 8).Select(i => new Rect(Mathf.Round(i * 271.5f), 0,
                Mathf.Round((i + 1) * 271.5f) - Mathf.Round(i * 271.5f), 724)).ToArray();
            float[] heel = { 60, 62, 63, 65, 66, 67, 70, 76 };
            SkullMonkMeleeSetup.Slice(Art + "SkullMonkWavePlant.png", cells, ppu,
                cells.Select((r, i) => new Vector2((heel[i] + .68f * ppu) / r.width,
                    (100 - reference.bounds.min.y * ppu) / r.height)).ToArray(), true);
            // Keep the shaft fixed in world space throughout the repeated bows.
            float[] shaft = { 181, 176, 182, 194, 189, 184, 210, 205 };
            float[] sole = { 136, 138, 138, 138, 138, 138, 140, 140 };
            SkullMonkMeleeSetup.Slice(Art + "SkullMonkWaveBow.png", cells, ppu,
                cells.Select((r, i) => new Vector2((shaft[i] - .65f * ppu) / r.width,
                    (sole[i] - reference.bounds.min.y * ppu) / r.height)).ToArray(), true);
            var smoke = new[] { new Rect(60,90,480,340), new Rect(593,98,480,420),
                new Rect(1108,102,504,454), new Rect(1657,114,478,402) };
            SkullMonkMeleeSetup.Slice(Art + "SkullMonkWaveSmoke.png", smoke, 190f,
                Enumerable.Repeat(new Vector2(.5f,0),4).ToArray());
            var plant = Frames("SkullMonkWavePlant.png");
            var bow = Frames("SkullMonkWaveBow.png");
            var prepare = Clip("SkullMonkWavePlant", plant, Enumerable.Range(0, 8).ToArray(),
                new[] { 0f, .14f, .28f, .43f, .60f, .76f, .88f, 1.0f }, 1.2f,
                new AnimationEvent { time = 1f, functionName = "SkullMonkPlantStaff" },
                new AnimationEvent { time = 1.19f, functionName = "SkullMonkBeginWavePulses" });
            var pulse = Clip("SkullMonkWavePulse", bow, new[] { 0,1,2,3,4,5,6,7,2,1,0 },
                new[] { 0f,.10f,.19f,.27f,.34f,.40f,.52f,.63f,.73f,.84f,.94f }, 1f,
                new AnimationEvent { time = .4f, functionName = "SkullMonkReleaseWave" },
                new AnimationEvent { time = .99f, functionName = "SkullMonkWaveCycleFinished" });
            var recover = Clip("SkullMonkWaveRecover", plant, new[] {7,6,5,3,1,0},
                new[] {0f,.14f,.30f,.45f,.65f,.85f}, 1f,
                new AnimationEvent { time = .99f, functionName = "SkullMonkWaveRecovered" });
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim + "SkullMonk.controller");
            var machine = controller.layers[0].stateMachine;
            foreach (var clip in new[] { prepare, pulse, recover })
            {
                var state = machine.states.FirstOrDefault(s => s.state.name == clip.name).state;
                if (state == null) state = machine.AddState(clip.name, new Vector3(600, 300 + Array.IndexOf(new[] {prepare,pulse,recover},clip)*90));
                state.motion = clip; EditorUtility.SetDirty(state);
            }
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var boss = all.Single(t => t.name == "Skull").GetComponent<MON_SkullMonk>();
            var data = new SerializedObject(boss);
            data.FindProperty("plantedStaffOffset").floatValue = .65f;
            data.FindProperty("waveArenaBounds").objectReferenceValue = all.Single(t => t.name == "CameraCollider").GetComponent<Collider2D>();
            data.FindProperty("waveFloor").objectReferenceValue = all.Single(t => t.name == "Floor").GetComponent<Collider2D>();
            var sprites = Frames("SkullMonkWaveSmoke.png"); var field = data.FindProperty("staffSmokeFrames"); field.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++) field.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(machine); AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("Skull Monk five-wave ritual configured.");
        }
        static void Copy(string source, string target)
        {
            if (!File.Exists(Art + target)) File.Copy(Art + source, Art + target);
            AssetDatabase.ImportAsset(Art + target);
        }
        public static Sprite[] Frames(string file) => AssetDatabase.LoadAllAssetsAtPath(Art + file).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        static AnimationClip Clip(string name, Sprite[] frames, int[] sequence, float[] times, float duration, params AnimationEvent[] events)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + name + ".anim");
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, Anim + name + ".anim"); }
            clip.name = name; clip.frameRate = 60;
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
                sequence.Select((n, i) => new ObjectReferenceKeyframe { time = times[i], value = frames[n] }).ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.startTime = 0; settings.stopTime = duration; settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AnimationUtility.SetAnimationEvents(clip, events); EditorUtility.SetDirty(clip); return clip;
        }
    }
}
