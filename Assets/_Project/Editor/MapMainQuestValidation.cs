using System;
using System.Collections.Generic;
using System.Reflection;
using EasternFantasy.Quest;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class MapMainQuestValidation
    {
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        private static void Check(bool value,string message){if(!value)throw new Exception(message);}
        private static void Expect(MapMainQuestUI ui,string scene,MapMainQuestUI.Stage state)
        {
            typeof(MapMainQuestUI).GetMethod("LateUpdate",Flags).Invoke(ui,null);
            int visible=0;
            foreach(var marker in (Array)typeof(MapMainQuestUI).GetField("markers",Flags).GetValue(ui))
            {
                var type=marker.GetType();
                if(!((RectTransform)type.GetField("Root").GetValue(marker)).gameObject.activeSelf)continue;
                visible++;
                Check((string)type.GetField("Scene").GetValue(marker)==scene,"Marker remained in the wrong region");
                Check((MapMainQuestUI.Stage)type.GetField("State").GetValue(marker)==state,"Wrong quest state icon");
            }
            Check(visible==(scene==null?0:1),"Unexpected number of map quest markers: "+visible);
        }
        public static string Run()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Run outside Play mode.");
            var instanceField=typeof(QuestManager).GetField("<Instance>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);
            var previousManager=instanceField.GetValue(null);
            var saved=new Dictionary<string,HashSet<string>>();
            foreach(string name in new[]{"visitedRegions","visitedScenes","knownScenes","presentedScenes"})
            {
                var set=(HashSet<string>)typeof(MapRegionDiscovery).GetField(name,BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                saved[name]=new HashSet<string>(set);set.Clear();
            }
            var first=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<QuestDefinition>("Assets/_Project/Data/Quest/퀘스트 1.asset"));
            var spider=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<QuestDefinition>("Assets/_Project/Data/Quest/SpiderHunt.asset"));
            var fixture=new GameObject("Map Quest Validation") { hideFlags=HideFlags.HideAndDontSave };
            var manager=fixture.AddComponent<QuestManager>();instanceField.SetValue(null,manager);
            var root=PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/UI/MapWindow.prefab");
            try
            {
                foreach(var quest in new[]{first,spider})
                {
                    var data=new SerializedObject(quest);
                    data.FindProperty("rewardYeopjeon").intValue=0;data.FindProperty("rewardEXP").intValue=0;
                    if(quest==spider)data.FindProperty("prerequisiteQuest").objectReferenceValue=first;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                var ui=root.GetComponent<MapMainQuestUI>();var config=new SerializedObject(ui);
                var quests=config.FindProperty("mainQuests");quests.arraySize=2;
                quests.GetArrayElementAtIndex(0).objectReferenceValue=first;quests.GetArrayElementAtIndex(1).objectReferenceValue=spider;
                config.ApplyModifiedPropertiesWithoutUndo();
                var window=root.GetComponent<MapWindowUI>();
                ((GameObject)typeof(MapWindowUI).GetField("windowRoot",Flags).GetValue(window)).SetActive(true);
                window.ShowTutorialMap();root.GetComponent<MapDetailDiscoveryUI>().StopPresentation();
                Expect(ui,null,MapMainQuestUI.Stage.None);
                MapRegionDiscovery.RecordSceneEntry("PrologueVilage");
                Expect(ui,"PrologueVilage",MapMainQuestUI.Stage.Available);
                Check(manager.AcceptQuest(first),"First quest acceptance failed");
                Expect(ui,"Cemetery",MapMainQuestUI.Stage.Active);
                Check(!MapRegionDiscovery.IsSceneVisited("Cemetery"),"Quest marker revealed scenery");
                manager.AddProgress(first,first.RequiredAmount);
                Expect(ui,"PrologueVilage",MapMainQuestUI.Stage.ReadyToTurnIn);
                manager.CompleteQuest(first);
                Expect(ui,"PrologueVilage",MapMainQuestUI.Stage.Available);
                Check(manager.AcceptQuest(spider),"Spider quest prerequisite did not unlock");
                Expect(ui,"SpiderCave",MapMainQuestUI.Stage.Active);
                Check(!MapRegionDiscovery.IsSceneVisited("SpiderCave"),"Spider marker revealed unvisited scenery");
                manager.AddProgress(spider,spider.RequiredAmount-1);
                Expect(ui,"SpiderCave",MapMainQuestUI.Stage.Active);
                window.ShowOverview();Expect(ui,null,MapMainQuestUI.Stage.None);
                window.ShowTutorialMap();root.GetComponent<MapDetailDiscoveryUI>().StopPresentation();
                Expect(ui,"SpiderCave",MapMainQuestUI.Stage.Active);
                manager.AddProgress(spider,1);
                Expect(ui,"SpiderCave",MapMainQuestUI.Stage.ReadyToTurnIn);
                manager.CompleteQuest(spider);Expect(ui,null,MapMainQuestUI.Stage.None);
                return "PASS: main quest prerequisites, offer/objective/turn-in regions, partial and ready progress, spider village-to-cave transfer, completion hiding, fog preservation, and overview hiding.";
            }
            finally
            {
                instanceField.SetValue(null,previousManager);
                PrefabUtility.UnloadPrefabContents(root);
                UnityEngine.Object.DestroyImmediate(fixture);UnityEngine.Object.DestroyImmediate(first);UnityEngine.Object.DestroyImmediate(spider);
                foreach(var pair in saved)
                {
                    var set=(HashSet<string>)typeof(MapRegionDiscovery).GetField(pair.Key,BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                    set.Clear();set.UnionWith(pair.Value);
                }
            }
        }
    }
}
