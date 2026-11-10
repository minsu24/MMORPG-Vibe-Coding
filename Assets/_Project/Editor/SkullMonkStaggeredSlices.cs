using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EasternFantasy.Editor
{
    // Import geometry separates close staff ribbons/hat edges without repainting the generated art.
    public static class SkullMonkStaggeredSlices
    {
        public static Sprite[] Import(Sprite reference)
        {
            string path=SkullMonkWalk12Setup.StaggeredSheet;
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                ImageConversion.LoadImage(texture,File.ReadAllBytes(path));
                if(texture.width!=2172 || texture.height!=724)throw new Exception("Unexpected staggered sheet resolution.");
                var pixels=texture.GetPixels32();int height=texture.height;
                var seams=new int[9][];
                seams[0]=Enumerable.Repeat(0,height+1).ToArray();
                seams[8]=Enumerable.Repeat(texture.width,height+1).ToArray();
                for(int i=1;i<8;i++)seams[i]=FindSeam(pixels,texture.width,height,(int)Math.Round(i*texture.width/8f));
                var opaque=new RectInt[8];
                for(int i=0;i<8;i++)
                {
                    int minX=texture.width,minY=height,maxX=0,maxY=0;
                    for(int y=0;y<height;y++)for(int x=seams[i][y];x<seams[i+1][y];x++)
                        if(pixels[y*texture.width+x].a>=128){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
                    opaque[i]=new RectInt(minX,minY,maxX-minX+1,maxY-minY+1);
                }
                var referenceBounds=VisibleBounds(reference);
                float ppu=opaque[0].height/referenceBounds.height;
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit=ppu;importer.maxTextureSize=4096;
                importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.Tight;importer.SetTextureSettings(settings);
                var factories=new SpriteDataProviderFactories();factories.Init();
                var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
                var existing=provider.GetSpriteRects().ToDictionary(s=>s.name,s=>s.spriteID);
                var rects=new SpriteRect[8];var pairs=new SpriteNameFileIdPair[8];
                for(int i=0;i<8;i++)
                {
                    string name="SkullMonkStaggered_"+i.ToString("00");
                    var id=existing.TryGetValue(name,out var old)?old:GUID.Generate();
                    int left=seams[i].Min(),right=seams[i+1].Max();
                    rects[i]=new SpriteRect{name=name,spriteID=id,rect=new Rect(left,0,right-left,height),alignment=SpriteAlignment.Custom,
                        pivot=new Vector2(((i+.5f)*texture.width/8f-left)/(right-left),(opaque[i].yMin-referenceBounds.yMin*ppu)/height)};
                    pairs[i]=new SpriteNameFileIdPair(name,id);
                }
                provider.SetSpriteRects(rects);provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
                var outlines=provider.GetDataProvider<ISpriteOutlineDataProvider>();
                for(int i=0;i<8;i++)
                {
                    var points=new List<Vector2>();
                    for(int y=0;y<=height;y++)
                        if(y==0 || y==height || seams[i][y]-seams[i][y-1] != seams[i][y+1]-seams[i][y])
                            points.Add(new Vector2(seams[i][y]-rects[i].rect.center.x,y-height*.5f));
                    for(int y=height;y>=0;y--)
                        if(y==0 || y==height || seams[i+1][y]-seams[i+1][y-1] != seams[i+1][y+1]-seams[i+1][y])
                            points.Add(new Vector2(seams[i+1][y]-rects[i].rect.center.x,y-height*.5f));
                    outlines.SetOutlines(rects[i].spriteID,new List<Vector2[]>{points.ToArray()});
                }
                provider.Apply();importer.SaveAndReimport();
            }
            finally{UnityEngine.Object.DestroyImmediate(texture);}
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        }

        public static Rect VisibleBounds(Sprite sprite)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                ImageConversion.LoadImage(texture,File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
                var pixels=texture.GetPixels32();var rect=sprite.rect;
                int bottom=texture.height,top=0;
                for(int y=(int)rect.yMin;y<(int)rect.yMax;y++)for(int x=(int)rect.xMin;x<(int)rect.xMax;x++)
                    if(pixels[y*texture.width+x].a>=128){bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
                return new Rect(0,(bottom-rect.y-sprite.pivot.y)/sprite.pixelsPerUnit,1,(top-bottom+1)/sprite.pixelsPerUnit);
            }
            finally{UnityEngine.Object.DestroyImmediate(texture);}
        }

        public static int[] FindSeam(Color32[] pixels,int width,int height,int center)
        {
            const int radius=36;int count=radius*2+1,left=center-radius;
            var cost=new float[height,count];var parents=new int[height,count];
            for(int y=0;y<height;y++)for(int n=0;n<count;n++)
            {
                int x=left+n;float alpha=pixels[y*width+x].a;
                float pixelCost=(alpha>=128?100f:alpha*.05f)+Mathf.Abs(x-center)*.002f;
                if(y==0){cost[y,n]=pixelCost;continue;}
                float best=float.PositiveInfinity;int parent=n;
                for(int prev=Math.Max(0,n-1);prev<=Math.Min(count-1,n+1);prev++)
                {
                    float value=cost[y-1,prev]+Mathf.Abs(prev-n)*.08f;
                    if(value<best){best=value;parent=prev;}
                }
                cost[y,n]=pixelCost+best;parents[y,n]=parent;
            }
            int end=0;for(int n=1;n<count;n++)if(cost[height-1,n]<cost[height-1,end])end=n;
            var seam=new int[height+1];
            for(int y=height-1;y>=0;y--){seam[y]=left+end;end=parents[y,end];}
            seam[height]=seam[height-1];return seam;
        }
    }
}
