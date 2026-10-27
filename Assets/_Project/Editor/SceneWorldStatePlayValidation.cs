using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.Inventory;
using EasternFantasy.Quest;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EasternFantasy.EditorTools
{
    public static class SceneWorldStatePlayValidation
    {
        private const string Pending = "WorldStateValidation.Pending";
        private const string Report = "Artifacts/MapStateValidation/PlayValidation.txt";
        private static IEnumerator routine;
        private static double deadline;
        private static readonly List<string> results = new List<string>();
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [InitializeOnLoadMethod]
        private static void Install()
        {
            EditorApplication.playModeStateChanged -= Changed;
            EditorApplication.playModeStateChanged += Changed;
        }

        [MenuItem("Eastern Fantasy/Validation/Run Map State and Floating Loot %#&v")]
        public static void Begin()
        {
            if (!Application.isPlaying)
            {
                SessionState.SetBool(Pending, true);
                EditorApplication.isPlaying = true;
                return;
            }
            if (routine != null) return;
            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            results.Clear();
            Application.runInBackground = true;
            deadline = EditorApplication.timeSinceStartup + 100;
            routine = Run();
            EditorApplication.update += Tick;
        }

        private static void Changed(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            EditorApplication.delayCall += Begin;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            results.Add("PASS: " + message);
            File.WriteAllLines(Report, results);
        }

        private static void Tick()
        {
            try
            {
                if (!Application.isPlaying || EditorApplication.timeSinceStartup > deadline)
                    throw new InvalidOperationException("Map validation timed out or was interrupted.");
                if (routine.MoveNext()) return;
                results.Add("All map-state and floating-loot checks passed.");
                Finish();
            }
            catch (Exception error)
            {
                results.Add("FAIL: " + error);
                Debug.LogException(error);
                Finish();
            }
        }

        private static void Finish()
        {
            (routine as IDisposable)?.Dispose();
            routine = null;
            EditorApplication.update -= Tick;
            File.WriteAllLines(Report, results);
            EditorApplication.isPlaying = false;
        }

        private static void CompletePrerequisites(QuestManager quests, QuestDefinition quest)
        {
            if (quest == null || quests.FindProgress(quest)?.Status == QuestStatus.Completed) return;
            CompletePrerequisites(quests, quest.PrerequisiteQuest);
            quests.AcceptQuest(quest);
            quests.AddProgress(quest, quest.RequiredAmount);
            quests.CompleteQuest(quest);
        }

        private static IEnumerator Run()
        {
            var loading = SceneManager.LoadSceneAsync("Cemetery");
            while (!loading.isDone) yield return null;
            int frame = Time.frameCount + 3;
            while (Time.frameCount < frame) yield return null;
            Time.timeScale = 1f;
            var scene = SceneManager.GetActiveScene();
            var candidates = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Where(e => e != null && e.gameObject.activeInHierarchy
                && !(e is PersistentBossController) && e.HP > 0).Take(2).ToArray();
            Check(candidates.Length == 2, "Real map contains two spawned normal enemies for the round-trip test");
            var survivor = candidates[0];
            float hp = survivor.maxHP * .5f;
            survivor.HP = hp;
            Vector3 survivorPosition = survivor.transform.position;
            string survivorName = survivor.name;
            string deadName = candidates[1].name;
            Object.Destroy(candidates[1].gameObject);
            yield return null;

            var floor = new GameObject("Floating Loot Test Floor", typeof(BoxCollider2D));
            floor.transform.position = new Vector3(500, -2, 0);
            floor.GetComponent<BoxCollider2D>().size = new Vector2(10, 1);
            var prefab = AssetDatabase.LoadAssetAtPath<WorldItemPickup>("Assets/_Project/Prefabs/WorldItemPickup.prefab");
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/ToadBellNecklace.asset");
            var drop = Object.Instantiate(prefab, new Vector3(500, 0, 0), Quaternion.identity);
            drop.Initialize(item, 1, new Vector3(500, -1.5f, 0));
            float until = Time.time + 1.5f;
            while (Time.time < until) yield return null;
            var body = drop.GetComponent<Rigidbody2D>();
            Check(body.simulated && body.bodyType == RigidbodyType2D.Kinematic
                && !drop.GetComponent<BoxCollider2D>().enabled,
                "Loot settles after a real floor collision while its pickup trigger stays simulated");
            float minY = drop.transform.position.y, maxY = minY;
            until = Time.time + 1.5f;
            while (Time.time < until)
            {
                minY = Mathf.Min(minY, drop.transform.position.y);
                maxY = Mathf.Max(maxY, drop.transform.position.y);
                yield return null;
            }
            Check(maxY - minY > .02f && maxY - drop.LandingPosition.y <= .081f,
                $"Landed loot gently floats within the configured small height (range {maxY - minY:F3})");

            survivorPosition = survivor.transform.position;
            SceneWorldState.CaptureActiveScene();
            Check(!drop.gameObject.activeSelf && drop.gameObject.scene != scene,
                "Leaving a map suspends the original uncollected pickup");
            loading = SceneManager.LoadSceneAsync("PrologueVilage");
            while (!loading.isDone) yield return null;
            loading = SceneManager.LoadSceneAsync("Cemetery");
            while (!loading.isDone) yield return null;
            frame = Time.frameCount + 3;
            while (Time.frameCount < frame) yield return null;
            // sceneLoaded restores the placed enemies before their first AI update.
            var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            var restored = enemies.First(e => e.name == survivorName);
            Check(Mathf.Abs(restored.HP - hp) < .01f
                && Vector3.Distance(restored.transform.position, survivorPosition) < 1f,
                "Returning preserves the normal enemy's health and position");
            Check(!enemies.Any(e => e.name == deadName), "The removed normal enemy does not respawn on map entry");
            Check(drop != null && drop.gameObject.activeSelf && drop.gameObject.scene == SceneManager.GetActiveScene()
                && drop.Item == item && drop.Quantity == 1,
                "Returning restores the same pickup instance and reward without duplication");

            SceneWorldState.CaptureActiveScene();
            loading = SceneManager.LoadSceneAsync("PrologueVilage");
            while (!loading.isDone) yield return null;
            var maps = (System.Collections.IDictionary)typeof(SceneWorldState)
                .GetField("maps", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            foreach (var saved in maps.Values)
                saved.GetType().GetField("ExpiresAt").SetValue(saved, Time.realtimeSinceStartupAsDouble - 1);
            typeof(SceneWorldState).GetMethod("CleanupExpired", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, null);
            frame = Time.frameCount + 2;
            while (Time.frameCount < frame) yield return null;
            Check(drop == null && maps.Count == 0, "Expired map memory releases its retained pickups");
            loading = SceneManager.LoadSceneAsync("Cemetery");
            while (!loading.isDone) yield return null;
            frame = Time.frameCount + 3;
            while (Time.frameCount < frame) yield return null;
            enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            Check(enemies.Any(e => e.name == deadName) && enemies.All(e => Mathf.Abs(e.HP - e.maxHP) < .01f),
                "After retention expires the normal encounter starts fresh");

            var marsh = SceneManager.LoadSceneAsync("Marshland");
            while (!marsh.isDone) yield return null;
            frame = Time.frameCount + 3;
            while (Time.frameCount < frame) yield return null;
            var portal = Object.FindObjectsByType<PortalManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(p => (bool)typeof(PortalManager).GetField("requiresToadNecklace", Private).GetValue(p));
            portal.SetLocked(false);
            Check(portal.IsLocked, "The real Marshland forward portal remains blocked before necklace acquisition");
            var flow = ToadHuntQuestController.Instance;
            var quests = QuestManager.Instance;
            CompletePrerequisites(quests, flow.Definition.prerequisite);
            quests.AcceptQuest(flow.Definition.quest);
            quests.AddProgress(flow.Definition.quest, flow.Definition.quest.RequiredAmount);
            Check(portal.IsLocked, "Defeating the toad alone cannot bypass the necklace gate");
            quests.CompleteQuest(flow.Definition.quest);
            Check(!portal.IsLocked, "Completing the necklace reward step releases the forward portal");
        }
    }
}
