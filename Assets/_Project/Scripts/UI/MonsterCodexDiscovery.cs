using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.UI
{
    public static class MonsterCodexDiscovery
    {
        public const string SavePrefix = "EasternFantasy.Codex.v1.";
        public const string GiantFarmRegionId = "giant-farm";
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            Changed = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            RecordSceneEntry(SceneManager.GetActiveScene().name);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RecordSceneEntry(scene.name);

        public static void RecordSceneEntry(string sceneName)
        {
            if (sceneName == "GiantFarm_01" || sceneName == "GiantFarm_02" || sceneName == "GiantFarm_03_Boss")
                Unlock("Region." + GiantFarmRegionId);
        }

        public static bool IsRegionDiscovered(CodexRegion region) =>
            !region.hiddenUntilVisited || (!string.IsNullOrEmpty(region.id) && PlayerPrefs.GetInt(SavePrefix + "Region." + region.id, 0) == 1);

        public static bool IsMonsterDiscovered(CodexMonster monster) =>
            monster.discovered || (!string.IsNullOrEmpty(monster.id) && PlayerPrefs.GetInt(SavePrefix + "Monster." + monster.id, 0) == 1);

        public static string IdForEnemy(EnemyController enemy)
        {
            if (enemy is MON_Goblin) return "goblin";
            if (enemy is MON_Ghost) return "ghost";
            if (enemy is MON_Spider) return "spider";
            if (enemy is MON_Frog) return "frog";
            if (enemy is FarmCartAttack) return "farm-cart";
            if (enemy is MON_Boar) return "boar";
            if (enemy is MON_Giant) return "giant";
            return null;
        }

        public static void RecordDefeat(EnemyController enemy)
        {
            string id = IdForEnemy(enemy);
            if (!string.IsNullOrEmpty(id)) Unlock("Monster." + id);
        }

        private static void Unlock(string suffix)
        {
            string key = SavePrefix + suffix;
            if (PlayerPrefs.GetInt(key, 0) == 1) return;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}

