using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EasternFantasy.Inventory;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class ArmorSetImageSetup
    {
        public const string IconFolder = "Assets/_Project/Art/Item/ArmorSets/Icons/";
        private const string Sources = "Assets/_Project/Art/Item/ArmorSets/Sources/";
        private static readonly string[] Names = {"OldHat","ClothTop","ClothBottom","ClothGloves","ClothShoes"};
        private static readonly string[] Grades = {"Common","Rare","Heroic","Legendary","Mythic"};
        public static bool IsSquareIcon(string path) => path.StartsWith(IconFolder, StringComparison.Ordinal);
        // Coordinates use the original sheet's top-left origin. Slanted boundaries follow
        // the spaces between clothing silhouettes rather than cutting across a fixed grid.
        private static readonly Vector2[][] Regions = {
            Polygon(0,0, 450,0, 450,285, 455,405, 500,500, 540,570, 590,650, 480,705, 350,690, 300,630, 0,615),
            Polygon(450,0, 1000,0, 1000,270, 1030,370, 1010,490, 1080,600, 1080,640, 1000,685, 920,750, 750,750, 700,675, 565,675, 540,610, 540,570, 500,500, 455,405, 450,285),
            Polygon(1000,0, 1448,0, 1448,655, 1350,670, 1250,650, 1180,640, 1130,655, 1050,645, 1000,610, 1010,490, 1030,370, 1000,270),
            Polygon(0,615, 300,630, 350,690, 480,705, 565,675, 700,675, 750,750, 760,1086, 0,1086),
            Polygon(760,1086, 750,750, 920,750, 1000,685, 1080,640, 1130,655, 1180,640, 1250,650, 1350,670, 1448,655, 1448,1086)
        };
        private static Vector2[] Polygon(params int[] xy)
        {
            var result = new Vector2[xy.Length/2];
            for(int i=0;i<result.Length;i++) result[i]=new Vector2(xy[i*2],xy[i*2+1]);
            return result;
        }
        [MenuItem("Eastern Fantasy/Inventory/Replace Training Armor Images")]
        public static void Run()
        {
            Directory.CreateDirectory(IconFolder);
            for(int g=0;g<5;g++)
            {
                var source = new Texture2D(2,2,TextureFormat.RGBA32,false);
                try
                {
                    source.LoadImage(File.ReadAllBytes(Sources+Grades[g]+".png"));
                    if(source.width!=1448 || source.height!=1086) throw new Exception("Unexpected source dimensions");
                    for(int i=0;i<5;i++)
                    {
                        string path=IconFolder+Names[i]+"_"+Grades[g]+".png";
                        WriteIcon(source,Regions[i],i,path);
                        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                        var sprite=ImportSquare(path,Names[i]);
                        string commonPath="Assets/_Project/Data/Items/"+Names[i]+".asset";
                        var common=AssetDatabase.LoadAssetAtPath<ItemDefinition>(commonPath);
                        var appearance=AssetDatabase.LoadAssetAtPath<ItemGradeAppearance>("Assets/_Project/Data/Items/Grades/"+Names[i]+"Appearance.asset");
                        var data=new SerializedObject(appearance);
                        data.FindProperty("icons").GetArrayElementAtIndex(g).objectReferenceValue=sprite;
                        data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(appearance);
                        var variant=common.GetGradeVariant((ItemGrade)g);
                        var itemData=new SerializedObject(variant);
                        itemData.FindProperty("icon").objectReferenceValue=sprite;
                        itemData.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(variant);
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(source);}
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Replaced 25 armor icons: isolated silhouettes, 512x512 square canvas, consistent 448px visible extent.");
        }
        private static bool Contains(Vector2[] polygon,float x,float y)
        {
            bool inside=false;
            for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
            {
                var a=polygon[i];var b=polygon[j];
                if((a.y>y)!=(b.y>y) && x<(b.x-a.x)*(y-a.y)/(b.y-a.y)+a.x)inside=!inside;
            }
            return inside;
        }
        private static void WriteIcon(Texture2D source,Vector2[] polygon,int itemIndex,string path)
        {
            int w=source.width,h=source.height;
            var pixels=source.GetPixels32();
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                if(!Contains(polygon,x+.5f,h-y-.5f))pixels[y*w+x]=new Color32(0,0,0,0);
            // Remove stray disconnected pixels from neighboring effects; preserve both gloves.
            var seen=new bool[pixels.Length];
            var components=new List<List<int>>();
            var queue=new Queue<int>();
            for(int p=0;p<pixels.Length;p++)
            {
                if(seen[p] || pixels[p].a<8)continue;
                var component=new List<int>();seen[p]=true;queue.Enqueue(p);
                while(queue.Count>0)
                {
                    int current=queue.Dequeue();component.Add(current);
                    int cx=current%w,cy=current/w;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        int nx=cx+dx,ny=cy+dy;
                        if(nx<0||nx>=w||ny<0||ny>=h)continue;
                        int next=ny*w+nx;
                        if(seen[next]||pixels[next].a<8)continue;
                        seen[next]=true;queue.Enqueue(next);
                    }
                }
                components.Add(component);
            }
            var keep=new bool[pixels.Length];
            foreach(var component in components.OrderByDescending(c=>c.Count).Take(itemIndex==3?2:1))
                foreach(int p in component)keep[p]=true;
            for(int p=0;p<pixels.Length;p++)if(!keep[p])pixels[p]=new Color32(0,0,0,0);
            Rect bounds=ItemIconAlignmentSetup.VisibleBounds(pixels,w,h,new Rect(0,0,w,h));
            var cropped=new Texture2D((int)bounds.width,(int)bounds.height,TextureFormat.RGBA32,false);
            var output=new Texture2D(512,512,TextureFormat.RGBA32,false);
            try
            {
                var cropPixels=new Color32[cropped.width*cropped.height];
                for(int y=0;y<cropped.height;y++)Array.Copy(pixels,((int)bounds.y+y)*w+(int)bounds.x,cropPixels,y*cropped.width,cropped.width);
                cropped.SetPixels32(cropPixels);cropped.Apply();
                float scale=448f/Mathf.Max(cropped.width,cropped.height);
                int rw=Mathf.Max(2,Mathf.RoundToInt(cropped.width*scale/2)*2);
                int rh=Mathf.Max(2,Mathf.RoundToInt(cropped.height*scale/2)*2);
                var result=new Color32[512*512];
                int left=(512-rw)/2,bottom=(512-rh)/2;
                for(int y=0;y<rh;y++)for(int x=0;x<rw;x++)
                    result[(bottom+y)*512+left+x]=cropped.GetPixelBilinear((x+.5f)/rw,(y+.5f)/rh);
                // Center by the actual resampled visible bounds, accurate within half a pixel.
                Rect visible=ItemIconAlignmentSetup.VisibleBounds(result,512,512,new Rect(0,0,512,512));
                int shiftX=Mathf.RoundToInt(256-visible.center.x),shiftY=Mathf.RoundToInt(256-visible.center.y);
                var centered=new Color32[result.Length];
                for(int y=0;y<512;y++)for(int x=0;x<512;x++)
                {
                    int nx=x+shiftX,ny=y+shiftY;
                    if(nx>=0&&nx<512&&ny>=0&&ny<512)centered[ny*512+nx]=result[y*512+x];
                }
                output.SetPixels32(centered);output.Apply();File.WriteAllBytes(path,output.EncodeToPNG());
            }
            finally{UnityEngine.Object.DestroyImmediate(cropped);UnityEngine.Object.DestroyImmediate(output);}
        }
        private static Sprite ImportSquare(string path,string name)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            var old=provider.GetSpriteRects().FirstOrDefault(s=>s.name==name);
            var rect=new SpriteRect {name=name,rect=new Rect(0,0,512,512),pivot=new Vector2(.5f,.5f),
                alignment=(int)SpriteAlignment.Center,spriteID=old!=null?old.spriteID:GUID.Generate()};
            provider.SetSpriteRects(new[]{rect});
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(new[]{new SpriteNameFileIdPair(rect.name,rect.spriteID)});
            provider.Apply();AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
        }
    }
}