using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class SkullMonkWalk12Validation
    {
        const string Folder="Artifacts/SkullMonkWalk12";
        static readonly EditorCurveBinding Binding=EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite");
        static string report;
        static int checks;
        static void Check(bool value,string message)
        {
            report+=(value?"PASS ":"FAIL ")+message+"\n";
            if(!value)throw new Exception(message);
            checks++;
        }

        public static string ValidateRestore(bool staggered=false,bool longStride=false)
        {
            string backup=longStride?SkullMonkWalk12Setup.LongStrideBackup:staggered?SkullMonkWalk12Setup.StaggeredBackup:SkullMonkWalk12Setup.Backup;
            var original=AssetDatabase.LoadAssetAtPath<AnimationClip>(backup+"/Walk.anim");
            var old=AnimationUtility.GetObjectReferenceCurve(original,Binding);
            var idle=File.ReadAllBytes("Assets/_Project/Animations/Old Temple/SkullMonkIdle.anim");
            try
            {
                if(longStride)SkullMonkWalk12Setup.RestoreLongStride();else if(staggered)SkullMonkWalk12Setup.RestoreStaggered();else SkullMonkWalk12Setup.Restore();
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(SkullMonkWalkSetup.ClipPath);
                var restored=AnimationUtility.GetObjectReferenceCurve(clip,Binding);
                if(restored.Length!=old.Length || !restored.Zip(old,(a,b)=>a.time==b.time && a.value==b.value).All(v=>v))
                    throw new Exception("Previous walk restore mismatch.");
            }
            finally{if(longStride)SkullMonkWalk12Setup.ApplyLongStride();else if(staggered)SkullMonkWalk12Setup.ApplyStaggered();else SkullMonkWalk12Setup.Apply();}
            if(!idle.SequenceEqual(File.ReadAllBytes("Assets/_Project/Animations/Old Temple/SkullMonkIdle.anim")))throw new Exception("Idle clip changed.");
            return "Previous "+old.Length+"-frame walk restored exactly; latest frames reapplied; Skeleton idle preserved.";
        }

        public static string Run(bool staggered=false,bool longStride=false)
        {
            string folder=longStride?"Artifacts/SkullMonkLongStride12":staggered?"Artifacts/SkullMonkStaggered8":Folder;
            string sheet=longStride?SkullMonkWalk12Setup.LongStrideSheet:staggered?SkullMonkWalk12Setup.StaggeredSheet:SkullMonkWalk12Setup.Sheet;
            string backup=longStride?SkullMonkWalk12Setup.LongStrideBackup:staggered?SkullMonkWalk12Setup.StaggeredBackup:SkullMonkWalk12Setup.Backup;
            int frameCount=staggered?8:12;
            int columns=staggered?4:6;
            if(!Application.isPlaying)throw new Exception("Run in Play Mode.");
            report="Native Unity "+frameCount+"-frame walk verification\n";checks=0;
            var scene=EditorSceneManager.NewPreviewScene();
            var raw=new Texture2D(2,2,TextureFormat.RGBA32,false);
            var preview=new Texture2D(columns*256,1024,TextureFormat.RGBA32,false);
            RenderTexture target=null;
            Camera previewCamera=null;
            var previous=RenderTexture.active;
            try
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(SkullMonkWalkSetup.ClipPath);
                var frames=AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
                var keys=AnimationUtility.GetObjectReferenceCurve(clip,Binding);
                Check(frames.Length==frameCount && keys.Length==frameCount && keys.Select(k=>k.value).Distinct().Count()==frameCount,"Distinct sprites and keys: "+frameCount);
                Check(clip.frameRate==6 && Mathf.Abs(clip.length-frameCount/6f)<.001f && AnimationUtility.GetAnimationClipSettings(clip).loopTime,"Slow six-FPS loop with correct duration");
                Check(AnimationUtility.GetCurveBindings(clip).Length==0,"No animation curves move or scale the actor");
                ImageConversion.LoadImage(raw,File.ReadAllBytes(sheet));
                Check(raw.GetPixel(0,raw.height-1).a==0 && raw.GetPixel(0,0).a<=1.01f/255f,"Generated sheet retains transparent alpha (one-level edge residue allowed)");
                if(longStride)
                {
                    var original=AssetDatabase.LoadAssetAtPath<AnimationClip>(backup+"/Walk.anim");
                    var reference=(Sprite)AnimationUtility.GetObjectReferenceCurve(original,Binding)[0].value;
                    var before=SkullMonkStaggeredSlices.VisibleBounds(reference);
                    var after=SkullMonkStaggeredSlices.VisibleBounds(frames[0]);
                    Check(Mathf.Abs(before.height-after.height)<.001f,"Visible character height preserved from previous walk");
                    Check(Mathf.Abs(before.yMin-after.yMin)<.001f,"Visible ground anchor preserved from previous walk");
                }
                var pixels=raw.GetPixels32();
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(backup+"/Skull.prefab"),scene);
                root.transform.position=Vector3.zero;root.transform.rotation=Quaternion.identity;root.transform.localScale=Vector3.one;
                var animator=root.GetComponent<Animator>();
                animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SkullMonkWalkSetup.ControllerPath);
                animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.Update(0);
                var renderer=root.GetComponent<SpriteRenderer>();
                var cameraObject=new GameObject("Walk preview camera",typeof(Camera));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
                var camera=cameraObject.GetComponent<Camera>();
                previewCamera=camera;
                camera.transform.position=new Vector3(-.35f,.1f,-10);camera.orthographic=true;camera.orthographicSize=3.6f;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.12f,.12f,1);
                camera.scene=scene;
                target=new RenderTexture(256,512,24);target.Create();camera.targetTexture=target;
                float ground=float.NaN;
                for(int i=0;i<frameCount;i++)
                {
                    Check(keys[i].value==frames[i] && Mathf.Abs(keys[i].time-i/6f)<.001f,"Chronological top-row then bottom-row frame "+i);
                    animator.Play("Base Layer.SkullMonkWalk",0,(i+.05f)/frameCount);animator.Update(0);
                    Check(renderer.sprite==frames[i] && root.transform.position==Vector3.zero && root.transform.localScale==Vector3.one,"Animator displays frame "+i+" without transform drift");
                    var rect=frames[i].rect;int bottom=9999;
                    for(int y=(int)rect.yMin;y<(int)rect.yMax;y++)for(int x=(int)rect.xMin;x<(int)rect.xMax;x++)
                        if(pixels[y*raw.width+x].a>=128)bottom=Math.Min(bottom,y);
                    float foot=(bottom-rect.y-frames[i].pivot.y)/frames[i].pixelsPerUnit;
                    if(!staggered)
                    {
                        if(i==0)ground=foot;
                        Check(Mathf.Abs(foot-ground)<.001f,"Foot baseline remains fixed in frame "+i);
                    }
                    camera.Render();RenderTexture.active=target;
                    int destX=i%columns*256,destY=(1-i/columns)*512;
                    preview.ReadPixels(new Rect(0,0,256,512),destX,destY,false);
                    if(staggered)
                    {
                        int sole=512;
                        for(int y=0;y<512 && sole==512;y++)for(int x=0;x<256;x++)
                        {
                            var color=preview.GetPixel(destX+x,destY+y);
                            if(Mathf.Max(color.r,color.g,color.b)>.18f){sole=y;break;}
                        }
                        if(i==0)ground=sole;
                        Check(Mathf.Abs(sole-ground)<=1,"Rendered sole height remains fixed in frame "+i);
                        Check(frames[i].vertices.Length>=4 && frames[i].triangles.Length>=6,"Frame has valid custom separation mesh "+i);
                    }
                }
                animator.Play("Base Layer.SkullMonkWalk",0,1.004f);animator.Update(0);
                Check(renderer.sprite==frames[0],"Last frame wraps back to first correctly");
                if(!staggered)for(int i=0;i<frameCount;i++)for(int j=i+1;j<frameCount;j++)Check(!frames[i].rect.Overlaps(frames[j].rect),"Slice separation "+i+" / "+j);
                preview.Apply();Directory.CreateDirectory(folder);File.WriteAllBytes(folder+"/ContactSheet.png",preview.EncodeToPNG());
                report+="COMPLETE "+checks+" checks passed\n";
                return report;
            }
            finally
            {
                RenderTexture.active=previous;
                if(previewCamera!=null)previewCamera.targetTexture=null;
                if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
                UnityEngine.Object.DestroyImmediate(raw);UnityEngine.Object.DestroyImmediate(preview);
                EditorSceneManager.ClosePreviewScene(scene);
                Directory.CreateDirectory(folder);File.WriteAllText(folder+"/Validation.txt",report);
            }
        }
    }
}
