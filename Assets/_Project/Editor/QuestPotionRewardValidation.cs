using System;
using System.Collections.Generic;
using System.IO;
using EasternFantasy.Inventory;
using EasternFantasy.Quest;
using UnityEditor;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public static class QuestPotionRewardValidation
    {
        private const string Pending = "QuestPotionRewardValidation.Pending";
        private static readonly List<string> results = new List<string>();

        [InitializeOnLoadMethod]
        private static void Install()
        {
            EditorApplication.playModeStateChanged -= Changed;
            EditorApplication.playModeStateChanged += Changed;
        }

        private static void Changed(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            EditorApplication.delayCall += Run;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            results.Add("PASS: " + message);
        }

        private static QuestDefinition Prepare(string path)
        {
            var quest = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<QuestDefinition>(path));
            var data = new SerializedObject(quest);
            data.FindProperty("prerequisiteQuest").objectReferenceValue = null;
            data.FindProperty("rewardEXP").intValue = 0;
            data.FindProperty("rewardYeopjeon").intValue = 0;
            data.ApplyModifiedPropertiesWithoutUndo();
            QuestManager.Instance.AcceptQuest(quest);
            QuestManager.Instance.AddProgress(quest, quest.RequiredAmount);
            return quest;
        }

        [MenuItem("Eastern Fantasy/Validation/Run Quest Potion Rewards %#&p")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                SessionState.SetBool(Pending, true);
                EditorApplication.isPlaying = true;
                return;
            }
            results.Clear();
            try
            {
                var inventory = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();
                var health = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/HealthPotion.asset");
                var mana = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/ManaPotion.asset");
                int initialHealth = inventory.GetQuantity(health), initialMana = inventory.GetQuantity(mana);
                var ghost = Prepare("Assets/_Project/Data/Quest/퀘스트 1.asset");
                Check(QuestManager.Instance.CompleteQuest(ghost)
                    && inventory.GetQuantity(health) == initialHealth + 3 && inventory.GetQuantity(mana) == initialMana + 3,
                    "Ghost quest grants exactly three health and three mana potions");
                Check(!QuestManager.Instance.CompleteQuest(ghost) && inventory.GetQuantity(health) == initialHealth + 3,
                    "Repeated turn-in cannot duplicate the potion reward");
                var spider = Prepare("Assets/_Project/Data/Quest/SpiderHunt.asset");
                Check(QuestManager.Instance.CompleteQuest(spider)
                    && inventory.GetQuantity(health) == initialHealth + 8 && inventory.GetQuantity(mana) == initialMana + 8,
                    "Spider quest grants exactly five health and five mana potions");
                int remaining = health.MaximumStack - inventory.GetQuantity(health);
                if (remaining > 0) inventory.AddItem(health, remaining);
                int manaBefore = inventory.GetQuantity(mana);
                var blocked = Prepare("Assets/_Project/Data/Quest/퀘스트 1.asset");
                Check(!QuestManager.Instance.CompleteQuest(blocked)
                    && QuestManager.Instance.FindProgress(blocked).Status == QuestStatus.ReadyToTurnIn
                    && inventory.GetQuantity(mana) == manaBefore && inventory.GetQuantity(health) == health.MaximumStack,
                    "A full potion stack blocks the entire reward without partial payment or quest completion");
                inventory.RemoveItem(health, 3);
                Check(QuestManager.Instance.CompleteQuest(blocked)
                    && inventory.GetQuantity(health) == health.MaximumStack && inventory.GetQuantity(mana) == manaBefore + 3,
                    "Freeing space allows a safe retry and grants both rewards once");
                results.Add("All quest potion reward checks passed.");
            }
            catch (Exception error) { results.Add("FAIL: " + error); Debug.LogException(error); }
            finally
            {
                Directory.CreateDirectory("Artifacts/QuestPotionRewards");
                File.WriteAllLines("Artifacts/QuestPotionRewards/Validation.txt", results);
                EditorApplication.isPlaying = false;
            }
        }
    }
}
