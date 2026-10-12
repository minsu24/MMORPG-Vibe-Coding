using System.Linq;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;
namespace EasternFantasy.EditorTools
{
    public static class MonsterCodexSetup
    {
        public const string CatalogPath="Assets/_Project/Data/MonsterCodex.asset";
        private const string Art="Assets/_Project/Art/UI/ScrollCodex/";
        [MenuItem("Eastern Fantasy/UI/Setup Monster Codex Contents")]
        public static void Run()
        {
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode before setup.");
            var sprites=Slice();
            var catalog=AssetDatabase.LoadAssetAtPath<MonsterCodexCatalog>(CatalogPath);
            if(!catalog) {
                catalog=ScriptableObject.CreateInstance<MonsterCodexCatalog>();
                catalog.regions.Add(Region("튜토리얼 맵","도깨비","유령","거미 요괴","거대 두꺼비"));
                catalog.regions.Add(Region("거인의 농장","농장 수레","멧돼지","거인"));
                var first=catalog.regions[0].monsters[0]; first.discovered=false;
                first.portrait=ImportPortrait();
                first.description="붉은 피부와 흰 갈기를 지닌 도깨비.\n가시가 박힌 곤봉을 들고 마을 외곽을 떠돈다.\n장난스러운 모습 뒤에 거친 힘을 감추고 있다.";
                AssetDatabase.CreateAsset(catalog,CatalogPath);
            }
            var root=PrefabUtility.LoadPrefabContents(ScrollCodexSetup.PrefabPath);
            try {
                var content=root.transform.Find("Window/ScrollCodexRoot/Unrolled/PaperViewport/Content");
                if(!content) throw new System.Exception("Scroll codex Content transform is missing.");
                ((RectTransform)content).sizeDelta=new Vector2(1240,530);
                var ui=content.GetComponent<MonsterCodexUI>();
                if(!ui) ui=content.gameObject.AddComponent<MonsterCodexUI>();
                ui.Configure(catalog,AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/GowunBatang-Bold SDF.asset"),
                    sprites["Description"],sprites["Nameplate"],sprites["Track"],sprites["Handle"]);
                // Keep clicks on the parchment from reaching the close backdrop.
                root.transform.Find("Window/ScrollCodexRoot/Unrolled/PaperViewport/Paper").GetComponent<Image>().raycastTarget=true;
                PrefabUtility.SaveAsPrefabAsset(root,ScrollCodexSetup.PrefabPath);
            } finally { PrefabUtility.UnloadPrefabContents(root); }
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(!Object.FindFirstObjectByType<ScrollCodexWindowUI>(FindObjectsInactive.Include)) {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ScrollCodexSetup.PrefabPath);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                instance.SetActive(true);
                Undo.RegisterCreatedObjectUndo(instance,"Add monster codex");
                EditorSceneManager.MarkSceneDirty(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Monster codex contents configured. Sample catalog can be edited at "+CatalogPath);
        }
        private static CodexRegion Region(string name,params string[] monsters)
        {
            var area=new CodexRegion {name=name,id=name=="거인의 농장"?"giant-farm":"tutorial",hiddenUntilVisited=name=="거인의 농장"};
            foreach(var monster in monsters) area.monsters.Add(new CodexMonster{name=monster,id=MonsterId(monster)});
            return area;
        }
        public static string MonsterId(string name)
        {
            switch(name) {
                case "도깨비": return "goblin";
                case "유령": return "ghost";
                case "거미 요괴": return "spider";
                case "거대 두꺼비": return "frog";
                case "농장 수레": return "farm-cart";
                case "멧돼지": return "boar";
                case "거인": return "giant";
                default: return null;
            }
        }
        private static Sprite ImportPortrait()
        {
            string path=Art+"RedDokkaebi.png"; var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=2048; importer.alphaIsTransparency=true; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static System.Collections.Generic.Dictionary<string,Sprite> Slice()
        {
            string path=Art+"CodexUI.png"; var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.alphaIsTransparency=true; importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=2048;
            importer.SaveAndReimport();
            var factories=new SpriteDataProviderFactories(); factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var old=provider.GetSpriteRects();
            string[] names={"Description","Nameplate","Track","Handle"};
            Rect[] topLeft={new Rect(50,110,1080,650),new Rect(115,825,1220,215),new Rect(1174,64,126,759),new Rect(1320,406,107,305)};
            var result=new SpriteRect[names.Length];
            for(int i=0;i<names.Length;i++) {
                var rect=topLeft[i]; rect.y=1086-rect.y-rect.height;
                var previous=old.FirstOrDefault(r=>r.name==names[i]);
                result[i]=new SpriteRect {name=names[i],rect=rect,pivot=new Vector2(.5f,.5f),border=i==0?new Vector4(120,95,120,95):i==1?new Vector4(240,70,240,70):Vector4.zero,
                    alignment=(int)SpriteAlignment.Center,spriteID=previous!=null?previous.spriteID:GUID.Generate()};
            }
            provider.SetSpriteRects(result);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(result.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply();
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s=>s.name);
        }
    }
}


