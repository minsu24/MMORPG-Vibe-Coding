using System.Linq;
using EasternFantasy.Quest;
using EasternFantasy.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class MapMainQuestSetup
    {
        [MenuItem("Eastern Fantasy/UI/Setup Detail Map Main Quests")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Run outside Play mode.");
            var first=AssetDatabase.LoadAssetAtPath<QuestDefinition>("Assets/_Project/Data/Quest/퀘스트 1.asset");
            var spider=AssetDatabase.LoadAssetAtPath<QuestDefinition>("Assets/_Project/Data/Quest/SpiderHunt.asset");
            Route(first,"PrologueVilage","Cemetery","PrologueVilage");
            Route(spider,"PrologueVilage","SpiderCave","SpiderCave");
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/UI/퀘스트 아이콘 스프라이트 시트.png");
            var available=SpriteAsset(texture,"MainQuestAvailable",new Rect(280,texture.height-370,280,290));
            var active=SpriteAsset(texture,"MainQuestActive",new Rect(280,texture.height-720,280,290));
            var ready=SpriteAsset(texture,"MainQuestReady",new Rect(280,texture.height-1045,280,280));
            const string path="Assets/_Project/Prefabs/UI/MapWindow.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var window=new SerializedObject(root.GetComponent<MapWindowUI>());
                var ui=root.GetComponent<MapMainQuestUI>();if(ui==null)ui=root.AddComponent<MapMainQuestUI>();
                var data=new SerializedObject(ui);
                data.FindProperty("detailImage").objectReferenceValue=window.FindProperty("detailImage").objectReferenceValue;
                data.FindProperty("font").objectReferenceValue=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/GowunBatang-Bold SDF.asset");
                data.FindProperty("availableSprite").objectReferenceValue=available;
                data.FindProperty("activeSprite").objectReferenceValue=active;
                data.FindProperty("readySprite").objectReferenceValue=ready;
                var quests=AssetDatabase.FindAssets("t:QuestDefinition",new[]{"Assets/_Project/Data/Quest"})
                    .Select(id=>AssetDatabase.LoadAssetAtPath<QuestDefinition>(AssetDatabase.GUIDToAssetPath(id)))
                    .Where(q=>q.Type==QuestType.Main).OrderBy(q=>q.QuestId).ToArray();
                var array=data.FindProperty("mainQuests");array.arraySize=quests.Length;
                for(int i=0;i<quests.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=quests[i];
                data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
        }
        private static void Route(QuestDefinition quest,string offer,string objective,string turnIn)
        {
            var data=new SerializedObject(quest);
            data.FindProperty("mapOfferScene").stringValue=offer;data.FindProperty("mapObjectiveScene").stringValue=objective;
            data.FindProperty("mapTurnInScene").stringValue=turnIn;data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static Sprite SpriteAsset(Texture2D texture,string name,Rect rect)
        {
            string path="Assets/_Project/Art/UI/MapSystem/"+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(existing!=null)return existing;
            var sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            sprite.name=name;AssetDatabase.CreateAsset(sprite,path);return sprite;
        }
    }
}
