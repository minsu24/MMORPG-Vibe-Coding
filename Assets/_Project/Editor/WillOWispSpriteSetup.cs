using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class WillOWispSpriteSetup
    {
        public const string Folder="Assets/_Project/Art/Old Temple/Monster/will-o'-the-wisp";
        public const string Backup="Artifacts/WillOWisp/Originals";
        public const int Cell=384, VisiblePixels=320;
        // Match the original first idle frame's 275px silhouette at the authored 100 PPU.
        public const float PixelsPerUnit=VisiblePixels/2.75f;
        public static string[] Paths => Directory.GetFiles(Folder,"*.png").Select(p=>p.Replace('\\','/')).OrderBy(p=>p).ToArray();
        public static int Count(string path) => path.Contains("투사체")?7:8;

        [MenuItem("Eastern Fantasy/Monsters/Normalize Will O Wisp Sprites")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            var paths=Paths;
            if(paths.Length!=4)throw new Exception("Expected four wisp source sheets.");
            Directory.CreateDirectory(Backup);
            // Preserve every source before changing any sheet; subsequent runs always use the originals.
            foreach(string path in paths)
            {
                string saved=Backup+"/"+Path.GetFileName(path);
                if(!File.Exists(saved)){File.Copy(path,saved);File.Copy(path+".meta",saved+".meta.txt");}
                if(!File.Exists(saved+".meta.txt"))throw new Exception("Incomplete original backup: "+saved);
            }
            foreach(string path in paths)Normalize(path);
            AssetDatabase.SaveAssets();
            Debug.Log("Wisp sprites normalized: 8 idle, 8 attack, 8 death, 7 projectile frames. Originals: "+Backup);
        }

        static void Normalize(string path)
        {
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            Texture2D result=null;
            try
            {
                ImageConversion.LoadImage(source,File.ReadAllBytes(Backup+"/"+Path.GetFileName(path)));
                int count=Count(path),width=source.width,height=source.height;
                var pixels=source.GetPixels32();
                var seams=new int[count+1][];
                seams[0]=Enumerable.Repeat(0,height+1).ToArray();
                seams[count]=Enumerable.Repeat(width,height+1).ToArray();
                int[] projectileCenters={295,590,895,1200,1530,1870};
                for(int i=1;i<count;i++)seams[i]=SkullMonkStaggeredSlices.FindSeam(pixels,width,height,
                    count==7?projectileCenters[i-1]:(int)Math.Round(i*width/8f));
                var bounds=new RectInt[count];
                for(int i=0;i<count;i++)
                {
                    int minX=width,minY=height,maxX=-1,maxY=-1;
                    for(int y=0;y<height;y++)for(int x=seams[i][y];x<seams[i+1][y];x++)
                    {
                        byte a=pixels[y*width+x].a;
                        if(a>=128){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
                    }
                    if(maxX<minX || maxY<minY)throw new Exception("Empty frame "+i+" in "+path);
                    bounds[i]=new RectInt(minX,minY,maxX-minX+1,maxY-minY+1);
                }
                // Recover original IDs from the original import settings, even on repeat runs.
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                var factories=new SpriteDataProviderFactories();factories.Init();
                var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
                var existing=provider.GetSpriteRects();
                var rects=new SpriteRect[count];var pairs=new SpriteNameFileIdPair[count];
                bool alreadyNormalized=source.width!=AssetDatabase.LoadAssetAtPath<Texture2D>(path).width;
                var used=new HashSet<GUID>();
                var destination=new Color32[count*Cell*Cell];
                for(int i=0;i<count;i++)
                {
                    var box=bounds[i];float scale=VisiblePixels/(float)Math.Max(box.width,box.height);
                    // Dense silhouette determines size/centre; translucent embers retain their original relative motion.
                    float centreX=box.x+(box.width-1)*.5f,centreY=box.y+(box.height-1)*.5f;
                    for(int y=0;y<Cell;y++)for(int x=0;x<Cell;x++)
                    {
                        if(x<8 || x>=Cell-8 || y<8 || y>=Cell-8)continue;
                        float sx=(x-(Cell-1)*.5f)/scale+centreX,sy=(y-(Cell-1)*.5f)/scale+centreY;
                        destination[y*count*Cell+i*Cell+x]=Sample(pixels,width,height,seams[i],seams[i+1],sx,sy);
                    }
                    string name=Path.GetFileNameWithoutExtension(path)+"_"+i;
                    SpriteRect old;
                    if(alreadyNormalized)old=existing.FirstOrDefault(r=>r.name==name);
                    else old=existing.Where(r=>!used.Contains(r.spriteID) && r.rect.center.x>=seams[i].Average()
                        && r.rect.center.x<seams[i+1].Average()).OrderByDescending(r=>r.rect.width*r.rect.height).FirstOrDefault();
                    var id=old!=null?old.spriteID:GUID.Generate();used.Add(id);
                    rects[i]=new SpriteRect{name=name,spriteID=id,rect=new Rect(i*Cell,0,Cell,Cell),
                        alignment=SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};
                    pairs[i]=new SpriteNameFileIdPair(name,id);
                }
                result=new Texture2D(count*Cell,Cell,TextureFormat.RGBA32,false,true);
                result.SetPixels32(destination);result.Apply();
                File.WriteAllBytes(path,result.EncodeToPNG());
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit=PixelsPerUnit;importer.maxTextureSize=4096;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
                foreach(var platform in new[]{"Standalone","Android","iPhone"})
                {
                    var config=importer.GetPlatformTextureSettings(platform);
                    if(config.overridden){config.maxTextureSize=4096;config.textureCompression=TextureImporterCompression.Uncompressed;importer.SetPlatformTextureSettings(config);}
                }
                provider.SetSpriteRects(rects);provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
                provider.Apply();importer.SaveAndReimport();
            }
            finally{UnityEngine.Object.DestroyImmediate(source);if(result!=null)UnityEngine.Object.DestroyImmediate(result);}
        }

        static Color32 Sample(Color32[] pixels,int width,int height,int[] left,int[] right,float x,float y)
        {
            int x0=Mathf.FloorToInt(x),y0=Mathf.FloorToInt(y);float tx=x-x0,ty=y-y0;
            float alpha=0,r=0,g=0,b=0;
            for(int dy=0;dy<=1;dy++)for(int dx=0;dx<=1;dx++)
            {
                int xx=x0+dx,yy=y0+dy;
                if(yy<0 || yy>=height || xx<0 || xx>=width || xx<left[yy] || xx>=right[yy])continue;
                var p=pixels[yy*width+xx];float w=(dx==0?1-tx:tx)*(dy==0?1-ty:ty)*p.a/255f;
                alpha+=w;r+=p.r*w;g+=p.g*w;b+=p.b*w;
            }
            // Remove only near-invisible generator residue so each cell has a clean transparent border.
            if(alpha<8f/255f)return new Color32(0,0,0,0);
            return new Color32((byte)Mathf.RoundToInt(r/alpha),(byte)Mathf.RoundToInt(g/alpha),(byte)Mathf.RoundToInt(b/alpha),(byte)Mathf.RoundToInt(alpha*255));
        }

        [MenuItem("Eastern Fantasy/Monsters/Restore Original Will O Wisp Sheets")]
        public static void Restore()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            foreach(string path in Paths)
                if(!File.Exists(Backup+"/"+Path.GetFileName(path)+".meta.txt"))throw new Exception("Original import settings missing.");
            foreach(string path in Paths)
            {
                string saved=Backup+"/"+Path.GetFileName(path);
                File.Copy(saved,path,true);File.Copy(saved+".meta.txt",path+".meta",true);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            }
        }
    }
}
