using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.Quest;
using EasternFantasy.Saving;
using EasternFantasy.Skill;
using EasternFantasy.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Editor
{
    public static class SaveSystemPlayValidation
    {
        private const string Folder = "Artifacts/SaveSystem";
        private const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        [InitializeOnLoadMethod]
        private static void Initialize() => EditorApplication.update += FreezeRestoredFixture;
        private static void FreezeRestoredFixture()
        {
            if (!Application.isPlaying || !SessionState.GetBool("EasternFantasy.SaveValidationFreeze", false)
                || SaveGameService.Instance == null || SaveGameService.IsRestoring) return;
            GameTimeController.SetPaused("SaveValidation", true);
        }
        private static object Call(object target, string method, params object[] args)
        {
            var type = target as Type ?? target.GetType();
            return type.GetMethod(method, Hidden).Invoke(target is Type ? null : target, args);
        }
        private static SaveGameData Capture() => (SaveGameData)Call(SaveGameService.Instance, "Capture");
        private static void Check(bool condition, string message)
        {
            File.AppendAllText(Folder + "/Validation.txt", (condition ? "PASS " : "FAIL ") + message + Environment.NewLine);
            if (!condition) throw new InvalidOperationException(message);
        }
        public static void Seed() => SaveGameService.Instance.StartCoroutine(SeedRoutine());
        private static IEnumerator SeedRoutine()
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder + "/Validation.txt", "Native Unity Play Mode save validation" + Environment.NewLine);
            SessionState.SetBool("EasternFantasy.SaveValidationFreeze", true);
            GameTimeController.SetPaused("SaveValidation", true);
            yield return SceneManager.LoadSceneAsync("Marshland");
            yield return null;
            var data = Capture();
            data.level = 4; data.experience = 13; data.currency = 1234;
            data.health = 37; data.mana = 12; data.mental = 61; data.energy = 45;
            data.skillPoints = 7; data.advancementRank = 0; data.unlockedTier = 0;
            data.inventoryTutorial = data.statsTutorial = data.mapTutorial = true;
            data.shopTutorial = data.dungeonTutorial = data.upgradeTutorial = true;
            data.companionTutorial = data.companionGuide = data.firstDeathGuide = true;
            data.companionsJson = "{\"entries\":[{\"characterClass\":1,\"companionId\":\"goblin\",\"progress\":{\"level\":3,\"equipped\":true,\"unlocked\":true}}]}";
            var catalog = Resources.Load<SaveAssetCatalog>("SaveAssetCatalog");
            data.playedDialogues = catalog.entries.Where(e => e.asset is EasternFantasy.Dialogue.DialogueSequence).Select(e => e.id).ToArray();
            data.rewardedDialogues = data.playedDialogues;
            var skill = UnityEngine.Object.FindFirstObjectByType<PlayerSkillSystem>().AvailableSkills.First(s => s.Tier == SkillTier.Base);
            data.skills.ForEach(s => s.level = s.id == SaveAssetCatalog.Id(skill) ? 1 : 0);
            data.quickSlots = new string[8]; data.quickSlots[2] = SaveAssetCatalog.Id(skill);
            var hp = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/HealthPotion.asset");
            var mp = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/ManaPotion.asset");
            var necklace = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/ToadBellNecklace.asset");
            data.items.Clear();
            data.items.Add(new SavedItem { id = SaveAssetCatalog.Id(hp), quantity = 6, slot = 3 });
            data.items.Add(new SavedItem { id = SaveAssetCatalog.Id(mp), quantity = 5, slot = 5 });
            data.items.Add(new SavedItem { id = SaveAssetCatalog.Id(necklace), quantity = 1, slot = 2, equipped = true });
            data.consumableSlots = new[] { SaveAssetCatalog.Id(hp), SaveAssetCatalog.Id(mp), null, null };
            data.quests.Clear();
            var definitions = catalog.entries.Select(e => e.asset).OfType<QuestDefinition>().ToArray();
            var toad = Resources.Load<ToadHuntDefinition>("ToadHunt").quest;
            var other = definitions.Where(q => q != toad).ToArray();
            data.quests.Add(new SavedQuest { id = SaveAssetCatalog.Id(other[0]), amount = other[0].RequiredAmount, status = 2 });
            data.quests.Add(new SavedQuest { id = SaveAssetCatalog.Id(other[1]), amount = 2, status = 0 });
            data.quests.Add(new SavedQuest { id = SaveAssetCatalog.Id(toad), amount = toad.RequiredAmount, status = 1 });
            var frog = UnityEngine.Object.FindFirstObjectByType<MON_Frog>(FindObjectsInactive.Include);
            data.toadDefeated = data.toadHasPosition = true; data.toadPosition = frog.transform.position;
            data.bossDefeats = new[] { (string)typeof(PersistentBossController).GetField("encounterKey", Hidden).GetValue(frog) };
            data.position = GameObject.Find("In_Marshland_Portal").transform.position + Vector3.right * 2f;
            Call(typeof(SaveGameService), "RestoreWorld", data);
            Call(SaveGameService.Instance, "RestorePlayer", data);
            UnityEngine.Object.FindFirstObjectByType<PlayerLocationSetter>().TeleportToAndRetarget(data.position, Quaternion.identity);
            var settings = UnityEngine.Object.FindFirstObjectByType<SettingsMenuController>();
            settings.Open();
            var button = settings.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "SaveButton");
            button.onClick.Invoke();
            Check(File.Exists(SaveGameService.PathFor(CharacterClassId.Monk)), "settings Save button writes Monk file");
            Check(!File.Exists(SaveGameService.PathFor(CharacterClassId.Dosa)), "Monk save does not create Dosa file");
            var volume = settings.GetComponentsInChildren<UnityEngine.UI.Slider>(true).First();
            Check(volume.GetComponent<RectTransform>().anchoredPosition.y < button.GetComponent<RectTransform>().anchoredPosition.y, "volume is below Save button");
            settings.Close();
            File.WriteAllText(Folder + "/Expected.json", JsonUtility.ToJson(Capture(), true));
            Check(true, "fixture: vitals, level, currency, inventory/equipment, quests, skills/slots, companion, tutorials and unclaimed toad reward");
        }
        public static void VerifyRestored()
        {
            var expected = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(Folder + "/Expected.json"));
            var actual = JsonUtility.FromJson<SaveGameData>(JsonUtility.ToJson(Capture()));
            Check(actual.scene == expected.scene && Vector3.Distance(actual.position, expected.position) < .05f, "scene and exact position restored after a new Play session");
            Check(Mathf.Approximately(actual.health, expected.health) && Mathf.Approximately(actual.mana, expected.mana)
                && Mathf.Approximately(actual.energy, expected.energy) && Mathf.Approximately(actual.mental, expected.mental), "HP, MP, mental and Monk energy restored");
            Check(actual.level == expected.level && actual.experience == expected.experience && actual.currency == expected.currency, "level, EXP and currency restored without duplicate rewards");
            Check(JsonUtility.ToJson(new ItemList { items = actual.items.ToArray() }) == JsonUtility.ToJson(new ItemList { items = expected.items.ToArray() }), "inventory quantity, slot positions and equipment restored");
            Check(actual.skillPoints == expected.skillPoints && actual.skills.All(s => expected.skills.Any(e => e.id == s.id && e.level == s.level))
                && actual.quickSlots.SequenceEqual(expected.quickSlots) && actual.consumableSlots.SequenceEqual(expected.consumableSlots), "skill allocation, unused points and both quick-slot sets restored");
            Check(actual.quests.Count == expected.quests.Count && actual.quests.All(q => expected.quests.Any(e => q.id == e.id && q.amount == e.amount && q.status == e.status)), "completed, active and ready quests restored without completion replay");
            Check(actual.companionsJson == expected.companionsJson && actual.companionTutorial && actual.mapTutorial && actual.statsTutorial && actual.inventoryTutorial, "companion growth/equipment and tutorial unlocks restored");
            var frog = UnityEngine.Object.FindFirstObjectByType<MON_Frog>(FindObjectsInactive.Include);
            Check(frog != null && !frog.gameObject.activeSelf && actual.toadDefeated && ToadHuntQuestController.Instance.Relic != null, "defeated toad stays defeated and unclaimed necklace returns");
        }
        [Serializable] private sealed class ItemList { public SavedItem[] items; }
        public static void VerifyDemo() => SaveGameService.Instance.StartCoroutine(DemoRoutine());
        public static void VerifyClassesAndAutosave() => SaveGameService.Instance.StartCoroutine(ClassesRoutine());
        private static IEnumerator ClassesRoutine()
        {
            var entity = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            float health = entity.HP;
            entity.HP = health + 2f; // No inventory/quest signal: exercise periodic autosaving.
            yield return new WaitForSecondsRealtime(SaveGameService.AutosaveSeconds + 1f);
            var disk = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(SaveGameService.PathFor(CharacterClassId.Monk)));
            Check(Mathf.Approximately(disk.health, health + 2f), "periodic autosave captures resource changes");
            entity.HP = health;
            SaveGameService.PrepareSelection();
            string monkBytes = File.ReadAllText(SaveGameService.PathFor(CharacterClassId.Monk));
            CharacterSelectionState.CreateOrSelect(CharacterClassId.Dosa);
            yield return SceneManager.LoadSceneAsync("MovementPrototype");
            yield return null;
            for (int i = 0; i < 180 && SaveGameService.IsRestoring; i++) yield return null;
            var dosa = Capture();
            Check(dosa.characterClass == 0 && dosa.level == 1 && dosa.currency == 0 && dosa.quests.Count == 0, "switching to Dosa starts independent progress");
            var dosaPlayer = UnityEngine.Object.FindFirstObjectByType<PlayerCurrency>();
            dosaPlayer.Add(222);
            string message;
            Check(SaveGameService.SaveNow(out message), "Dosa manual save succeeds");
            Check(File.ReadAllText(SaveGameService.PathFor(CharacterClassId.Monk)) == monkBytes, "Dosa save does not modify Monk save bytes");
            SaveGameService.PrepareSelection();
            CharacterSelectionState.CreateOrSelect(CharacterClassId.Monk);
            yield return SceneManager.LoadSceneAsync("MovementPrototype");
            yield return null;
            for (int i = 0; i < 180 && SaveGameService.IsRestoring; i++) yield return null;
            var monk = Capture();
            Check(monk.characterClass == 1 && monk.level == 4 && monk.currency == 1234 && monk.skillPoints == 7 && monk.scene == "Marshland", "switching back to Monk restores its own scene and progression");
        }
        private static IEnumerator DemoRoutine()
        {
            string path = SaveGameService.PathFor(CharacterClassId.Monk);
            string original = File.ReadAllText(path);
            SaveGameService.ResetForDemo();
            for (int i = 0; i < 180 && (SaveGameService.IsRestoring || SceneManager.GetActiveScene().name != "MovementPrototype"); i++) yield return null;
            var actual = Capture();
            Check(SaveGameService.IsDemoSession && actual.level == 1 && actual.currency == 0 && actual.skillPoints == 0 && actual.quests.Count == 0, "admin demo reset starts fresh in the tutorial");
            string message;
            Check(!SaveGameService.SaveNow(out message) && File.ReadAllText(path) == original, "manual save after demo reset preserves original save bytes");
            yield return new WaitForSecondsRealtime(SaveGameService.AutosaveSeconds + 1f);
            Check(File.ReadAllText(path) == original, "automatic save after demo reset preserves original save bytes");
        }
    }
}
