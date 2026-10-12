using System.Collections.Generic;
using System.Linq;
using EasternFantasy.UI;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.EditorTools
{
    public static class SkillScrollEffectSetup
    {
        private const string PrefabPath="Assets/_Project/Prefabs/UI/SkillWindow.prefab";
        private const string Art="Assets/_Project/Art/UI/SkillScroll/";
        [MenuItem("Eastern Fantasy/UI/Setup Skill Scroll Opening Effect")]
        public static void Run()
        {
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
            var left=Import("LeftRoller.png",new Rect(70,20,390,1430));
            var paper=Import("Paper.png",new Rect(45,104,1585,718));
            var right=Import("RightRoller.png",new Rect(395,10,425,1500));
            var root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try {
                var controller=root.GetComponent<SkillWindowUI>();
                var data=new SerializedObject(controller);
                var panel=(GameObject)data.FindProperty("windowRoot").objectReferenceValue;
                if(panel.GetComponent<SkillScrollOpenEffect>()) {
                    Debug.Log("Skill scroll effect is already configured."); return;
                }
                var groups=new List<CanvasGroup>();
                foreach(Transform child in panel.transform) {
                    var group=child.GetComponent<CanvasGroup>();
                    if(!group) group=child.gameObject.AddComponent<CanvasGroup>();
                    groups.Add(group);
                }
                var panelRect=(RectTransform)panel.transform;
                var overlay=Rect("ScrollOpeningOverlay",panel.transform,panelRect.sizeDelta);
                var overlayGroup=overlay.gameObject.AddComponent<CanvasGroup>();
                var mask=Rect("PaperViewport",overlay,new Vector2(0,panelRect.rect.height));
                mask.gameObject.AddComponent<RectMask2D>();
                var image=Picture("Paper",mask,paper,panelRect.sizeDelta);
                image.preserveAspect=true; image.raycastTarget=true;
                float height=panelRect.rect.height;
                var leftImage=Picture("LeftRoller",overlay,left,new Vector2(height*left.rect.width/left.rect.height,height));
                var rightImage=Picture("RightRoller",overlay,right,new Vector2(height*right.rect.width/right.rect.height,height));
                var effect=panel.AddComponent<SkillScrollOpenEffect>();
                effect.Configure(mask,leftImage.rectTransform,rightImage.rectTransform,overlayGroup,groups.ToArray(),panel.GetComponent<Image>());
                overlay.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                Debug.Log("Skill scroll opening effect added; original panel, skill slots and controller retained.");
            } finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Eastern Fantasy/UI/Connect Skill Scroll Effect In Active Scene")]
        public static void ConnectScene()
        {
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var sourceController=prefab.GetComponent<SkillWindowUI>();
            var sourceData=new SerializedObject(sourceController);
            var sourcePanel=(GameObject)sourceData.FindProperty("windowRoot").objectReferenceValue;
            var source=sourcePanel.transform.Find("ScrollOpeningOverlay");
            if(!source) throw new System.Exception("Configure the skill prefab first.");
            foreach(var controller in Object.FindObjectsByType<SkillWindowUI>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
                var data=new SerializedObject(controller);
                var panel=(GameObject)data.FindProperty("windowRoot").objectReferenceValue;
                if(!panel || panel.GetComponent<SkillScrollOpenEffect>()) continue;
                var groups=new List<CanvasGroup>();
                foreach(Transform child in panel.transform) {
                    var group=child.GetComponent<CanvasGroup>();
                    if(!group) group=Undo.AddComponent<CanvasGroup>(child.gameObject);
                    groups.Add(group);
                }
                var art=Object.Instantiate(source.gameObject,panel.transform,false);
                art.name="ScrollOpeningOverlay"; art.SetActive(false);
                Undo.RegisterCreatedObjectUndo(art,"Skill scroll opening art");
                var effect=Undo.AddComponent<SkillScrollOpenEffect>(panel);
                effect.Configure((RectTransform)art.transform.Find("PaperViewport"),
                    (RectTransform)art.transform.Find("LeftRoller"),(RectTransform)art.transform.Find("RightRoller"),
                    art.GetComponent<CanvasGroup>(),groups.ToArray(),panel.GetComponent<Image>());
                EditorUtility.SetDirty(effect);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panel.scene);
            }
        }

        private static Sprite Import(string file,Rect topLeft)
        {
            string path=Art+file;
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.maxTextureSize=2048; importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var factories=new SpriteDataProviderFactories(); factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var old=provider.GetSpriteRects().FirstOrDefault();
            float textureHeight=file=="Paper.png"?941:1536;
            topLeft.y=textureHeight-topLeft.y-topLeft.height;
            var rect=new SpriteRect {name="ScrollArt",rect=topLeft,pivot=new Vector2(.5f,.5f),
                alignment=(int)SpriteAlignment.Center,spriteID=old!=null?old.spriteID:GUID.Generate()};
            provider.SetSpriteRects(new[]{rect});
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(new[]{new SpriteNameFileIdPair(rect.name,rect.spriteID)});
            provider.Apply();
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.layer=parent.gameObject.layer;
            go.transform.SetParent(parent,false); var rt=(RectTransform)go.transform;
            rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f); rt.sizeDelta=size; return rt;
        }
        private static Image Picture(string name,Transform parent,Sprite sprite,Vector2 size)
        {
            var rt=Rect(name,parent,size); var image=rt.gameObject.AddComponent<Image>();
            image.sprite=sprite; image.raycastTarget=false; return image;
        }
    }
}

