using System;
using System.Collections.Generic;
using System.Reflection;
using EasternFantasy.CharacterSelection;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class MapDetailDiscoveryValidation
    {
        private struct Saved { public bool Exists; public int Value; }
        private static void Check(bool value,string message) { if(!value) throw new Exception(message); }
        public static string ValidateRecords()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run record validation outside Play mode.");
            var flags=BindingFlags.Static|BindingFlags.NonPublic;
            var type=typeof(MapRegionDiscovery);
            var reset=type.GetMethod("ResetSession",flags);
            var changedField=type.GetField("Changed",flags);
            var subscribers=changedField.GetValue(null);
            var sets=new Dictionary<string,HashSet<string>>();
            foreach(string name in new[]{"visitedRegions","visitedScenes","knownScenes","presentedScenes"})
                sets[name]=new HashSet<string>((HashSet<string>)type.GetField(name,flags).GetValue(null));
            var saved=new Dictionary<string,Saved>();
            foreach(string key in new[]{"Character.SelectedClass","Map.Visited.Dosa.tutorial","Map.SceneVisited.Dosa.BrokenSeal","Map.SceneKnown.Dosa.BrokenSeal","Map.DetailPresented.Dosa.MovementPrototype"})
                saved[key]=new Saved { Exists=PlayerPrefs.HasKey(key),Value=PlayerPrefs.GetInt(key) };
            int changed=0; Action onChange=()=>changed++;
            try
            {
                reset.Invoke(null,null);
                PlayerPrefs.SetInt("Character.SelectedClass",(int)CharacterClassId.Dosa);
                foreach(string key in saved.Keys) if(key!="Character.SelectedClass")PlayerPrefs.SetInt(key,1);
                Check(!MapRegionDiscovery.IsVisited(MapRegionDiscovery.Tutorial) && !MapRegionDiscovery.IsSceneNameKnown("BrokenSeal"),"Legacy saved map records leaked into session");
                Check(!MapRegionDiscovery.HasPresentedScene("MovementPrototype"),"Legacy saved reveal prevented new session animation");
                MapRegionDiscovery.Changed+=onChange;
                MapRegionDiscovery.RecordSceneEntry("MovementPrototype");
                Check(MapRegionDiscovery.IsVisited(MapRegionDiscovery.Tutorial),"Island was not discovered");
                Check(MapRegionDiscovery.IsSceneVisited("MovementPrototype"),"Entered scene was not recorded");
                Check(!MapRegionDiscovery.IsSceneVisited("BrokenSeal"),"Island discovery leaked into unvisited scenes");
                MapRegionDiscovery.RecordSceneEntry("MovementPrototype");
                Check(changed==1,"Repeated entry emitted redundant events");
                MapRegionDiscovery.MarkScenePresented("BrokenSeal");
                Check(!MapRegionDiscovery.HasPresentedScene("BrokenSeal"),"Unvisited reveal was marked complete");
                MapRegionDiscovery.MarkScenePresented("MovementPrototype");
                MapRegionDiscovery.RecordSceneEntry("PrologueVilage");
                Check(changed==2 && MapRegionDiscovery.IsSceneVisited("PrologueVilage") && MapRegionDiscovery.HasPresentedScene("MovementPrototype"),"Scene entry lost session records");
                var quest=AssetDatabase.LoadAssetAtPath<EasternFantasy.Quest.QuestDefinition>("Assets/_Project/Data/Quest/SpiderHunt.asset");
                MapRegionDiscovery.RevealQuestDestinations(quest);
                Check(MapRegionDiscovery.IsSceneNameKnown("SpiderCave") && !MapRegionDiscovery.IsSceneVisited("SpiderCave"),"Quest name-only discovery failed");
                Check(!MapRegionDiscovery.IsSceneNameKnown("BrokenSeal"),"Quest leaked an unrelated name");
                MapRegionDiscovery.RevealQuestDestinations(quest);
                Check(changed==3,"Repeated quest emitted redundant events");
                PlayerPrefs.SetInt("Character.SelectedClass",(int)CharacterClassId.Monk);
                Check(!MapRegionDiscovery.IsSceneVisited("MovementPrototype") && !MapRegionDiscovery.IsSceneNameKnown("SpiderCave") && !MapRegionDiscovery.HasPresentedScene("MovementPrototype"),"Records leaked between classes");
                MapRegionDiscovery.RecordSceneEntry("GiantFarm_01");
                Check(MapRegionDiscovery.IsSceneVisited("GiantFarm_01") && !MapRegionDiscovery.IsSceneVisited("GiantFarm_02"),"Farm detail discovery is not per scene");
                PlayerPrefs.SetInt("Character.SelectedClass",(int)CharacterClassId.Dosa);
                Check(MapRegionDiscovery.IsSceneVisited("MovementPrototype") && MapRegionDiscovery.IsSceneNameKnown("SpiderCave") && MapRegionDiscovery.HasPresentedScene("MovementPrototype"),"Session records lost on class switch");
                reset.Invoke(null,null);
                Check(!MapRegionDiscovery.IsVisited(MapRegionDiscovery.Tutorial) && !MapRegionDiscovery.IsSceneVisited("MovementPrototype") && !MapRegionDiscovery.IsSceneNameKnown("SpiderCave") && !MapRegionDiscovery.HasPresentedScene("MovementPrototype"),"New play session did not reset all records");
                PlayerPrefs.SetInt("Character.SelectedClass",(int)CharacterClassId.Monk);
                Check(!MapRegionDiscovery.IsSceneVisited("GiantFarm_01"),"New session did not reset other class");
                return "PASS: session-only visits, quest names, reveal completion, class isolation, scene retention, full session reset, and ignored legacy saves.";
            }
            finally
            {
                foreach(var pair in sets)
                {
                    var set=(HashSet<string>)type.GetField(pair.Key,flags).GetValue(null);
                    set.Clear();set.UnionWith(pair.Value);
                }
                changedField.SetValue(null,subscribers);
                foreach(var pair in saved)
                    if(pair.Value.Exists) PlayerPrefs.SetInt(pair.Key,pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key);
                PlayerPrefs.Save();
            }
        }
    }
}
