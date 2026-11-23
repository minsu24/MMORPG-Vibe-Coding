using System;
using System.Linq;
using EasternFantasy.Shop;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class MerchantPipeSmokeSetup
    {
        private const string Art = TempleGateBlacksmithSetup.Art;
        private const string SmokePath = Art + "곰방대 연기.png";

        [MenuItem("Eastern Fantasy/Old Temple/Configure Detached Merchant Pipe Smoke")]
        public static string Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before saving the NPC.");
            var scene = SceneManager.GetSceneByPath(TempleGateMerchantSetup.ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(TempleGateMerchantSetup.ScenePath, OpenSceneMode.Additive);
            try
            {
                var npc = scene.GetRootGameObjects().Single(g => g.name == TempleGateMerchantSetup.ObjectName);
                var frames = AssetDatabase.LoadAllAssetsAtPath(MerchantPipeTransitionSetup.PipeSheet)
                    .OfType<Sprite>().OrderBy(s => s.name).ToArray();
                var data = new SerializedObject(npc.GetComponent<MonkeyMerchantAmbientAnimation>());
                Configure(data, frames[MerchantPipeTransitionSetup.ExhalePoseIndex]);
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save TempleGate.");
                Selection.activeGameObject = npc;
                return "The smoke plays all eight frames while drifting up-right from the exhale pose.";
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        public static void Configure(SerializedObject data, Sprite exhalePose)
        {
            var frames = AssetDatabase.LoadAllAssetsAtPath(SmokePath).OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
            if (frames.Length != 8) throw new InvalidOperationException("Expected eight frames in " + SmokePath);
            var cloud = frames[0];
            // Register the smoke's trailing tip with the lips in the clean exhale sprite.
            var mouthPixels = new Vector2(1415f, 422f) - exhalePose.rect.position;
            var tailPixels = new Vector2(23f, 17f);
            var mouth = (mouthPixels - exhalePose.pivot) / exhalePose.pixelsPerUnit;
            const float cloudWidth = .93f;
            var size = new Vector2(cloudWidth, cloudWidth * cloud.rect.height / cloud.rect.width);
            var tailToPivot = (cloud.pivot - tailPixels) * (cloudWidth / cloud.rect.width);

            data.FindProperty("smokeSprite").objectReferenceValue = cloud;
            data.FindProperty("smokeExhaleFrame").objectReferenceValue = exhalePose;
            var animation = data.FindProperty("smokeFrames");
            animation.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) animation.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            data.FindProperty("smokeMouthLocalOffset").vector2Value = mouth;
            data.FindProperty("smokeLocalOffset").vector2Value = mouth + tailToPivot;
            data.FindProperty("smokeLocalSize").vector2Value = size;
            data.FindProperty("smokeDrift").vector2Value = new Vector2(.63f, .66f);
            data.FindProperty("smokeLifetime").floatValue = 2.35f;
        }
    }
}
