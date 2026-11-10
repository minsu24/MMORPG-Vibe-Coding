using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class SkullMonkIdleAlignment
    {
        const string Art = "Assets/_Project/Art/Old Temple/";
        const string Clip = "Assets/_Project/Animations/Old Temple/SkullMonkIdle.anim";
        const string Backup = "Assets/_Project/Animations/Old Temple/Backups/SkullMonkIdle_BeforeAlignment";
        // Foot silhouette centres and sole rows in the original, full-resolution sheets (top-left origin).
        static readonly float[] FeetX = {132,400,675.5f,946.5f,1209,1483,1753.5f,2023.5f};
        static readonly int[] SoleY = {640,639,639,639,638,639,641,641};

        [MenuItem("Eastern Fantasy/Monsters/Align Skull Monk Idle")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            Directory.CreateDirectory(Backup);
            if (!File.Exists(Backup + "/SkullMonkIdle_Original.anim"))
            {
                AssetDatabase.CopyAsset(Clip, Backup + "/SkullMonkIdle_Original.anim");
                foreach (string suffix in new[]{"1357","2468"})
                    File.Copy(Art + "해골 승려 대기 " + suffix + ".png.meta", Backup + "/Idle" + suffix + ".meta.txt");
            }
            // Keep the first frame's original placement and all existing sprite IDs/references.
            foreach (string suffix in new[]{"1357","2468"}) AlignSheet(suffix);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Skeleton idle aligned by feet; original clip and import settings backed up: " + Backup);
        }

        [MenuItem("Eastern Fantasy/Monsters/Restore Skull Monk Original Idle Alignment")]
        public static void Restore()
        {
            if(Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if(AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            foreach(string suffix in new[]{"1357","2468"})
                if(!File.Exists(Backup+"/Idle"+suffix+".meta.txt")) throw new FileNotFoundException("Original idle import settings are missing.");
            foreach(string suffix in new[]{"1357","2468"})
            {
                string path=Art+"해골 승려 대기 "+suffix+".png";
                File.Copy(Backup+"/Idle"+suffix+".meta.txt",path+".meta",true);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            }
        }

        static void AlignSheet(string suffix)
        {
            string path = Art + "해골 승려 대기 " + suffix + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var rects = provider.GetSpriteRects().OrderBy(r=>r.rect.x).ToArray();
            if(rects.Length != 4) throw new InvalidOperationException("Expected four sprites per sheet.");
            var texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                ImageConversion.LoadImage(texture,File.ReadAllBytes(path));
                var pixels = texture.GetPixels32();
                var regions = new List<RectInt>();
                int start=-1,last=-1;
                for(int x=0;x<texture.width+8;x++)
                {
                    bool opaque=false;
                    if(x<texture.width)
                        for(int y=0;y<texture.height;y++) if(pixels[y*texture.width+x].a>=128){opaque=true;break;}
                    if(opaque){if(start<0)start=x;last=x;}
                    else if(start>=0 && x-last>=8)
                    {
                        int bottom=texture.height,top=0;
                        for(int xx=start;xx<=last;xx++) for(int y=0;y<texture.height;y++)
                            if(pixels[y*texture.width+xx].a>=128){bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
                        int left=Math.Max(0,start-2),right=Math.Min(texture.width,last+3);
                        bottom=Math.Max(0,bottom-2);top=Math.Min(texture.height,top+3);
                        // Ignore isolated residue in the deliberately empty alternating cells.
                        if(right-left>20) regions.Add(new RectInt(left,bottom,right-left,top-bottom));
                        start=-1;
                    }
                }
                if(regions.Count!=4)throw new InvalidOperationException("Could not isolate four idle frames: "+regions.Count);
                for(int i=0;i<4;i++)
                {
                    int frame=i*2+(suffix=="2468"?1:0);
                    var region=regions[i];
                    rects[i].rect=new Rect(region.x,region.y,region.width,region.height);
                    rects[i].alignment=SpriteAlignment.Custom;
                    // Original frame 0: rect (0,78,294,515), centre pivot (147,257.5).
                    // Its foot centre is -15px horizontally and -251.5px vertically from the pivot.
                    rects[i].pivot=new Vector2((FeetX[frame]+15-region.x)/region.width,
                        (texture.height-SoleY[frame]+251.5f-region.y)/region.height);
                }
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
                provider.SetSpriteRects(rects);provider.Apply();importer.SaveAndReimport();
            }
            finally {UnityEngine.Object.DestroyImmediate(texture);}
        }

        public static string Validate()
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Clip);
            var original=AssetDatabase.LoadAssetAtPath<AnimationClip>(Backup+"/SkullMonkIdle_Original.anim");
            var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
            var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);
            var old=AnimationUtility.GetObjectReferenceCurve(original,binding);
            if(keys.Length!=old.Length || clip.frameRate!=original.frameRate || clip.length!=original.length)
                throw new Exception("Idle timing changed.");
            if(AnimationUtility.GetCurveBindings(clip).Length!=0)throw new Exception("Unexpected transform curves.");
            string report="Skeleton idle alignment: "+keys.Length+" keys, "+clip.frameRate+" FPS, "+clip.length+" seconds\n";
            var go=new GameObject("IdleAlignmentValidation");var renderer=go.AddComponent<SpriteRenderer>();
            var animator=go.AddComponent<Animator>();
            animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SkullMonkWalkSetup.ControllerPath);
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();animator.Update(0);
            var position=go.transform.position;
            try
            {
                for(int i=0;i<keys.Length;i++)
                {
                    if(keys[i].value!=old[i].value || keys[i].time!=old[i].time)throw new Exception("Sprite reference or order changed.");
                    animator.Play("Skeleton",0,(keys[i].time+.02f)/clip.length);animator.Update(0);
                    var sprite=renderer.sprite;
                    if(sprite!=keys[i].value || go.transform.position!=position)throw new Exception("Animation sample mismatch at key "+i+": actual="+sprite+" expected="+keys[i].value);
                    int frame=int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_')+1))*2+
                        (AssetDatabase.GetAssetPath(sprite).Contains("2468")?1:0);
                    float scale=sprite.texture.width/2172f;
                    var anchor=new Vector2((FeetX[frame]*scale-sprite.rect.x-sprite.pivot.x)/sprite.pixelsPerUnit,
                        ((724-SoleY[frame])*scale-sprite.rect.y-sprite.pivot.y)/sprite.pixelsPerUnit);
                    if(Vector2.Distance(anchor,new Vector2(-.15f,-2.515f))>.002f)throw new Exception("Foot anchor drift: "+anchor);
                    report+="PASS key "+i+": frame "+(frame+1)+"; foot anchor="+anchor.ToString("F4")+"; transform unchanged\n";
                }
            }
            finally {UnityEngine.Object.DestroyImmediate(go);}
            Directory.CreateDirectory("Artifacts/SkullMonkIdle");
            File.WriteAllText("Artifacts/SkullMonkIdle/Validation.txt",report+"PASS: original references, timing, position and all foot anchors.\n");
            return report;
        }
    }
}
