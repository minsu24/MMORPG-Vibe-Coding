using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Quest;
using EasternFantasy.Saving;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EasternFantasy.Editor
{
    public static class QuestProgressRecoveryValidation
    {
        private const string Folder = "Artifacts/QuestProgressRecovery";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Check(bool condition, string message)
        {
            File.AppendAllText(Folder + "/Validation.txt", (condition ? "PASS " : "FAIL ") + message + "\n");
            if (!condition) throw new InvalidOperationException(message);
        }
        private static void Restore(QuestManager manager, SaveGameData data) => typeof(QuestManager).GetMethod("RestoreSave", Hidden).Invoke(manager, new object[] { data });
        private static string NonQuestData(SaveGameData data)
        {
            var copy = JsonUtility.FromJson<SaveGameData>(JsonUtility.ToJson(data)); copy.quests.Clear(); return JsonUtility.ToJson(copy);
        }
        public static void Run()
        {
            Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/Validation.txt", "All-class quest history recovery\n");
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/TutorialMap/SpiderCave.unity", OpenSceneMode.Additive);
            var testRoot = new GameObject("Quest recovery validation manager");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(testRoot, scene);
            var manager = testRoot.AddComponent<QuestManager>();
            var previousInstance = QuestManager.Instance;
            string previousDirectory = SessionState.GetString("EasternFantasy.SaveTestDirectory", "");
            try
            {
                var gate = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<QuestObjectivePortalGate>(true)).First();
                var required = (QuestDefinition)typeof(QuestObjectivePortalGate).GetField("requiredQuest", Hidden).GetValue(gate);
                var portal = gate.GetComponent<PortalManager>();
                typeof(QuestObjectivePortalGate).GetField("portal", Hidden).SetValue(gate, portal);
                typeof(QuestObjectivePortalGate).GetField("manager", Hidden).SetValue(gate, manager);
                Action refresh = () => typeof(QuestObjectivePortalGate).GetMethod("Refresh", Hidden).Invoke(gate, null);

                foreach (CharacterClassId id in Enum.GetValues(typeof(CharacterClassId)))
                {
                    string path = Folder + "/OriginalSaves/class-" + (int)id + ".json";
                    if (!File.Exists(path)) continue;
                    var data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path));
                    string unchanged = NonQuestData(data); int before = data.quests.Count;
                    bool changed = QuestSaveRecovery.RecoverMissingProgress(data);
                    Check(data.quests.Count == 3 && data.quests.All(q => q.status == (int)QuestStatus.Completed), id + " has all three completed main quests");
                    Check(changed == (id == CharacterClassId.Dosa), id + " only repairs missing history (before=" + before + ")");
                    Check(NonQuestData(data) == unchanged, id + " recovery grants no duplicate EXP, currency, items or stats");
                    Check(!QuestSaveRecovery.RecoverMissingProgress(data), id + " recovery is idempotent");
                    Restore(manager, data); refresh(); Check(!portal.IsLocked, id + " actual SpiderCave gate unlocks");
                    var snapshot = EditorJsonUtility.ToJson(manager);
                    Restore(manager, new SaveGameData());
                    EditorJsonUtility.FromJsonOverwrite(snapshot, manager); refresh();
                    Check(manager.Quests.Count == 3 && !portal.IsLocked, id + " Unity serialization round-trip preserves quest history");
                    var setInstance = typeof(QuestManager).GetProperty("Instance").GetSetMethod(true);
                    setInstance.Invoke(null, new object[] { null });
                    typeof(QuestManager).GetMethod("OnEnable", Hidden).Invoke(manager, null);
                    Check(QuestManager.Instance == manager && manager.Quests.Count == 3, id + " singleton rebind after script reload preserves quests");
                    string testDirectory = Path.GetFullPath(Folder + "/LoadTests"); Directory.CreateDirectory(testDirectory);
                    File.WriteAllText(Path.Combine(testDirectory, "class-" + (int)id + ".json"), File.ReadAllText(path));
                    Check(File.Exists(Path.Combine(testDirectory, "class-" + (int)id + ".json")), id + " original save copied for isolated Play Mode load");
                }
                var fresh = new SaveGameData();
                Check(!QuestSaveRecovery.RecoverMissingProgress(fresh) && fresh.quests.Count == 0, "fresh character is not granted quests");
                Restore(manager, fresh); refresh(); Check(portal.IsLocked, "fresh character remains blocked at actual gate");
                var incomplete = new SaveGameData(); incomplete.quests.Add(new SavedQuest { id = SaveAssetCatalog.Id(required), amount = 4, status = (int)QuestStatus.Active });
                Restore(manager, incomplete); refresh(); Check(portal.IsLocked, "unfinished spider objective remains blocked");
                var ready = new SaveGameData(); ready.quests.Add(new SavedQuest { id = SaveAssetCatalog.Id(required), amount = 0, status = (int)QuestStatus.ReadyToTurnIn });
                Restore(manager, ready); refresh(); Check(!portal.IsLocked, "ready-to-turn-in status remains passable despite legacy count");
                var complete = new SaveGameData(); complete.quests.Add(new SavedQuest { id = SaveAssetCatalog.Id(required), amount = 0, status = (int)QuestStatus.Completed });
                Restore(manager, complete); refresh(); Check(!portal.IsLocked, "completed quest remains passable despite legacy count");
                var weakEvidence = new SaveGameData { toadDefeated = true, visitedScenes = new[] { "Dosa.BrokenSeal", "Dosa.Marshland" } };
                Check(!QuestSaveRecovery.RecoverMissingProgress(weakEvidence), "boss defeat and map visits alone do not invent completion");
                var activeToad = new SaveGameData();
                var toad = Resources.Load<ToadHuntDefinition>("ToadHunt");
                activeToad.quests.Add(new SavedQuest { id = SaveAssetCatalog.Id(toad.quest), amount = 0, status = (int)QuestStatus.Active });
                QuestSaveRecovery.RecoverMissingProgress(activeToad);
                Check(activeToad.quests.Count == 3 && activeToad.quests[0].status == (int)QuestStatus.Active, "accepted successor restores only its completed prerequisites");
                Check(activeToad.quests.All(q => SaveAssetCatalog.Resolve<QuestDefinition>(q.id) != null), "all recovered records reference valid quest assets");
                File.AppendAllText(Folder + "/Validation.txt", "COMPLETE\n");
            }
            finally
            {
                SessionState.SetString("EasternFantasy.SaveTestDirectory", previousDirectory);
                typeof(QuestManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { previousInstance });
                UnityEngine.Object.DestroyImmediate(testRoot);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void PrepareLive()
        {
            SessionState.SetString("QuestRecovery.PreviousSaveDirectory", SessionState.GetString("EasternFantasy.SaveTestDirectory", ""));
            SessionState.SetString("EasternFantasy.SaveTestDirectory", Path.GetFullPath(Folder + "/LoadTests"));
        }
        public static void FinishLive() => SessionState.SetString("EasternFantasy.SaveTestDirectory", SessionState.GetString("QuestRecovery.PreviousSaveDirectory", ""));
        public static void RunLive()
        {
            EditorApplication.LockReloadAssemblies();
            SaveGameService.Instance.StartCoroutine(Live());
        }
        private static IEnumerator WaitForLoad()
        {
            float deadline = Time.realtimeSinceStartup + 45;
            do { yield return null; }
            while ((SaveGameService.IsRestoring || PortalSceneTransition.IsTransitioning || EasternFantasy.UI.SceneLoadingScreen.IsVisible)
                && Time.realtimeSinceStartup < deadline);
            Check(!SaveGameService.IsRestoring && !PortalSceneTransition.IsTransitioning && !EasternFantasy.UI.SceneLoadingScreen.IsVisible, "isolated live character load finishes");
        }
        private static IEnumerator Live()
        {
            try
            {
                Application.runInBackground = true;
                foreach (CharacterClassId id in Enum.GetValues(typeof(CharacterClassId)))
                {
                    object[] args = { id, false };
                    var data = (SaveGameData)typeof(SaveGameService).GetMethod("Read", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
                    Check(data != null && data.quests.Count == 3 && data.characterClass == (int)id, id + " actual save reader restores its own three quest records");
                }
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MovementPrototype");
                yield return WaitForLoad();
                var player = UnityEngine.Object.FindFirstObjectByType<EasternFantasy.Player.PlayerEntity>();
                player.isInvincible = true;
                player.GetComponent<Rigidbody2D>().simulated = false;
                Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SpiderCave", "Dosa loads its original SpiderCave position");
                Check(QuestManager.Instance != null && QuestManager.Instance.Quests.Count == 3, "Dosa recovered quests reach live manager");
                var gate = UnityEngine.Object.FindFirstObjectByType<QuestObjectivePortalGate>();
                Check(gate != null && !gate.GetComponent<PortalManager>().IsLocked, "live SpiderCave exit is unlocked");
                var original = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(Folder + "/OriginalSaves/class-0.json"));
                var progression = player.GetComponent<EasternFantasy.Player.PlayerProgression>();
                var currency = player.GetComponent<EasternFantasy.Inventory.PlayerCurrency>();
                Check(progression.CurrentLevel == original.level && progression.CurrentExperience == original.experience && currency.Yeopjeon == original.currency,
                    "live recovery preserves level, EXP and currency without paying rewards twice");
                Check(SaveGameService.SaveNow(out string message), "write recovered history to isolated save: " + message);
                var written = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(SaveGameService.PathFor(CharacterClassId.Dosa)));
                Check(written.quests.Count == 3, "next save persists all recovered quest records");
                SaveGameService.PrepareSelection();
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MovementPrototype");
                yield return WaitForLoad();
                gate = UnityEngine.Object.FindFirstObjectByType<QuestObjectivePortalGate>();
                Check(QuestManager.Instance.Quests.Count == 3 && gate != null && !gate.GetComponent<PortalManager>().IsLocked,
                    "reselecting and reloading Dosa keeps SpiderCave unlocked");
                File.AppendAllText(Folder + "/Validation.txt", "LIVE COMPLETE\n");
            }
            finally { EditorApplication.UnlockReloadAssemblies(); }
        }
    }
}
