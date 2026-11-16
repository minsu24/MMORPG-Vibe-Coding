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
        public const string PrefabPath = "Assets/_Project/Resources/SkullMonkBossHealthUI.prefab";
        [MenuItem("Eastern Fantasy/Skull Monk/Configure Boss Health UI")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            AssetDatabase.ImportAsset(Art+"SkullMonkBossHealthFrame.png");
            var importer=(TextureImporter)AssetImporter.GetAtPath(Art+"SkullMonkBossHealthFrame.png");
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            var frame=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"SkullMonkBossHealthFrame.asset");
            if(frame==null)
            {
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"SkullMonkBossHealthFrame.png");
                frame=Sprite.Create(texture,new Rect(0,100,2172,524),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                frame.name="SkullMonkBossHealthFrame";AssetDatabase.CreateAsset(frame,Art+"SkullMonkBossHealthFrame.asset");
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
                ui.panel.sizeDelta=new Vector2(1280,1280f*524/2172);
                ui.panel.anchorMin=ui.panel.anchorMax=ui.panel.pivot=new Vector2(.5f,1f);
                ui.panel.anchoredPosition=new Vector2(0,ui.panel.sizeDelta.y*23f/524-8f);
                var title=ui.panel.Find("BossName").GetComponent<TMP_Text>();
                title.text="고찰의 수문장 · 해골 승려";title.fontSize=28;title.fontSizeMax=28;
                ArtworkRect(title.rectTransform,750,262,1420,358);
                ArtworkRect(ui.health.GetComponent<RectTransform>(),294,410,1879,466);
                ArtworkRect(ui.healthText.rectTransform,745,410,1430,466);ui.healthText.fontSize=18;
                ui.healthText.SetText("3000 / 3000");ui.visibility.alpha=0;
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            var scene=SceneManager.GetSceneByPath(SkullMonkWaveSetup.ScenePath);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(SkullMonkWaveSetup.ScenePath,OpenSceneMode.Additive);
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
        }
        static void ArtworkRect(RectTransform rect,float left,float top,float right,float bottom)
        {
            rect.anchorMin=new Vector2(left/2172,(624-bottom)/524);
            rect.anchorMax=new Vector2(right/2172,(624-top)/524);
            rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
}
