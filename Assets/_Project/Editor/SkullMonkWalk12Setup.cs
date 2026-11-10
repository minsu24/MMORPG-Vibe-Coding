using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class SkullMonkWalk12Setup
    {
        public const string Sheet = "Assets/_Project/Art/Old Temple/SkullMonkWalk_Generated12.png";
        public const string Backup = "Assets/_Project/Animations/Old Temple/Backups/SkullMonkWalk_Before12Frames";
        public const float FPS = 6f;
        public const string StaggeredSheet = "Assets/_Project/Art/Old Temple/SkullMonkWalk_Staggered8.png";
        public const string StaggeredBackup = "Assets/_Project/Animations/Old Temple/Backups/SkullMonkWalk_BeforeStaggered8";
        public const string LongStrideSheet = "Assets/_Project/Art/Old Temple/SkullMonkWalk_LongStride12.png";
        public const string LongStrideBackup = "Assets/_Project/Animations/Old Temple/Backups/SkullMonkWalk_BeforeLongStride12";
        static readonly EditorCurveBinding Binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        static GameObject Skull(Scene scene) => scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="Skull").gameObject;

        [MenuItem("Eastern Fantasy/Monsters/Apply Twelve Frame Skull Monk Walk")]
        public static void Apply() => EditScene(false);
        [MenuItem("Eastern Fantasy/Monsters/Restore Skull Monk Walk Before Twelve Frames")]
        public static void Restore() => EditScene(true);

        [MenuItem("Eastern Fantasy/Monsters/Apply Staggered Eight Frame Skull Monk Walk")]
        public static void ApplyStaggered() => EditScene(false,true);
        [MenuItem("Eastern Fantasy/Monsters/Restore Skull Monk Walk Before Staggered Eight Frames")]
        public static void RestoreStaggered() => EditScene(true,true);

        [MenuItem("Eastern Fantasy/Monsters/Apply Long Stride Twelve Frame Skull Monk Walk")]
        public static void ApplyLongStride() => EditScene(false,false,true);
        [MenuItem("Eastern Fantasy/Monsters/Restore Skull Monk Walk Before Long Stride Twelve Frames")]
        public static void RestoreLongStride() => EditScene(true,false,true);

        static void EditScene(bool restore,bool staggered=false,bool longStride=false)
        {
            string backup=longStride?LongStrideBackup:staggered?StaggeredBackup:Backup;
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            if(AnimationMode.InAnimationMode())AnimationMode.StopAnimationMode();
            var previous=SceneManager.GetActiveScene();
            var scene=SceneManager.GetSceneByPath(SkullMonkWalkSetup.TemplePath);
            bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(SkullMonkWalkSetup.TemplePath,OpenSceneMode.Additive);
            try
            {
                var actor=Skull(scene);
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(SkullMonkWalkSetup.ClipPath);
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(SkullMonkWalkSetup.ControllerPath);
                if(restore)
                {
                    var original=AssetDatabase.LoadAssetAtPath<AnimationClip>(backup+"/Walk.anim");
                    if(original==null)throw new InvalidOperationException("Previous walk backup is missing.");
                    string name=clip.name;EditorUtility.CopySerialized(original,clip);clip.name=name;
                    var savedController=AssetDatabase.LoadAssetAtPath<AnimatorController>(backup+"/Controller.controller");
                    string defaultName=savedController.layers[0].stateMachine.defaultState.name;
                    controller.layers[0].stateMachine.defaultState=controller.layers[0].stateMachine.states.First(s=>s.state.name==defaultName).state;
                    actor.GetComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<GameObject>(backup+"/Skull.prefab").GetComponent<SpriteRenderer>().sprite;
                }
                else
                {
                    Preserve(scene,actor,clip,backup);
                    var referenceClip=AssetDatabase.LoadAssetAtPath<AnimationClip>(backup+"/Walk.anim");
                    var reference=(Sprite)AnimationUtility.GetObjectReferenceCurve(referenceClip,Binding)[0].value;
                    var frames=staggered?SkullMonkStaggeredSlices.Import(reference):Slice(reference,longStride?LongStrideSheet:Sheet,longStride);
                    Undo.RecordObject(clip,"Apply Skull walk sprite sequence");
                    clip.frameRate=FPS;
                    AnimationUtility.SetObjectReferenceCurve(clip,Binding,frames.Select((sprite,i)=>new ObjectReferenceKeyframe{time=i/FPS,value=sprite}).ToArray());
                    var settings=AnimationUtility.GetAnimationClipSettings(clip);
                    settings.startTime=0;settings.stopTime=frames.Length/FPS;settings.loopTime=true;
                    AnimationUtility.SetAnimationClipSettings(clip,settings);
                    var walk=controller.layers[0].stateMachine.states.First(s=>s.state.name=="SkullMonkWalk").state;
                    walk.motion=clip;EditorUtility.SetDirty(walk);
                    controller.layers[0].stateMachine.defaultState=walk;
                    actor.GetComponent<SpriteRenderer>().sprite=frames[0];
                }
                actor.GetComponent<Animator>().runtimeAnimatorController=controller;
                EditorUtility.SetDirty(clip);EditorUtility.SetDirty(controller);EditorUtility.SetDirty(controller.layers[0].stateMachine);
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject=actor;
            }
            finally
            {
                if(opened)EditorSceneManager.CloseScene(scene,true);
                if(previous.IsValid() && previous.isLoaded)SceneManager.SetActiveScene(previous);
            }
        }

        static void Preserve(Scene scene,GameObject actor,AnimationClip clip,string backup)
        {
            if(AssetDatabase.LoadAssetAtPath<AnimationClip>(backup+"/Walk.anim")!=null)return;
            if(!AssetDatabase.IsValidFolder(backup))AssetDatabase.CreateFolder("Assets/_Project/Animations/Old Temple/Backups",Path.GetFileName(backup));
            if(!EditorSceneManager.SaveScene(scene,backup+"/TempleGate.unity",true))throw new Exception("Could not back up scene.");
            var copy=UnityEngine.Object.Instantiate(clip);copy.name="Walk";AssetDatabase.CreateAsset(copy,backup+"/Walk.anim");
            AssetDatabase.SaveAssets();
            if(!AssetDatabase.CopyAsset(SkullMonkWalkSetup.ControllerPath,backup+"/Controller.controller"))throw new Exception("Could not back up controller.");
            var saved=AssetDatabase.LoadAssetAtPath<AnimatorController>(backup+"/Controller.controller");
            foreach(var state in saved.layers[0].stateMachine.states)
                if(state.state.motion==clip){state.state.motion=copy;EditorUtility.SetDirty(state.state);}
            PrefabUtility.SaveAsPrefabAsset(actor,backup+"/Skull.prefab");
            var prefab=PrefabUtility.LoadPrefabContents(backup+"/Skull.prefab");
            try{prefab.GetComponent<Animator>().runtimeAnimatorController=saved;PrefabUtility.SaveAsPrefabAsset(prefab,backup+"/Skull.prefab");}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            AssetDatabase.SaveAssets();
        }

        static Sprite[] Slice(Sprite reference,string sheet,bool preserveVisibleSize=false)
        {
            AssetDatabase.ImportAsset(sheet);
            var importer=(TextureImporter)AssetImporter.GetAtPath(sheet);
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                ImageConversion.LoadImage(texture,File.ReadAllBytes(sheet));
                if(texture.width!=1536 || texture.height!=1024)throw new Exception("Unexpected generated sheet dimensions.");
                var pixels=texture.GetPixels32();
                var regions=new RectInt[12];var heads=new float[12];
                for(int i=0;i<12;i++)
                {
                    int cx=i%6*256,cy=(1-i/6)*512;
                    int left=cx+256,right=cx,bottom=cy+512,top=cy;
                    double headX=0,weight=0;
                    for(int y=cy;y<cy+512;y++)for(int x=cx;x<cx+256;x++)
                    {
                        var p=pixels[y*texture.width+x];
                        if(p.a<128)continue;
                        left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);
                        // Follow the skull rather than the changing width of the robe or extended sandal.
                        int fromTop=cy+511-y;
                        if(x-cx>=100 && x-cx<=190 && fromTop>=105 && fromTop<=140 && p.r>145 && p.g>120 && p.b>85)
                        {headX+=x+.5;weight++;}
                    }
                    if(weight<10 || right<=left)throw new Exception("Could not identify complete frame "+i);
                    heads[i]=(float)(headX/weight);
                    regions[i]=new RectInt(left,bottom,right-left+1,top-bottom+1);
                }
                var referenceBounds=preserveVisibleSize?SkullMonkStaggeredSlices.VisibleBounds(reference):new Rect(0,reference.bounds.min.y,1,reference.bounds.size.y);
                float ppu=regions[0].height/referenceBounds.height;
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit=ppu;importer.maxTextureSize=2048;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
                var factories=new SpriteDataProviderFactories();factories.Init();
                var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
                var existing=provider.GetSpriteRects().ToDictionary(s=>s.name,s=>s.spriteID);
                var rects=new SpriteRect[12];var pairs=new SpriteNameFileIdPair[12];
                for(int i=0;i<12;i++)
                {
                    string name="SkullMonkWalk12_"+i.ToString("00");
                    var id=existing.TryGetValue(name,out var old)?old:GUID.Generate();
                    var region=regions[i];
                    // Original pixels stay untouched; full cells keep all soft edge pixels and identical canvas size.
                    int cellX=i%6*256,cellY=(1-i/6)*512;
                    rects[i]=new SpriteRect{name=name,spriteID=id,rect=new Rect(cellX,cellY,256,512),alignment=SpriteAlignment.Custom,
                        pivot=new Vector2((heads[i]-cellX)/256f,(region.yMin-cellY-referenceBounds.yMin*ppu)/512f)};
                    pairs[i]=new SpriteNameFileIdPair(name,id);
                }
                provider.SetSpriteRects(rects);provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
                provider.Apply();importer.SaveAndReimport();
            }
            finally{UnityEngine.Object.DestroyImmediate(texture);}
            return AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        }
    }
}
