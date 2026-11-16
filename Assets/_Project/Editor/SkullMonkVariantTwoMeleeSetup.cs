using System;
using System.IO;
using System.Linq;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class SkullMonkVariantTwoMeleeSetup
    {
        [MenuItem("Eastern Fantasy/Skull Monk/Apply Variant Two Melee")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            string art = SkullMonkWaveSetup.Art, anim = SkullMonkWaveSetup.Anim;
            string path = art + "SkullMonkMelee_Variant2.png";
            File.Copy(art + "해골승려 기본공격2안.png", path, true);
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(anim + "SkullMonkIdle.anim");
            var reference = (Sprite)AnimationUtility.GetObjectReferenceCurve(idle, binding)[0].value;
            float ppu = 340f / reference.bounds.size.y;
            var cells = Enumerable.Range(0, 8).Select(i => new Rect(i % 4 * 543, i < 4 ? 362 : 0, 543, 362)).ToArray();
            float[] heel = {173,139,104,82,78,38,21,8};
            float[] sole = {16,16,16,16,20,20,20,19};
            // Preserve idle proportions and the planted rear heel throughout the lunge.
            SkullMonkMeleeSetup.Slice(path, cells, ppu, cells.Select((r,i) =>
                new Vector2((heel[i] + .68f * ppu) / r.width, (sole[i] - reference.bounds.min.y * ppu) / r.height)).ToArray(), true);
            var sprites = SkullMonkWaveSetup.Frames("SkullMonkMelee_Variant2.png");
            int[] order = {0,1,2,3,4,5,6,7,2,1,0};
            float[] times = {0f,.08f,.16f,.23f,.37f,.39f,.41f,.43f,.65f,.79f,.91f};
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(anim + "SkullMonkAttack.anim");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, order.Select((n,i) =>
                new ObjectReferenceKeyframe {time=times[i],value=sprites[n]}).ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.startTime=0; settings.stopTime=1.03f; settings.loopTime=false;
            AnimationUtility.SetAnimationClipSettings(clip,settings);
            AnimationUtility.SetAnimationEvents(clip,new[]{
                new AnimationEvent{time=.37f,functionName="SkullMonkPlantStep"},
                new AnimationEvent{time=.43f,functionName="SkullMonkThrust"},
                new AnimationEvent{time=1.02f,functionName="SkullMonkAttackFinished"}});
            EditorUtility.SetDirty(clip);

            string prefabPath = SkullMonkBossHealthSetup.PrefabPath;
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var panel = root.GetComponent<SkullMonkBossHealthUI>().panel;
                panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,1f);
                panel.anchoredPosition=new Vector2(0,panel.sizeDelta.y*23f/524f-8f);
                PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }
    }
}
