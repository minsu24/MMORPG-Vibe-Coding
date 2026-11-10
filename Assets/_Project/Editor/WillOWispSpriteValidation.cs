using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class WillOWispSpriteValidation
    {
        const string Folder="Artifacts/WillOWisp";
        static string report;static int checks;
        static void Check(bool value,string message)
        {
            report+=(value?"PASS ":"FAIL ")+message+"\n";
            if(!value)throw new Exception(message);
            checks++;
        }

        public static string VerifyRestore()
        {
            var paths=WillOWispSpriteSetup.Paths;
            var bytes=paths.Select(File.ReadAllBytes).ToArray();
            var ids=paths.Select(p=>AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().OrderBy(s=>s.name).Select(Id).ToArray()).ToArray();
            try
            {
                WillOWispSpriteSetup.Restore();
                foreach(string path in paths)
                    if(!File.ReadAllBytes(path).SequenceEqual(File.ReadAllBytes(WillOWispSpriteSetup.Backup+"/"+Path.GetFileName(path))))
                        throw new Exception("Original PNG restoration mismatch: "+path);
            }
            finally{WillOWispSpriteSetup.Apply();}
            for(int i=0;i<paths.Length;i++)
            {
                if(!bytes[i].SequenceEqual(File.ReadAllBytes(paths[i])))throw new Exception("Non-deterministic normalized image: "+paths[i]);
                var current=AssetDatabase.LoadAllAssetsAtPath(paths[i]).OfType<Sprite>().OrderBy(s=>s.name).Select(Id);
                if(!ids[i].SequenceEqual(current))throw new Exception("Restoration changed normalized sprite IDs: "+paths[i]);
            }
            return "All four original sheets restored byte-for-byte; normalized images and sprite IDs recreated exactly.";
        }
        static long Id(Sprite sprite)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id);return id;
        }

        public static string Run()
        {
            report="Native Unity wisp frame size, separation and rendering verification\n";checks=0;
            var scene=EditorSceneManager.NewPreviewScene();
            var contact=new Texture2D(2048,1024,TextureFormat.RGBA32,false);
            var oldTarget=RenderTexture.active;
            var target=new RenderTexture(256,256,24);target.Create();
            Camera camera=null;
            try
            {
                var root=new GameObject("Wisp sprite verification",typeof(SpriteRenderer));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
                var renderer=root.GetComponent<SpriteRenderer>();
                var cameraObject=new GameObject("Wisp preview camera",typeof(Camera));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
                camera=cameraObject.GetComponent<Camera>();camera.scene=scene;
                camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=1.8f;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.12f,.12f,1);
                camera.targetTexture=target;
                int total=0,row=0;
                foreach(string path in WillOWispSpriteSetup.Paths)
                {
                    int count=WillOWispSpriteSetup.Count(path);
                    var frames=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>int.Parse(s.name.Substring(s.name.LastIndexOf('_')+1))).ToArray();
                    Check(frames.Length==count,Path.GetFileName(path)+": correct frame count "+count);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                    Check(importer.maxTextureSize>=3072 && !importer.mipmapEnabled && importer.textureCompression==TextureImporterCompression.Uncompressed,"Native resolution and uncompressed transparent import");
                    var raw=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    try
                    {
                        ImageConversion.LoadImage(raw,File.ReadAllBytes(path));var pixels=raw.GetPixels32();
                        Check(raw.width==count*384 && raw.height==384,"Identical 384px square frame cells");
                        for(int i=0;i<count;i++)
                        {
                            var sprite=frames[i];var rect=sprite.rect;
                            Check(rect==new Rect(i*384,0,384,384),"Frame "+i+": exact non-overlapping grid rectangle");
                            Check(Mathf.Abs(sprite.pixelsPerUnit-WillOWispSpriteSetup.PixelsPerUnit)<.001f && sprite.pivot==new Vector2(192,192),"Frame "+i+": common scale and centre pivot");
                            int minX=384,minY=384,maxX=-1,maxY=-1;bool borderClear=true;
                            for(int y=0;y<384;y++)for(int x=0;x<384;x++)
                            {
                                byte a=pixels[y*raw.width+i*384+x].a;
                                if((x<8 || x>=376 || y<8 || y>=376) && a!=0)borderClear=false;
                                if(a>=128){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
                            }
                            Check(borderClear,"Frame "+i+": eight-pixel transparent outer border prevents neighbouring-frame bleed");
                            int longest=Math.Max(maxX-minX+1,maxY-minY+1);
                            Check(Math.Abs(longest-320)<=2,"Frame "+i+": visible maximum extent "+longest+"px (target320)");
                            Check(Math.Abs((minX+maxX)*.5f-191.5f)<=1.5f && Math.Abs((minY+maxY)*.5f-191.5f)<=1.5f,"Frame "+i+": visible silhouette centred");
                            renderer.sprite=sprite;
                            Check(Vector2.Distance((Vector2)renderer.bounds.size,new Vector2(3.3f,3.3f))<.001f,"Frame "+i+": native SpriteRenderer uses common world bounds");
                            camera.Render();RenderTexture.active=target;
                            contact.ReadPixels(new Rect(0,0,256,256),i*256,(3-row)*256,false);
                            total++;
                        }
                    }
                    finally{UnityEngine.Object.DestroyImmediate(raw);}
                    row++;
                }
                Check(total==31,"All 31 animation frames rendered in Unity");
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/will-o'-the-wisp.prefab");
                var linked=prefab.GetComponent<SpriteRenderer>().sprite;
                Check(linked!=null && linked.name=="도깨비불 대기_0" && Id(linked)==4944381863768852117L,"Existing monster prefab retains its original sprite file ID");
                contact.Apply();Directory.CreateDirectory(Folder);File.WriteAllBytes(Folder+"/ContactSheet.png",contact.EncodeToPNG());
                report+="COMPLETE "+checks+" checks passed\n";return checks+" checks passed; 31 sprites rendered.";
            }
            finally
            {
                if(camera!=null)camera.targetTexture=null;RenderTexture.active=oldTarget;
                target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(contact);
                EditorSceneManager.ClosePreviewScene(scene);
                Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Validation.txt",report);
            }
        }
    }
}
