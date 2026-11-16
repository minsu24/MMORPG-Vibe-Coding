using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class SkullMonkDashSetup
    {
        [MenuItem("Eastern Fantasy/Skull Monk/Configure Dash Melee")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            string art=SkullMonkWaveSetup.Art, anim=SkullMonkWaveSetup.Anim;
            string path=art+"SkullMonkDash.png";
            if(!File.Exists(path))File.Copy(art+"해골 승려 원거리 대시.png",path);
            var binding=EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite");
            var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(anim+"SkullMonkIdle.anim");
            var reference=(Sprite)AnimationUtility.GetObjectReferenceCurve(idle,binding)[0].value;
            float ppu=333f/reference.bounds.size.y;
            var cells=new[]{new Rect(10,150,225,350),new Rect(240,150,300,350),new Rect(505,150,340,350),
                new Rect(795,150,335,350),new Rect(1330,150,265,350),new Rect(1590,150,340,350),new Rect(1940,150,232,350)};
            float[] centers={118,171,188,205,160,173,116};
            SkullMonkMeleeSetup.Slice(path,cells,ppu,cells.Select((r,i)=>new Vector2(centers[i]/r.width,
                (158-r.y-reference.bounds.min.y*ppu)/r.height)).ToArray(),true);
            var frames=SkullMonkWaveSetup.Frames("SkullMonkDash.png");
            // Source pose 5 is an intentional disappearance, not the previous frame's trailing pixels.
            var blank=AssetDatabase.LoadAssetAtPath<Sprite>(art+"SkullMonkDashInvisible.asset");
            if(blank==null)
            {
                blank=Sprite.Create(AssetDatabase.LoadAssetAtPath<Texture2D>(path),new Rect(1200,170,30,330),
                    new Vector2(.5f,.5f),ppu,0,SpriteMeshType.FullRect);
                blank.name="SkullMonkDashInvisible";AssetDatabase.CreateAsset(blank,art+"SkullMonkDashInvisible.asset");
            }
            var poses=new[]{frames[0],frames[1],frames[2],frames[3],blank,frames[4],frames[5],frames[6]};
            float[] times={0f,.05f,.10f,.17f,.25f,.34f,.40f,.46f};
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(anim+"SkullMonkDash.anim");
            if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,anim+"SkullMonkDash.anim");}
            clip.name="SkullMonkDash";clip.frameRate=60;
            AnimationUtility.SetObjectReferenceCurve(clip,binding,poses.Select((s,i)=>new ObjectReferenceKeyframe{time=times[i],value=s}).ToArray());
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;settings.startTime=0;settings.stopTime=.5f;
            AnimationUtility.SetAnimationClipSettings(clip,settings);AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(anim+"SkullMonk.controller");
            var machine=controller.layers[0].stateMachine;
            var state=machine.states.FirstOrDefault(s=>s.state.name==clip.name).state;
            if(state==null)state=machine.AddState(clip.name);state.motion=clip;
            EditorUtility.SetDirty(clip);EditorUtility.SetDirty(state);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(SkullMonkWaveSetup.ScenePath);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(SkullMonkWaveSetup.ScenePath,OpenSceneMode.Additive);
            var boss=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MON_SkullMonk>(true)).Single();
            var data=new SerializedObject(boss);data.FindProperty("dashMaxRange").floatValue=14f;
            data.FindProperty("dashStopDistance").floatValue=2.8f;data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
    }
}
