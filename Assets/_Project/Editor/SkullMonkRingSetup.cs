using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class SkullMonkRingSetup
    {
        public const string Art=SkullMonkWaveSetup.Art;
        public const string Anim=SkullMonkWaveSetup.Anim;
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim+"SkullMonkIdle.anim");
            var binding=EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite");
            var reference=(Sprite)AnimationUtility.GetObjectReferenceCurve(idle,binding)[0].value;
            Copy("해골 승려 석장 고리 공격.png","SkullMonkRingCharge.png");
            Copy("해골 승려 석장 고리 공격2.png","SkullMonkRingGesture.png");
            Copy("석장 고리 회전.png","SkullMonkRingSpin.png");
            Copy("석장 고리 발사.png","SkullMonkRingFlight.png");
            var cells=Enumerable.Range(0,8).Select(i=>new Rect(Mathf.Round(i*271.5f),0,
                Mathf.Round((i+1)*271.5f)-Mathf.Round(i*271.5f),724)).ToArray();
            float ppu=408f/reference.bounds.size.y;
            float[] heels={87,81,80,71,67,60,55,50};
            SkullMonkMeleeSetup.Slice(Art+"SkullMonkRingCharge.png",cells,ppu,
                cells.Select((r,i)=>new Vector2((heels[i]+.68f*ppu)/r.width,(149-reference.bounds.min.y*ppu)/r.height)).ToArray());
            KeepChargeHalo();
            var gestures=Enumerable.Range(0,12).Select(i=>new Rect(i%6*362,i<6?362:0,362,362)).ToArray();
            ppu=344f/reference.bounds.size.y;
            SkullMonkMeleeSetup.Slice(Art+"SkullMonkRingGesture.png",gestures,ppu,
                gestures.Select(r=>new Vector2((120+.68f*ppu)/r.width,(10-reference.bounds.min.y*ppu)/r.height)).ToArray(),true);
            var spinCells=cells.Select(r=>new Rect(r.x,240,r.width,240)).ToArray();
            float[] cx={138,127,124,123,120,117,118,125};
            float[] cy={362,363,363,364,365,365,365,369};
            SkullMonkMeleeSetup.Slice(Art+"SkullMonkRingSpin.png",spinCells,400,
                spinCells.Select((r,i)=>new Vector2(cx[i]/r.width,(724-cy[i]-r.y)/r.height)).ToArray());
            var flyCells=cells.Select(r=>new Rect(r.x,200,r.width,340)).ToArray();
            float[] fx={125,108,97,86,129,107,119,124};
            float[] fy={365,359,353,346,325,336,349,353};
            SkullMonkMeleeSetup.Slice(Art+"SkullMonkRingFlight.png",flyCells,300,
                flyCells.Select((r,i)=>new Vector2(fx[i]/r.width,(724-fy[i]-r.y)/r.height)).ToArray());
            var charge=Frames("SkullMonkRingCharge.png");var gesture=Frames("SkullMonkRingGesture.png");
            MakeClip("SkullMonkRingCharge",charge,new[]{0,1,2,3,4,5,6,7},new[]{0f,.16f,.32f,.48f,.64f,.8f,.96f,1.12f},1.32f,false,
                new AnimationEvent{time=1.31f,functionName="SkullMonkRingChargeFinished"});
            // Source frame 4 (zero-based index 3) has a third hand: never reference it.
            MakeClip("SkullMonkRingLift",gesture,new[]{0,1,2,4,5,6,7},new[]{0f,.16f,.32f,.48f,.64f,.8f,.96f},1.15f,false,
                new AnimationEvent{time=.48f,functionName="SkullMonkLiftRings"},
                new AnimationEvent{time=1.14f,functionName="SkullMonkRingHoldReady"});
            MakeClip("SkullMonkRingHold",gesture,new[]{7,8,7},new[]{0f,.35f,.7f},1f,true);
            var recover=MakeClip("SkullMonkRingRecover",gesture,new[]{9,10,11,0},new[]{0f,.16f,.32f,.5f},.9f,false,
                new AnimationEvent{time=.89f,functionName="SkullMonkRingRecovered"});
            var keys=AnimationUtility.GetObjectReferenceCurve(recover,binding).ToList();
            keys.Add(new ObjectReferenceKeyframe{time=.72f,value=reference});AnimationUtility.SetObjectReferenceCurve(recover,binding,keys.ToArray());
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim+"SkullMonk.controller");
            var machine=controller.layers[0].stateMachine;
            string[] names={"SkullMonkRingCharge","SkullMonkRingLift","SkullMonkRingHold","SkullMonkRingRecover"};
            for(int i=0;i<names.Length;i++)
            {
                var state=machine.states.FirstOrDefault(s=>s.state.name==names[i]).state;
                if(state==null)state=machine.AddState(names[i],new Vector3(850,120+i*90));
                state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim+names[i]+".anim");EditorUtility.SetDirty(state);
            }
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(SkullMonkWaveSetup.ScenePath);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(SkullMonkWaveSetup.ScenePath,OpenSceneMode.Additive);
            var boss=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MON_SkullMonk>(true)).Single();
            var data=new SerializedObject(boss);Assign(data,"rotatingRingFrames",Frames("SkullMonkRingSpin.png"));
            Assign(data,"flyingRingFrames",Frames("SkullMonkRingFlight.png"));data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);EditorUtility.SetDirty(machine);EditorUtility.SetDirty(recover);AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("Four-ring pattern configured; gesture frame 4 excluded.");
        }
        static void Assign(SerializedObject obj,string field,Sprite[] frames)
        {
            var property=obj.FindProperty(field);property.arraySize=frames.Length;
            for(int i=0;i<frames.Length;i++)property.GetArrayElementAtIndex(i).objectReferenceValue=frames[i];
        }
        static void Copy(string src,string dst){if(!File.Exists(Art+dst))File.Copy(Art+src,Art+dst);AssetDatabase.ImportAsset(Art+dst);}
        public static Sprite[] Frames(string name)=>AssetDatabase.LoadAllAssetsAtPath(Art+name).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        static AnimationClip MakeClip(string name,Sprite[] frames,int[] sequence,float[] times,float duration,bool loop,params AnimationEvent[] events)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim+name+".anim");
            if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,Anim+name+".anim");}
            clip.name=name;clip.frameRate=60;
            AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),
                sequence.Select((n,i)=>new ObjectReferenceKeyframe{time=times[i],value=frames[n]}).ToArray());
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.startTime=0;settings.stopTime=duration;settings.loopTime=loop;
            AnimationUtility.SetAnimationClipSettings(clip,settings);AnimationUtility.SetAnimationEvents(clip,events);EditorUtility.SetDirty(clip);return clip;
        }
        static void KeepChargeHalo()
        {
            string path=Art+"SkullMonkRingCharge.png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            var raw=new Texture2D(2,2);ImageConversion.LoadImage(raw,File.ReadAllBytes(path));
            try
            {
                var method=typeof(UnityEditor.Sprites.SpriteUtility).GetMethods(System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static)
                    .Single(m=>m.Name=="GenerateOutline"&&m.GetParameters().Length==7);
                var outlines=provider.GetDataProvider<ISpriteOutlineDataProvider>();
                foreach(var rect in provider.GetSpriteRects())
                {
                    object[] args={raw,rect.rect,.75f,(byte)64,false,null,false};method.Invoke(null,args);
                    var keep=((Vector2[][])args[5]).Where(p=>p.Length>=3 && p.Average(v=>v.x)+rect.rect.width*.5f>14).ToList();
                    outlines.SetOutlines(rect.spriteID,keep);
                }
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.Tight;importer.SetTextureSettings(settings);
                provider.Apply();importer.SaveAndReimport();
            }
            finally{UnityEngine.Object.DestroyImmediate(raw);}
        }
    }
}
