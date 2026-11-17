using System;
using System.Linq;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class SkullMonkBossHealthSetup
    {
        const string Art = "Assets/_Project/Art/UI/SkullMonkBoss/";
        const string FrameImage = Art + "SkullMonkBossHealthBarOnly.png";
        const string FrameSprite = Art + "SkullMonkBossHealthBarOnly.asset";
        public const string PrefabPath = "Assets/_Project/Resources/SkullMonkBossHealthUI.prefab";
        [MenuItem("Eastern Fantasy/Skull Monk/Configure Boss Health UI")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            AssetDatabase.ImportAsset(FrameImage);
            var importer=(TextureImporter)AssetImporter.GetAtPath(FrameImage);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Bilinear;
            importer.SaveAndReimport();
            var frame=AssetDatabase.LoadAssetAtPath<Sprite>(FrameSprite);
            if(frame==null)
            {
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(FrameImage);
                // Import only the compact bar, excluding the generated canvas padding.
                frame=Sprite.Create(texture,new Rect(32,280,2110,172),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                frame.name="SkullMonkBossHealthBarOnly";AssetDatabase.CreateAsset(frame,FrameSprite);
            }
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/GiantBossHealthUI.prefab");
            var root=UnityEngine.Object.Instantiate(source);root.name="SkullMonkBossHealthUI";
            try
            {
                var previous=root.GetComponent<GiantBossHealthUI>();
                var ui=root.AddComponent<SkullMonkBossHealthUI>();
                ui.visibility=previous.visibility;ui.panel=previous.panel;ui.health=previous.health;ui.healthText=previous.healthText;
                UnityEngine.Object.DestroyImmediate(previous);
                ui.panel.name="SkullMonkBossPanel";ui.panel.GetComponent<Image>().sprite=frame;
                float panelWidth=SkullMonkBossHealthUI.MaximumPanelWidth;
                ui.panel.sizeDelta=new Vector2(panelWidth,panelWidth*172/2110);
                ui.panel.anchorMin=ui.panel.anchorMax=ui.panel.pivot=new Vector2(.5f,1f);
                ui.panel.anchoredPosition=new Vector2(0,-12f);
                var title=ui.panel.Find("BossName");
                if(title!=null)UnityEngine.Object.DestroyImmediate(title.gameObject);
                ArtworkRect(ui.health.GetComponent<RectTransform>(),255,337,1918,390);
                var track=new GameObject("EmptyHealthTrack",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                track.layer=ui.health.gameObject.layer;
                var trackRect=track.GetComponent<RectTransform>();
                trackRect.SetParent(ui.health.transform,false);trackRect.SetAsFirstSibling();
                trackRect.anchorMin=Vector2.zero;trackRect.anchorMax=Vector2.one;
                trackRect.offsetMin=trackRect.offsetMax=Vector2.zero;
                var trackImage=track.GetComponent<Image>();
                trackImage.color=new Color(.045f,.025f,.02f,.94f);trackImage.raycastTarget=false;
                ArtworkRect(ui.healthText.rectTransform,745,337,1430,390);ui.healthText.fontSize=18;
                ui.healthText.SetText("3000 / 3000");ui.visibility.alpha=0;
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            var scene=SceneManager.GetSceneByPath(SkullMonkWaveSetup.ScenePath);
            bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(SkullMonkWaveSetup.ScenePath,OpenSceneMode.Additive);
            var boss=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MON_SkullMonk>(true)).Single();
            var presenter=boss.GetComponent<SkullMonkBossHealthPresenter>();
            if(presenter==null)presenter=boss.gameObject.AddComponent<SkullMonkBossHealthPresenter>();
            var data=new SerializedObject(presenter);
            data.FindProperty("prefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<SkullMonkBossHealthUI>();
            var portals=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PortalManager>(true)).ToArray();
            var array=data.FindProperty("encounterPortals");array.arraySize=portals.Length;
            for(int i=0;i<portals.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=portals[i].gameObject;
            data.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        // Pixel coordinates in the generated image, measured from its bottom-left.
        static void ArtworkRect(RectTransform rect,float left,float bottom,float right,float top)
        {
            rect.anchorMin=new Vector2((left-32)/2110,(bottom-280)/172);
            rect.anchorMax=new Vector2((right-32)/2110,(top-280)/172);
            rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
}
