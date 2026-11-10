using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class SkullMonkWalkInbetweens
    {
        public const string Sheet="Assets/_Project/Art/Old Temple/SkullMonkWalk_Inbetweens_06_10.png";
        public const string Backup="Assets/_Project/Animations/Old Temple/Backups/SkullMonkWalk_BeforeInbetweens";
        static readonly EditorCurveBinding Binding=EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite");
        static readonly string[] Starts={"SkullMonkWalk12_06","SkullMonkWalk12_07","SkullMonkWalk12_08"};
        static readonly string[] Ends={"SkullMonkWalk12_07","SkullMonkWalk12_08","SkullMonkWalk12_10"};

        [MenuItem("Eastern Fantasy/Monsters/Add Skull Walk Inbetweens 06 To 10")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
            if(AnimationMode.InAnimationMode())AnimationMode.StopAnimationMode();
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(SkullMonkWalkSetup.ClipPath);
            if(!AssetDatabase.IsValidFolder(Backup))AssetDatabase.CreateFolder("Assets/_Project/Animations/Old Temple/Backups","SkullMonkWalk_BeforeInbetweens");
            var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(Backup+"/Walk.anim");
            if(saved==null){saved=UnityEngine.Object.Instantiate(clip);saved.name="Walk";AssetDatabase.CreateAsset(saved,Backup+"/Walk.anim");}
            var original=AnimationUtility.GetObjectReferenceCurve(saved,Binding);
            var first=(Sprite)original.First(k=>k.value.name==Starts[0]).value;
            var sprites=Slice(first);
            var keys=new List<ObjectReferenceKeyframe>(original);
            for(int pair=0;pair<3;pair++)
            {
                int at=Array.FindIndex(original,k=>k.value.name==Starts[pair]);
                int next=(at+1)%original.Length;
                if(at<0 || original[next].value.name!=Ends[pair])throw new Exception("The original sprite order does not match the requested transition.");
                float start=original[at].time,end=next==0?saved.length:original[next].time;
                for(int step=1;step<=2;step++)keys.Add(new ObjectReferenceKeyframe{time=Mathf.Lerp(start,end,step/3f),value=sprites[pair*2+step-1]});
            }
            Undo.RecordObject(clip,"Insert generated Skull walk inbetweens");
            clip.frameRate=saved.frameRate*3f;
            AnimationUtility.SetObjectReferenceCurve(clip,Binding,keys.OrderBy(k=>k.time).ToArray());
            var settings=AnimationUtility.GetAnimationClipSettings(saved);
            AnimationUtility.SetAnimationClipSettings(clip,settings);
            EditorUtility.SetDirty(clip);AssetDatabase.SaveAssets();
        }

        [MenuItem("Eastern Fantasy/Monsters/Restore Skull Walk Before Inbetweens")]
        public static void Restore()
        {
            if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
            var source=AssetDatabase.LoadAssetAtPath<AnimationClip>(Backup+"/Walk.anim");
            if(source==null)throw new Exception("Previous animation backup is missing.");
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(SkullMonkWalkSetup.ClipPath);
            string name=clip.name;EditorUtility.CopySerialized(source,clip);clip.name=name;
            EditorUtility.SetDirty(clip);AssetDatabase.SaveAssets();
        }

        static Sprite[] Slice(Sprite reference)
        {
            AssetDatabase.ImportAsset(Sheet);
            var importer=(TextureImporter)AssetImporter.GetAtPath(Sheet);
            var raw=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                ImageConversion.LoadImage(raw,File.ReadAllBytes(Sheet));
                if(raw.width%6!=0)throw new Exception("Sheet must have six equal-width cells.");
                int width=raw.width/6,height=raw.height;
                var pixels=raw.GetPixels32();var bottoms=new int[6];var tops=new int[6];var heads=new float[6];
                for(int i=0;i<6;i++)
                {
                    int bottom=height,top=0;double sum=0,n=0;
                    for(int y=0;y<height;y++)for(int x=i*width;x<(i+1)*width;x++)
                    {
                        var p=pixels[y*raw.width+x];if(p.a<128)continue;
                        bottom=Math.Min(bottom,y);top=Math.Max(top,y);
                        float localX=(x-i*width)/(float)width,fromTop=(height-1-y)/(float)height;
                        if(localX>.45f && localX<.75f && fromTop>.18f && fromTop<.30f && p.r>145 && p.g>120 && p.b>85)
                        {sum+=x+.5;n++;}
                    }
                    if(n<10 || top<=bottom)throw new Exception("Incomplete generated pose "+i);
                    bottoms[i]=bottom;tops[i]=top;heads[i]=(float)(sum/n);
                }
                var original=SkullMonkStaggeredSlices.VisibleBounds(reference);
                float ppu=(tops[0]-bottoms[0]+1)/original.height;
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit=ppu;importer.maxTextureSize=4096;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
                var factories=new SpriteDataProviderFactories();factories.Init();
                var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
                var previous=provider.GetSpriteRects().ToDictionary(r=>r.name,r=>r.spriteID);
                var rects=new SpriteRect[6];var pairs=new SpriteNameFileIdPair[6];
                for(int i=0;i<6;i++)
                {
                    string name="SkullMonkBridge_"+Starts[i/2].Substring(16)+"_"+Ends[i/2].Substring(16)+"_"+(i%2+1);
                    var id=previous.TryGetValue(name,out var existing)?existing:GUID.Generate();
                    rects[i]=new SpriteRect{name=name,spriteID=id,rect=new Rect(i*width,0,width,height),alignment=SpriteAlignment.Custom,
                        pivot=new Vector2((heads[i]-i*width)/width,(bottoms[i]-original.yMin*ppu)/height)};
                    pairs[i]=new SpriteNameFileIdPair(name,id);
                }
                provider.SetSpriteRects(rects);provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
                provider.Apply();importer.SaveAndReimport();
            }
            finally{UnityEngine.Object.DestroyImmediate(raw);}
            return AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        }

        public static string Validate()
        {
            if(!Application.isPlaying)throw new Exception("Run playback validation in Play Mode.");
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(SkullMonkWalkSetup.ClipPath);
            var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(Backup+"/Walk.anim");
            var keys=AnimationUtility.GetObjectReferenceCurve(clip,Binding);var old=AnimationUtility.GetObjectReferenceCurve(saved,Binding);
            var report=new List<string>();
            Action<bool,string> check=(value,message)=>{if(!value)throw new Exception(message);report.Add("PASS "+message);};
            check(keys.Length==old.Length+6,"Exactly six intermediate keys added");
            check(Mathf.Abs(clip.length-saved.length)<.001f,"Original 1.5 second duration retained");
            foreach(var key in old)check(keys.Any(k=>k.value==key.value && Mathf.Abs(k.time-key.time)<.0001f),"Original pose and time retained: "+key.value.name);
            check(AnimationUtility.GetAnimationClipSettings(clip).loopTime,"Looping enabled");
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var go=new GameObject("Skull inbetween validation",typeof(SpriteRenderer),typeof(Animator));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
                var animator=go.GetComponent<Animator>();animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SkullMonkWalkSetup.ControllerPath);
                animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.Update(0);
                foreach(var key in keys)
                {
                    animator.Play("Base Layer.SkullMonkWalk",0,(key.time+.005f)/clip.length);animator.Update(0);
                    check(go.GetComponent<SpriteRenderer>().sprite==key.value,"Animator renders "+key.value.name);
                    check(go.transform.position==Vector3.zero && go.transform.localScale==Vector3.one,"Transform remains fixed at "+key.time);
                }
                animator.Play("Base Layer.SkullMonkWalk",0,1.001f);animator.Update(0);
                check(go.GetComponent<SpriteRenderer>().sprite==old[0].value,"Interpolated final segment loops to original first pose");
                for(int pair=0;pair<3;pair++)
                {
                    int start=Array.FindIndex(keys,k=>k.value.name==Starts[pair]);
                    check(AssetDatabase.GetAssetPath(keys[start+1].value)==Sheet && AssetDatabase.GetAssetPath(keys[start+2].value)==Sheet,
                        "Two new poses follow "+Starts[pair]);
                    check(keys[(start+3)%keys.Length].value.name==Ends[pair],"Transition ends at "+Ends[pair]);
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
            Directory.CreateDirectory("Artifacts/SkullMonkWalkInbetweens");
            File.WriteAllLines("Artifacts/SkullMonkWalkInbetweens/Validation.txt",report.Concat(new[]{"COMPLETE "+report.Count+" checks passed"}));
            return report.Count+" checks passed";
        }
    }
}
