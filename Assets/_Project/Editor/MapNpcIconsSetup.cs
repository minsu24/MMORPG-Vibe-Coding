using System.Collections.Generic;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.EditorTools
{
    public static class MapNpcIconsSetup
    {
        private const string TexturePath = "Assets/_Project/Art/UI/MapSystem/NpcPortraits.png";
        private const string Folder = "Assets/_Project/Art/UI/MapSystem/";
        [MenuItem("Eastern Fantasy/UI/Setup Map NPC Icons")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Run outside Play mode.");
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.isReadable=true; importer.mipmapEnabled=true; importer.filterMode=FilterMode.Trilinear; importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=2048;
            importer.wrapMode=TextureWrapMode.Clamp; importer.SaveAndReimport();
            try
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                var sprites = ExtractSprites(texture);
                const string path="Assets/_Project/Prefabs/UI/MapWindow.prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var window=new SerializedObject(root.GetComponent<MapWindowUI>());
                    var component=root.GetComponent<MapNpcIconsUI>();
                    if(component==null) component=root.AddComponent<MapNpcIconsUI>();
                    var data=new SerializedObject(component);
                    data.FindProperty("detailImage").objectReferenceValue=window.FindProperty("detailImage").objectReferenceValue;
                    data.FindProperty("font").objectReferenceValue=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/GowunBatang-Bold SDF.asset");
                    var entries=data.FindProperty("entries"); entries.arraySize=5;
                    string[] scenes={"PrologueVilage","SpiderCave","PrologueVilage","PrologueVilage","BrokenSeal"};
                    string[] names={"대장장이","다친 주민","상점 주인","촌장","금두꺼비"};
                    for(int i=0;i<5;i++)
                    {
                        var entry=entries.GetArrayElementAtIndex(i);
                        entry.FindPropertyRelative("sceneName").stringValue=scenes[i];
                        entry.FindPropertyRelative("displayName").stringValue=names[i];
                        entry.FindPropertyRelative("portrait").objectReferenceValue=sprites[i];
                    }
                    data.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                AssetDatabase.SaveAssets();
                Debug.Log("Five original NPC portraits assigned to village, spider cave, and broken seal detail map locations.");
            }
            finally { importer.isReadable=false; importer.SaveAndReimport(); }
        }
        // Keep the supplied bitmap intact. Meshes select each disconnected sticker directly from its atlas.
        private static MapNpcPortrait[] ExtractSprites(Texture2D texture)
        {
            var pixels=texture.GetPixels32(); int width=texture.width, height=texture.height;
            var component=new int[pixels.Length]; int id=0;
            var queue=new Queue<int>(); var groups=new List<List<int>>();
            for(int start=0;start<pixels.Length;start++)
            {
                if(component[start]!=0 || pixels[start].a<20)continue;
                id++; var group=new List<int>(); component[start]=id; queue.Enqueue(start);
                while(queue.Count>0)
                {
                    int k=queue.Dequeue(),x=k%width,y=k/width; group.Add(k);
                    if(x>0) Enqueue(k-1,id,pixels,component,queue);
                    if(x<width-1) Enqueue(k+1,id,pixels,component,queue);
                    if(y>0) Enqueue(k-width,id,pixels,component,queue);
                    if(y<height-1) Enqueue(k+width,id,pixels,component,queue);
                }
                if(group.Count>5000)groups.Add(group);
            }
            if(groups.Count!=5)throw new System.InvalidOperationException("Expected five disconnected portraits; found "+groups.Count);
            // Top row left to right, then bottom row left to right.
            groups.Sort((a,b)=>
            {
                var ra=Bounds(a,width); var rb=Bounds(b,width);
                bool topA=ra.yMax>height*.8f,topB=rb.yMax>height*.8f;
                return topA!=topB ? (topA?-1:1) : ra.xMin.CompareTo(rb.xMin);
            });
            string[] names={"Blacksmith","InjuredResident","Shopkeeper","VillageChief","GoldenToad"};
            var result=new MapNpcPortrait[5];
            for(int i=0;i<5;i++)
            {
                var group=groups[i]; Rect rect=Bounds(group,width); int groupId=component[group[0]];
                var strips=new List<Vector4>();
                for(int y=(int)rect.yMin;y<(int)rect.yMax;y++)
                {
                    int x=(int)rect.xMin;
                    while(x<(int)rect.xMax)
                    {
                        if(component[y*width+x]!=groupId){x++;continue;}
                        int left=x; while(x<(int)rect.xMax && component[y*width+x]==groupId)x++;
                        strips.Add(new Vector4(left,y,x,y+1));
                    }
                }
                if(strips.Count*4>65535)throw new System.InvalidOperationException("Portrait mesh too large");
                string path=Folder+names[i]+"MapIcon.asset";
                var shape=AssetDatabase.LoadAssetAtPath<MapNpcPortrait>(path);
                if(shape==null)
                {
                    if(AssetDatabase.LoadMainAssetAtPath(path)!=null) AssetDatabase.DeleteAsset(path);
                    shape=ScriptableObject.CreateInstance<MapNpcPortrait>();
                    AssetDatabase.CreateAsset(shape,path);
                }
                shape.atlas=texture; shape.region=rect; shape.strips=strips.ToArray();
                EditorUtility.SetDirty(shape); result[i]=shape;
            }
            return result;
        }
        private static void Enqueue(int k,int id,Color32[] pixels,int[] component,Queue<int> queue)
        {
            if(component[k]!=0 || pixels[k].a<20)return;
            component[k]=id;queue.Enqueue(k);
        }
        private static Rect Bounds(List<int> group,int width)
        {
            int left=width,right=0,bottom=int.MaxValue,top=0;
            foreach(int k in group){int x=k%width,y=k/width;left=Mathf.Min(left,x);right=Mathf.Max(right,x+1);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y+1);}
            return Rect.MinMaxRect(left,bottom,right,top);
        }
    }
}
