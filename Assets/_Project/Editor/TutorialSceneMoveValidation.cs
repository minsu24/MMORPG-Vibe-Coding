using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Player;
using EasternFantasy.Quest;
using EasternFantasy.Saving;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EasternFantasy.EditorTools
{
    public static class TutorialSceneMoveValidation
    {
        private const string Report = "Artifacts/TutorialSceneMove/PlayValidation.txt";
        private const BindingFlags Hidden = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static readonly string[] Names = { "MovementPrototype", "PrologueVilage", "Cemetery", "SpiderCave", "Marshland", "BrokenSeal" };
        private static readonly string[] Entries = { "Out_Tuto_Portal", "In_Vilage_Portal", "In_Cemetery_Portal", "In_SpiderCave_Portal", "In_Marshland_Portal", "In_BrokenSeal_Portal" };
        private static void Check(bool condition, string message)
        {
            File.AppendAllText(Report, (condition ? "PASS " : "FAIL ") + message + Environment.NewLine);
            if (!condition) throw new InvalidOperationException(message);
        }
        private static SaveGameData Capture() => (SaveGameData)typeof(SaveGameService).GetMethod("Capture", Hidden).Invoke(SaveGameService.Instance, null);
        private static string LegacyKey(string stable)
        {
            int separator = stable.IndexOf(':');
            return "Assets/_Project/Scenes/" + stable.Substring(0, separator) + ".unity" + stable.Substring(separator);
        }
        public static void Run()
        {
            if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("EasternFantasy.SaveTestDirectory", "")))
                throw new InvalidOperationException("Run in Play mode with isolated test save storage.");
            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllText(Report, "Native Unity Play mode: moved scenes and legacy save compatibility" + Environment.NewLine);
            SaveGameService.Instance.StartCoroutine(Validate());
        }
        private static IEnumerator Validate()
        {
            GameTimeController.SetPaused("TutorialSceneMoveValidation", true);
            yield return SceneManager.LoadSceneAsync("MovementPrototype");
            while (SaveGameService.IsRestoring) yield return null;
            yield return null;
            var player = Object.FindFirstObjectByType<PlayerEntity>();
            player.isInvincible = true;
            var goblin = Object.FindFirstObjectByType<MON_Goblin>(FindObjectsInactive.Include);
            string goblinKey = (string)typeof(MON_Goblin).GetProperty("EncounterKey", Hidden).GetValue(goblin);
            Check(goblinKey.StartsWith("MovementPrototype:"), "goblin encounter identity no longer depends on its folder");
            yield return SceneManager.LoadSceneAsync("Marshland");
            yield return null;
            var frog = Object.FindFirstObjectByType<MON_Frog>(FindObjectsInactive.Include);
            string frogKey = (string)typeof(PersistentBossController).GetField("encounterKey", Hidden).GetValue(frog);
            Check(frogKey.StartsWith("Marshland:"), "boss encounter identity no longer depends on its folder");
            var data = Capture();
            data.scene = "Marshland";
            data.position = GameObject.Find("In_Marshland_Portal").transform.position + Vector3.right * 2f;
            data.level = 4; data.experience = 13; data.currency = 4321; data.skillPoints = 7;
            data.health = 37; data.mana = 12; data.energy = 45;
            data.inventoryTutorial = data.statsTutorial = data.mapTutorial = true;
            data.shopTutorial = data.dungeonTutorial = data.upgradeTutorial = true;
            data.bossDefeats = new[] { LegacyKey(frogKey) };
            data.goblinDefeats = new[] { LegacyKey(goblinKey) };
            data.dungeonDefeats = new[] { "Assets/_Project/Scenes/GiantFarm/GiantFarm_03_Boss.unity:MON_Giant:test-encounter" };
            data.toadDefeated = data.toadHasPosition = true; data.toadPosition = frog.transform.position;
            var definition = Resources.Load<ToadHuntDefinition>("ToadHunt");
            data.quests.Clear();
            data.quests.Add(new SavedQuest { id = SaveAssetCatalog.Id(definition.quest), amount = definition.quest.RequiredAmount, status = (int)QuestStatus.ReadyToTurnIn });
            data.playedDialogues = Resources.Load<SaveAssetCatalog>("SaveAssetCatalog").entries
                .Where(e => e.asset is EasternFantasy.Dialogue.DialogueSequence).Select(e => e.id).ToArray();

            // Write the old on-disk format, then enter through the actual character restore flow.
            typeof(SaveGameService).GetField("ready", Hidden).SetValue(SaveGameService.Instance, false);
            Directory.CreateDirectory(SaveGameService.StorageDirectory);
            File.WriteAllText(SaveGameService.PathFor(CharacterClassId.Monk), JsonUtility.ToJson(data, true));
            SaveGameService.PrepareSelection();
            yield return SceneManager.LoadSceneAsync("MovementPrototype");
            while (SaveGameService.IsRestoring) yield return null;
            yield return null;
            player = Object.FindFirstObjectByType<PlayerEntity>();
            Check(SceneManager.GetActiveScene().path == "Assets/_Project/Scenes/TutorialMap/Marshland.unity"
                && Vector3.Distance(player.transform.position, data.position) < .01f, "legacy save restores the scene and exact position after relocation");
            var restored = Capture();
            Check(restored.level == 4 && restored.experience == 13 && restored.currency == 4321 && restored.skillPoints == 7
                && Mathf.Approximately(restored.health, 37f), "legacy save retains progression, currency, points and health");
            frog = Object.FindFirstObjectByType<MON_Frog>(FindObjectsInactive.Include);
            Check(frog != null && frog.WasDefeatedThisSession && !frog.gameObject.activeSelf, "old path-based toad defeat stays defeated");
            Check(ToadHuntQuestController.Instance.Relic != null
                && QuestManager.Instance.FindProgress(definition.quest)?.Status == QuestStatus.ReadyToTurnIn,
                "unclaimed necklace and quest progress survive relocation");
            Check(restored.bossDefeats.Contains(frogKey) && restored.goblinDefeats.Contains(goblinKey)
                && restored.dungeonDefeats.Contains("GiantFarm_03_Boss:MON_Giant:test-encounter"), "boss, goblin and dungeon encounter records normalize to folder-independent IDs");
            Check(SceneStateIdentity.NormalizeEncounterKey(LegacyKey(frogKey).Replace("/Scenes/", "/Scenes/TutorialMap/")) == frogKey
                && SceneStateIdentity.NormalizeEncounterKey(frogKey) == frogKey, "both legacy and moved-folder paths migrate idempotently");
            string message;
            Check(SaveGameService.SaveNow(out message), "restored character can be saved");
            var saved = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(SaveGameService.PathFor(CharacterClassId.Monk)));
            Check(saved.version == 1 && saved.bossDefeats.Contains(frogKey) && saved.goblinDefeats.Contains(goblinKey), "next save writes stable IDs without changing save format");

            var targets = new List<Tuple<string, string>>();
            var objectsByScene = new Dictionary<string, HashSet<string>>();
            for (int index = 0; index < Names.Length; index++)
            {
                Check(Application.CanStreamedLevelBeLoaded(Names[index]), Names[index] + " remains loadable by its original name");
                Check(PortalSceneTransition.TryLoad(Names[index], Entries[index]), Names[index] + " portal transfer starts");
                while (PortalSceneTransition.IsTransitioning) yield return null;
                Scene scene = SceneManager.GetActiveScene();
                Check(scene.path == "Assets/_Project/Scenes/TutorialMap/" + Names[index] + ".unity", Names[index] + " loads from TutorialMap");
                Check(Vector3.Distance(player.transform.position, GameObject.Find(Entries[index]).transform.position) < .01f, Names[index] + " entry spawn remains correct");
                var names = new HashSet<string>();
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true)) names.Add(transform.name);
                objectsByScene[scene.name] = names;
                foreach (var portal in Object.FindObjectsByType<PortalManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    string destination = (string)typeof(PortalManager).GetField("sceneName", Hidden).GetValue(portal);
                    string target = (string)typeof(PortalManager).GetField("targetPointName", Hidden).GetValue(portal);
                    if (string.IsNullOrEmpty(destination)) continue;
                    Check(Application.CanStreamedLevelBeLoaded(destination), scene.name + "/" + portal.name + " destination stays in the build");
                    targets.Add(Tuple.Create(destination, target));
                }
                if (scene.name == "MovementPrototype")
                {
                    goblin = Object.FindFirstObjectByType<MON_Goblin>(FindObjectsInactive.Include);
                    Check(goblin != null && goblin.WasDefeatedThisSession && !goblin.gameObject.activeSelf, "old tutorial goblin defeat remains effective on scene revisit");
                }
            }
            foreach (var target in targets)
                if (objectsByScene.TryGetValue(target.Item1, out var names))
                    Check(names.Contains(target.Item2), target.Item1 + "/" + target.Item2 + " portal target still exists");
            GameTimeController.SetPaused("TutorialSceneMoveValidation", false);
            File.AppendAllText(Report, "All scene relocation checks passed." + Environment.NewLine);
        }
    }
}
