using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.Quest;
using EasternFantasy.Saving;
using EasternFantasy.Skill;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class GinsengCollectionValidation
    {
        private const string Report = "Artifacts/GinsengCollection/Validation.txt";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static GameObject holder;
        private static Scene scene;
        private static QuestManager manager;
        private static object oldQuests;
        private static int checks;
        private static bool previousRunInBackground;
        private static readonly List<string> errors = new List<string>();
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Hidden).SetValue(target, value);
        private static object Get(object target, string name) => target.GetType().GetField(name, Hidden).GetValue(target);
        private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Hidden).Invoke(target, args);
        private static void Check(bool value, string message)
        {
            File.AppendAllText(Report, (value ? "PASS " : "FAIL ") + message + "\n");
            if (!value) throw new InvalidOperationException(message);
            checks++;
        }
        private static void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
        }
        public static string Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run in Play Mode.");
            Directory.CreateDirectory("Artifacts/GinsengCollection");
            File.WriteAllText(Report, "Native Unity ginseng collection and accessory validation\n");
            checks = 0; errors.Clear();
            previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            SaveGameService.BeginAdminSession();
            SaveGameService.Instance.StartCoroutine(Guard());
            return "Started native quest, pickup, reward, indicator and regeneration checks.";
        }
        private static IEnumerator Guard()
        {
            Application.logMessageReceived += Log;
            var test = Validate();
            try
            {
                for (;;)
                {
                    bool next;
                    try { next = test.MoveNext(); }
                    catch (Exception error) { File.AppendAllText(Report, "ERROR " + error + "\n"); yield break; }
                    if (!next) break;
                    yield return test.Current;
                }
                Check(errors.Count == 0, "No Unity runtime errors: " + string.Join("; ", errors));
                File.AppendAllText(Report, "COMPLETE " + checks + " checks passed\n");
            }
            finally
            {
                Application.logMessageReceived -= Log;
                if (manager != null && oldQuests != null) Set(manager, "quests", oldQuests);
                if (holder != null) UnityEngine.Object.DestroyImmediate(holder);
                if (scene.IsValid()) SceneManager.UnloadSceneAsync(scene);
                Time.timeScale = 1f;
                Application.runInBackground = previousRunInBackground;
            }
        }
        private static IEnumerator Validate()
        {
            // Loot landing uses the shared Physics2D world, as the actual gameplay scenes do.
            scene = SceneManager.CreateScene("Ginseng Collection Validation");
            holder = new GameObject("Ginseng test fixtures"); holder.SetActive(false);
            SceneManager.MoveGameObjectToScene(holder, scene);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab");
            var player = UnityEngine.Object.Instantiate(prefab, holder.transform);
            UnityEngine.Object.DestroyImmediate(player.GetComponent<PlayerClassRuntime>());
            UnityEngine.Object.DestroyImmediate(player.GetComponent<PlayerAdminController>());
            UnityEngine.Object.DestroyImmediate(player.GetComponent<PlayerLocationSetter>());
            foreach (var behaviour in player.GetComponents<MonoBehaviour>())
                if (!(behaviour is PlayerEntity || behaviour is PlayerMovement2D || behaviour is PlayerIdleAnimation || behaviour is PlayerInventory || behaviour is PlayerProgression || behaviour is PlayerSkillSystem || behaviour is PlayerProjectileShooter)) behaviour.enabled = false;
            var preview = EditorSceneManager.OpenPreviewScene(GinsengCollectionSetup.TemplePath);
            GameObject npc;
            try
            {
                var source = preview.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).First(t => t.name == "Traveler");
                npc = UnityEngine.Object.Instantiate(source.gameObject, holder.transform);
                npc.transform.position = new Vector3(8f, 0f, 0f);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            manager = QuestManager.Instance;
            if (manager == null) manager = new GameObject("Ginseng test quest manager").AddComponent<QuestManager>();
            oldQuests = Get(manager, "quests");
            Set(manager, "quests", new List<QuestProgress>());
            holder.SetActive(true);
            var entity = player.GetComponent<PlayerEntity>();
            var inventory = player.GetComponent<PlayerInventory>();
            player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            Set(player.GetComponent<PlayerIdleAnimation>(), "inputReader", null);
            var floor = new GameObject("Floor", typeof(BoxCollider2D)); SceneManager.MoveGameObjectToScene(floor, scene);
            floor.transform.position = new Vector3(0, -2f, 0); floor.GetComponent<BoxCollider2D>().size = new Vector2(100, 1);
            yield return null;
            var stem = AssetDatabase.LoadAssetAtPath<ItemDefinition>(GinsengCollectionSetup.StemPath);
            var ring = AssetDatabase.LoadAssetAtPath<ItemDefinition>(GinsengCollectionSetup.RingPath);
            var quest = AssetDatabase.LoadAssetAtPath<QuestDefinition>(GinsengCollectionSetup.QuestPath);
            var giver = npc.GetComponent<QuestGiver>();
            var indicator = npc.GetComponent<QuestAvailabilityIndicator>();
            var image = (Image)Get(indicator, "indicatorImage");
            var indicatorRoot = (GameObject)Get(indicator, "indicatorRoot");
            Check(stem.Category == ItemCategory.Miscellaneous && quest.RequiredItem == stem && quest.RequiredAmount == 15, "Quest uses existing stem material and requires 15");
            Check(ring.EquipmentSlot == EquipmentSlot.Ring && ring.SpecialAbility == AccessoryAbility.HealthRegeneration && ring.SpecialAbilityValue == 1f, "Existing ring has regeneration ability 1 HP per second");
            Check(ring.ItemId != AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/ToadBellNecklace.asset").ItemId, "Ring identity differs from toad necklace");
            Check(giver.Quest == quest && giver.CanOfferQuest, "Actual TempleGate traveler offers the quest");
            Call(manager, "BindCollectionInventory"); Call(indicator, "Refresh");
            Check(indicatorRoot.activeSelf && image.sprite == (Sprite)Get(indicator, "availableSprite"), "Traveler available marker visible");
            Check(manager.AcceptQuest(quest), "Quest accepts");
            Check(image.sprite == (Sprite)Get(indicator, "activeSprite"), "Traveler in-progress marker visible");
            Check(inventory.AddItem(stem, 14), "Collect first 14 stems");
            var progress = manager.FindProgress(quest);
            Check(progress.CurrentAmount == 14 && progress.Status == QuestStatus.Active && !manager.CompleteQuest(quest), "14 stems cannot complete quest");
            Check(inventory.AddItem(stem) && progress.Status == QuestStatus.ReadyToTurnIn, "15 stems ready to turn in");
            Check(image.sprite == (Sprite)Get(indicator, "readyToTurnInSprite"), "Traveler completion marker visible");
            inventory.RemoveMaterial(stem);
            Check(progress.CurrentAmount == 14 && progress.Status == QuestStatus.Active, "Removing a stem reverses ready state");
            inventory.AddItem(stem, 3);
            var fillers = new List<ItemDefinition>();
            for (int i = 0; i < PlayerInventory.SlotsPerCategory; i++)
            {
                var filler = ScriptableObject.CreateInstance<ItemDefinition>();
                Set(filler, "category", ItemCategory.Equipment); fillers.Add(filler);
                inventory.AddItem(filler);
            }
            Check(!manager.CompleteQuest(quest) && inventory.GetQuantity(stem) == 17 && inventory.GetQuantity(ring) == 0, "Full equipment inventory preserves materials and reward");
            inventory.RemoveItem(fillers[0]);
            Check(manager.CompleteQuest(quest) && inventory.GetQuantity(stem) == 2 && inventory.GetQuantity(ring) == 1, "Turn-in consumes exactly 15 and awards one ring");
            Check(!manager.CompleteQuest(quest) && !manager.CanAccept(quest) && inventory.GetQuantity(ring) == 1, "Quest and ring reward cannot be repeated");
            Check(!indicatorRoot.activeSelf, "Traveler marker hides after completion");
            foreach (var item in fillers) { inventory.RemoveItem(item); UnityEngine.Object.DestroyImmediate(item); }
            inventory.RemoveItem(stem, inventory.GetQuantity(stem));
            Set(manager, "quests", new List<QuestProgress>());
            inventory.AddItem(stem, 15);
            Check(manager.AcceptQuest(quest) && manager.FindProgress(quest).Status == QuestStatus.ReadyToTurnIn, "Stems collected before acceptance count immediately");
            var save = new SaveGameData();
            Call(inventory, "CaptureSave", save); Call(manager, "CaptureSave", save);
            inventory.RemoveItem(stem, 15);
            Call(inventory, "RestoreSave", save); Call(manager, "RestoreSave", save);
            Check(manager.FindProgress(quest).Status == QuestStatus.ReadyToTurnIn && inventory.GetQuantity(stem) == 15, "Collection inventory and quest survive save round-trip");
            entity.HP = 50f;
            yield return new WaitForSeconds(1.1f);
            Check(Mathf.Abs(entity.HP - 50f) < .01f, "Unequipped ring does not heal");
            Check(inventory.TryEquip(ring), "Ring equips in ring slot");
            yield return new WaitForSeconds(1.1f);
            Check(Mathf.Abs(entity.HP - 51f) < .01f, "Equipped ring heals exactly 1 HP after one second");
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(1.1f);
            Check(Mathf.Abs(entity.HP - 51f) < .01f, "Paused game does not regenerate");
            Time.timeScale = 1f;
            inventory.TryUnequip(EquipmentSlot.Ring);
            yield return new WaitForSeconds(1.1f);
            Check(Mathf.Abs(entity.HP - 51f) < .01f, "Removing ring stops regeneration");
            inventory.TryEquip(ring); entity.HP = entity.maxHP - .5f;
            yield return new WaitForSeconds(1.1f);
            Check(entity.HP == entity.maxHP, "Regeneration never exceeds maximum HP");
            entity.HP = 0f;
            yield return new WaitForSeconds(1.1f);
            Check(entity.HP == 0f, "Ring never revives a dead player");
            entity.HP = entity.maxHP;
            ValidateDrops(stem, inventory);
        }

        private static void ValidateDrops(ItemDefinition stem, PlayerInventory inventory)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/Ginseng.prefab");
            var data = new SerializedObject(prefab.GetComponent<EnemyLootDrop>());
            var entries = data.FindProperty("drops");
            var entry = entries.GetArrayElementAtIndex(0);
            Check(entry.FindPropertyRelative("item").objectReferenceValue == stem && Mathf.Abs(entry.FindPropertyRelative("dropChance").floatValue - .7f) < .001f, "Ginseng prefab drop table uses existing stem at 70 percent");
            int drops = 0;
            bool collected = false;
            var random = UnityEngine.Random.state;
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    var enemyObject = UnityEngine.Object.Instantiate(prefab);
                    SceneManager.MoveGameObjectToScene(enemyObject, scene);
                    enemyObject.transform.position = new Vector3(0, 0, 0);
                    var enemy = enemyObject.GetComponent<EnemyController>(); enemy.enabled = false;
                    var loot = enemyObject.GetComponent<EnemyLootDrop>();
                    Set(loot, "overridesCommonLoot", true);
                    typeof(EnemyController).GetField("_reward_EXP", Hidden).SetValue(enemy, 0f);
                    typeof(EnemyController).GetField("_rewardYeopjeon", Hidden).SetValue(enemy, 0);
                    UnityEngine.Random.InitState(8317 + i);
                    if (i == 0)
                    {
                        enemy.TakeDamage(enemy.HP + 1f);
                        Check((bool)Get(loot, "hasDropped"), "Actual ginseng death invokes configured loot component");
                        Time.timeScale = 1f;
                    }
                    else Call(loot, "HandleDefeated", enemy);
                    var pickups = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<WorldItemPickup>()).Where(p => p.Item == stem).ToArray();
                    drops += pickups.Length;
                    Check(pickups.All(p => p.Quantity == 1), "Stem drop quantity is one, roll " + i);
                    Call(loot, "HandleDefeated", enemy);
                    Check(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<WorldItemPickup>()).Count(p => p.Item == stem) == pickups.Length, "Defeat callback cannot duplicate loot, roll " + i);
                    if (!collected && pickups.Length > 0)
                    {
                        int before = inventory.GetQuantity(stem);
                        Call(pickups[0], "Collect", inventory.GetComponent<Collider2D>());
                        Check(pickups[0].IsCollected && inventory.GetQuantity(stem) == before + 1, "Actual dropped stem pickup adds one item to inventory");
                        collected = true;
                    }
                    foreach (var pickup in pickups) UnityEngine.Object.DestroyImmediate(pickup.gameObject);
                    UnityEngine.Object.DestroyImmediate(enemyObject);
                }
                Check(drops >= 55 && drops <= 85, "Actual seeded drop rolls near 70 percent: " + drops + "/100");
                Check(collected, "At least one real stem pickup collected");
            }
            finally { UnityEngine.Random.state = random; }
        }
    }
}
