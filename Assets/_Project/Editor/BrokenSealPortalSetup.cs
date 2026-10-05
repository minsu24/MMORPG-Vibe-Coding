using System;
using System.Linq;
using EasternFantasy.Dialogue;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class BrokenSealPortalSetup
    {
        private const string AnimPath = "Assets/_Project/Animations/PortalSeal/";
        private const string PrefabPath = "Assets/_Project/Prefabs/World/PortalSeal.prefab";
        [MenuItem("Eastern Fantasy/Setup Broken Seal Portal")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode before setup.");
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "BrokenSeal") throw new InvalidOperationException("Open BrokenSeal before setup.");
            var portal = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PortalManager>(true))
                .First(p => p.name.Trim() == "Out_BrokenSeal_Portal");
            var trigger = scene.GetRootGameObjects().First(g => g.name == "GoldFrogDialogue").GetComponent<DialogueTrigger>();
            var dialogue = new SerializedObject(trigger).FindProperty("sequence").objectReferenceValue as DialogueSequence;
            if (dialogue == null) throw new InvalidOperationException("GoldFrog dialogue is missing.");
            var lockedSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/동양풍 신비의 사슬 자물쇠.png").OfType<Sprite>().First();
            var frames = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/사슬 봉인 해제와 붕괴 효과 8프레임.png")
                .OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (frames.Length != 8) throw new InvalidOperationException("Exactly eight unlock sprites are required.");
            float portalWidth = portal.GetComponent<SpriteRenderer>().sprite.bounds.size.x;
            float lockedScale = portalWidth / lockedSprite.bounds.size.x;
            float unlockScale = portalWidth / frames.Max(frame => frame.bounds.size.x);
            var locked = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimPath + "PortalLocked.anim");
            if (locked == null)
            {
                locked = new AnimationClip { name = "PortalLocked", frameRate = 6 };
                AnimationUtility.SetObjectReferenceCurve(locked, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
                    new[] {new ObjectReferenceKeyframe {time = 0, value = lockedSprite}, new ObjectReferenceKeyframe {time = 0.1f, value = lockedSprite}});
                Scale(locked, lockedScale);
                locked.SetCurve("", typeof(SpriteRenderer), "m_Color.a", AnimationCurve.Constant(0, 0.1f, 1));
                AssetDatabase.CreateAsset(locked, AnimPath + "PortalLocked.anim");
            }
            var unlock = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimPath + "PortalUnlock.anim");
            if (unlock == null)
            {
                unlock = new AnimationClip {name = "PortalUnlock", frameRate = 6};
                AnimationUtility.SetObjectReferenceCurve(unlock, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
                    frames.Select((s, i) => new ObjectReferenceKeyframe {time = i / 6f, value = s}).ToArray());
                Scale(unlock, unlockScale);
                unlock.SetCurve("", typeof(SpriteRenderer), "m_Color.a",
                    new AnimationCurve(new Keyframe(0, 1), new Keyframe(7f / 6f, 1), new Keyframe(1.65f, 0)));
                AssetDatabase.CreateAsset(unlock, AnimPath + "PortalUnlock.anim");
            }
            Scale(locked, lockedScale);
            Scale(unlock, unlockScale);
            EditorUtility.SetDirty(locked);
            EditorUtility.SetDirty(unlock);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimPath + "PortalSeal.controller");
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(AnimPath + "PortalSeal.controller");
                var stateMachine = controller.layers[0].stateMachine;
                var lockedState = stateMachine.AddState("Locked");
                lockedState.motion = locked;
                stateMachine.defaultState = lockedState;
                stateMachine.AddState("Unlock").motion = unlock;
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                var root = new GameObject("PortalSeal", typeof(SpriteRenderer), typeof(Animator));
                try
                {
                    root.transform.localScale = Vector3.one * lockedScale;
                    var renderer = root.GetComponent<SpriteRenderer>();
                    renderer.sprite = lockedSprite;
                    renderer.sortingLayerID = portal.GetComponent<SpriteRenderer>().sortingLayerID;
                    renderer.sortingOrder = portal.GetComponent<SpriteRenderer>().sortingOrder + 5;
                    renderer.sharedMaterial = portal.GetComponent<SpriteRenderer>().sharedMaterial;
                    var animator = root.GetComponent<Animator>();
                    animator.runtimeAnimatorController = controller;
                    animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }
                finally {UnityEngine.Object.DestroyImmediate(root);}
            }
            else
            {
                var root = PrefabUtility.LoadPrefabContents(PrefabPath);
                try
                {
                    root.transform.localScale = Vector3.one * lockedScale;
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            var seal = portal.transform.Find("PortalSeal");
            if (seal == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.SetParent(portal.transform, false);
                instance.transform.localPosition = Vector3.zero;
                seal = instance.transform;
            }
            seal.localScale = Vector3.one * lockedScale;
            var sequence = portal.GetComponent<LockedPortalSequence>();
            if (sequence == null) sequence = portal.gameObject.AddComponent<LockedPortalSequence>();
            var background = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SpriteRenderer>())
                .OrderByDescending(r => r.bounds.size.x * r.bounds.size.y).First();
            sequence.Configure(dialogue, seal.GetComponent<Animator>(), seal.GetComponent<SpriteRenderer>(), unlock, background);
            var data = new SerializedObject(portal);
            data.FindProperty("startsLocked").boolValue = true;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sequence);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("BrokenSeal exit is sealed until the GoldFrog dialogue finishes.");
        }
        private static void Scale(AnimationClip clip, float scale)
        {
            foreach (var axis in new[] {"x", "y", "z"})
                clip.SetCurve("", typeof(Transform), "m_LocalScale." + axis, AnimationCurve.Constant(0, 0.1f, scale));
        }
    }
}
