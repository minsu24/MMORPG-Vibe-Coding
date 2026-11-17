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
    public static class SkullMonkPrayerSetup
    {
        const string Art = SkullMonkWaveSetup.Art, Anim = SkullMonkWaveSetup.Anim;
        [MenuItem("Eastern Fantasy/Skull Monk/Configure Prayer Eruptions")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            Copy("해골 승려 합장 공격 점프.png", "SkullMonkPrayerJump.png");
            Copy("해골 승려 합장 공격.png", "SkullMonkPrayerPrepare.png");
            Copy("해골 승려 합장 공격 기운 반복.png", "SkullMonkPrayerAura.png");
            Copy("합장 공격 폭발 이펙트.png", "SkullMonkPrayerExplosion.png");
            Copy("합장 공격 폭발 소멸.png", "SkullMonkPrayerDissolve.png");
            var binding=EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite");
            var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim+"SkullMonkIdle.anim");
            var reference=(Sprite)AnimationUtility.GetObjectReferenceCurve(idle,binding)[0].value;
            float ppu=320f/reference.bounds.size.y;
            int[] left={0,202,406,622,858,1070,1297,1490,1735,1960};
            int[] right={205,410,617,842,1062,1277,1490,1743,1957,2172};
            float[] center={100,315,520,735,960,1170,1390,1616,1837,2060};
            float[] foot={149,149,151,205,217,267,194,150,150,149};
            var jumpCells=Enumerable.Range(0,10).Select(i=>new Rect(left[i],i>=3&&i<=6?185:135,right[i]-left[i],
                i>=3&&i<=6?420:470)).ToArray();
            SkullMonkMeleeSetup.Slice(Art+"SkullMonkPrayerJump.png",jumpCells,ppu,
                jumpCells.Select((r,i)=>new Vector2((center[i]-r.x)/r.width,
                    (foot[i]-r.y-reference.bounds.min.y*ppu)/r.height)).ToArray(),true);
            var cells=Enumerable.Range(0,8).Select(i=>new Rect(Mathf.Round(i*271.5f),90,
                Mathf.Round((i+1)*271.5f)-Mathf.Round(i*271.5f),510)).ToArray();
            ppu=462f/reference.bounds.size.y;
            SkullMonkMeleeSetup.Slice(Art+"SkullMonkPrayerPrepare.png",cells,ppu,
                cells.Select(r=>new Vector2(144/r.width,(111-r.y-reference.bounds.min.y*ppu)/r.height)).ToArray(),true);
            SkullMonkMeleeSetup.Slice(Art+"SkullMonkPrayerAura.png",cells,ppu,
                cells.Select(r=>new Vector2(144/r.width,(109-r.y-reference.bounds.min.y*ppu)/r.height)).ToArray());
            FullRect(Art+"SkullMonkPrayerAura.png");
            var blastCells=cells.Select(r=>new Rect(r.x,160,r.width,440)).ToArray();
            SkullMonkMeleeSetup.Slice(Art+"SkullMonkPrayerExplosion.png",blastCells,100,
                blastCells.Select(r=>new Vector2(.5f,20/r.height)).ToArray());
            FullRect(Art+"SkullMonkPrayerExplosion.png");
            var fadeCells=Enumerable.Range(0,5).Select(i=>new Rect(Mathf.Round(i*434.4f),130,
                Mathf.Round((i+1)*434.4f)-Mathf.Round(i*434.4f),470)).ToArray();
            float[] fadeCenter={237,660,1090,1512,1953};
            SkullMonkMeleeSetup.Slice(Art+"SkullMonkPrayerDissolve.png",fadeCells,100,
                fadeCells.Select((r,i)=>new Vector2((fadeCenter[i]-r.x)/r.width,23/r.height)).ToArray());
            FullRect(Art+"SkullMonkPrayerDissolve.png");
            var jump=Frames("SkullMonkPrayerJump.png");var prep=Frames("SkullMonkPrayerPrepare.png");var aura=Frames("SkullMonkPrayerAura.png");
            Clip("SkullMonkPrayerJump",jump,Enumerable.Range(0,10).ToArray(),
                new[]{0f,.04f,.085f,MON_SkullMonk.PrayerTakeoffTime,.18f,.26f,MON_SkullMonk.PrayerDropTime,
                    MON_SkullMonk.PrayerLandTime,.86f,1.04f},MON_SkullMonk.PrayerSettleTime,false);
            // Skip the awkward intermediate hand pose and close the palms in one clear gesture.
            Clip("SkullMonkPrayerPrepare",prep,new[]{0,1,2,4,5,6,7},new[]{0f,.12f,.24f,.36f,.47f,.58f,.67f},.74f,false);
            Clip("SkullMonkPrayerAura",aura,Enumerable.Range(0,8).ToArray(),new[]{0f,.1f,.2f,.3f,.4f,.5f,.6f,.7f},.8f,true);
            var recover=Clip("SkullMonkPrayerRecover",prep,new[]{7,5,4,2,1,0},new[]{0f,.10f,.22f,.34f,.46f,.56f},.65f,false);
            var keys=AnimationUtility.GetObjectReferenceCurve(recover,binding).ToList();
            keys.Add(new ObjectReferenceKeyframe{time=.64f,value=reference});AnimationUtility.SetObjectReferenceCurve(recover,binding,keys.ToArray());
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim+"SkullMonk.controller");
            var machine=controller.layers[0].stateMachine;
            foreach(string name in new[]{"SkullMonkPrayerJump","SkullMonkPrayerPrepare","SkullMonkPrayerAura","SkullMonkPrayerRecover"})
            {
                var state=machine.states.FirstOrDefault(s=>s.state.name==name).state;
                if(state==null)state=machine.AddState(name);state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim+name+".anim");EditorUtility.SetDirty(state);
            }
            var scene=SceneManager.GetSceneByPath(SkullMonkWaveSetup.ScenePath);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(SkullMonkWaveSetup.ScenePath,OpenSceneMode.Additive);
            var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var boss=transforms.Single(t=>t.name=="Skull").GetComponent<MON_SkullMonk>();var data=new SerializedObject(boss);
            Assign(data,"prayerExplosionFrames",Frames("SkullMonkPrayerExplosion.png"));Assign(data,"prayerDissolveFrames",Frames("SkullMonkPrayerDissolve.png"));
            SkullMonkPrayerHighResolutionSetup.AssignFrames(data);
            data.FindProperty("prayerLeftWall").objectReferenceValue=transforms.Single(t=>t.name=="Wall").GetComponent<Collider2D>();
            data.FindProperty("prayerRightWall").objectReferenceValue=transforms.Single(t=>t.name=="Wall (1)").GetComponent<Collider2D>();
            data.FindProperty("prayerJumpHeight").floatValue=16f;
            data.FindProperty("prayerExplosionHeight").floatValue=18f;
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(controller);EditorUtility.SetDirty(machine);EditorUtility.SetDirty(recover);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        static void Copy(string from,string to){if(!File.Exists(Art+to))File.Copy(Art+from,Art+to);AssetDatabase.ImportAsset(Art+to);}
        static Sprite[] Frames(string file)=>SkullMonkWaveSetup.Frames(file);
        static void Assign(SerializedObject data,string name,Sprite[] sprites){var field=data.FindProperty(name);field.arraySize=sprites.Length;for(int i=0;i<sprites.Length;i++)field.GetArrayElementAtIndex(i).objectReferenceValue=sprites[i];}
        static void FullRect(string path){var importer=(TextureImporter)AssetImporter.GetAtPath(path);var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();}
        static AnimationClip Clip(string name,Sprite[] frames,int[] order,float[] times,float duration,bool loop)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim+name+".anim");if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,Anim+name+".anim");}
            clip.name=name;clip.frameRate=60;AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),
                order.Select((n,i)=>new ObjectReferenceKeyframe{time=times[i],value=frames[n]}).ToArray());
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.startTime=0;settings.stopTime=duration;settings.loopTime=loop;
            AnimationUtility.SetAnimationClipSettings(clip,settings);AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());EditorUtility.SetDirty(clip);return clip;
        }
    }
}
