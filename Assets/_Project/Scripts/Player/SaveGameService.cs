using System;
using System.Collections;
using System.IO;
using EasternFantasy.Advancement;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dialogue;
using EasternFantasy.Dungeon;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.Quest;
using EasternFantasy.Skill;
using EasternFantasy.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Saving
{
    [DefaultExecutionOrder(10000)]
    public sealed class SaveGameService : MonoBehaviour
    {
        public const float AutosaveSeconds = 10f;
        public static SaveGameService Instance { get; private set; }
        public static bool IsRestoring { get; private set; }
        public static bool IsDemoSession { get; private set; }
        public static bool FirstDeathGuideCompleted { get; set; }
        private PlayerEntity player;
        private CharacterClassId characterClass;
        private float nextSave, requestedSave = float.PositiveInfinity;
        private bool ready, quitting, protectedSave;
        private SaveGameData loading;
        private static bool selectionPending;
        public static string StorageDirectory
        {
            get
            {
#if UNITY_EDITOR
                string testDirectory = UnityEditor.SessionState.GetString("EasternFantasy.SaveTestDirectory", "");
                if (!string.IsNullOrEmpty(testDirectory)) return testDirectory;
#endif
                return Path.Combine(Application.persistentDataPath, "Saves");
            }
        }
        public static string PathFor(CharacterClassId id) => Path.Combine(StorageDirectory, "class-" + (int)id + ".json");

        public static bool TryGetSavedLevel(CharacterClassId id, out int level)
        {
            var data = Read(id, out _);
            level = data != null ? data.level : 0;
            return data != null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null; IsRestoring = false; IsDemoSession = false; FirstDeathGuideCompleted = false; selectionPending = false;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (Instance == null) new GameObject("Character Save System").AddComponent<SaveGameService>();
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += SceneLoaded;
        }

        public static void Attach(PlayerEntity entity)
        {
            if (Instance == null) Install();
            if (entity == null || Instance.player == entity) return;
            Instance.Unbind();
            Instance.player = entity;
            selectionPending = false;
            Instance.characterClass = entity.GetComponent<PlayerClassRuntime>().RepresentedClass;
            Instance.ready = false; IsRestoring = true;
            Instance.protectedSave = false;
            Instance.loading = IsDemoSession ? null : Read(Instance.characterClass, out Instance.protectedSave);
            SceneWorldState.ClearForNewCharacter();
            PersistentBossController.RestoreSave(Instance.loading ?? new SaveGameData());
            MON_Goblin.RestoreSave(Instance.loading?.goblinDefeats);
            GameTimeController.SetPaused(Instance, true);
            Instance.StartCoroutine(Instance.RestoreCharacter());
        }
        public static void PrepareSelection()
        {
            SaveNow(out _);
            selectionPending = true;
        }
        public static void ResumeExistingPlayer(PlayerEntity entity)
        {
            if (!selectionPending || Instance == null) return;
            Instance.Unbind(); Instance.player = null;
            Attach(entity);
        }
        private IEnumerator RestoreCharacter()
        {
            // Player Start methods establish stat/event bindings before restoration.
            yield return null;
            bool inputEnabled = player.GetComponent<PlayerInputReader>().enabled;
            player.GetComponent<PlayerInputReader>().enabled = false;
            var body = player.GetComponent<Rigidbody2D>();
            bool simulated = body != null && body.simulated;
            if (body != null) { body.linearVelocity = Vector2.zero; body.simulated = false; }
            RestoreWorld(loading ?? new SaveGameData());
            if (loading != null)
            {
                if (!string.IsNullOrEmpty(loading.scene) && loading.scene != SceneManager.GetActiveScene().name
                    && Application.CanStreamedLevelBeLoaded(loading.scene))
                {
                    MapTransferData.Clear();
                    yield return SceneManager.LoadSceneAsync(loading.scene);
                    yield return null;
                }
                RestorePlayer(loading);
                player.GetComponent<PlayerLocationSetter>().TeleportToAndRetarget(loading.position, Quaternion.identity);
            }
            else
            {
                QuestManager.Instance?.RestoreSave(new SaveGameData());
                ToadHuntQuestController.Instance?.RestoreSave(new SaveGameData());
            }
            if (body != null) { body.linearVelocity = Vector2.zero; body.simulated = simulated; }
            player.GetComponent<PlayerInputReader>().enabled = inputEnabled;
            Physics2D.SyncTransforms();
            loading = null; ready = true; IsRestoring = false;
            MapRegionDiscovery.RecordSceneEntry(SceneManager.GetActiveScene().name);
            MonsterCodexDiscovery.RecordSceneEntry(SceneManager.GetActiveScene().name);
            GameTimeController.SetPaused(this, false);
            player.GetComponent<PlayerAdvancementSystem>()?.ResumeAfterSaveLoad();
            Bind();
            nextSave = Time.unscaledTime + AutosaveSeconds;
            RequestSave();
        }
        private void RestorePlayer(SaveGameData data)
        {
            player.GetComponent<PlayerProgression>().RestoreSave(data);
            player.GetComponent<PlayerAdvancementSystem>()?.RestoreSave(data);
            player.GetComponent<PlayerInventory>().RestoreSave(data);
            player.GetComponent<PlayerSkillSystem>().RestoreSave(data);
            PetCompanionWindowUI.Instance?.RestoreSave(data);
            ShopWindowUI.RestoreSave(data); DungeonWindowUI.RestoreSave(data); EquipmentUpgradeWindowUI.RestoreSave(data);
            QuestManager.Instance?.RestoreSave(data);
            ToadHuntQuestController.Instance?.RestoreSave(data);
            player.RestoreSave(data);
            player.GetComponent<MonkEnergy>()?.RestoreSave(data.energy);
            player.GetComponent<PlayerCurrency>()?.RestoreSave(data.currency);
            DungeonRunController.Instance?.RestoreSave(data);
        }
        private static void RestoreWorld(SaveGameData data)
        {
            FirstDeathGuideCompleted = data.firstDeathGuide;
            PersistentBossController.RestoreSave(data); MON_Goblin.RestoreSave(data.goblinDefeats);
            DialogueTrigger.RestoreSave(data.playedDialogues); DialogueManager.RestoreSave(data.rewardedDialogues);
            ShopWindowUI.RestoreSave(data); DungeonWindowUI.RestoreSave(data); EquipmentUpgradeWindowUI.RestoreSave(data);
            MapRegionDiscovery.RestoreSave(data); MonsterCodexDiscovery.RestoreSave(data.codex);
            InventoryEquipmentTutorialUI.RestoreSave(data); StatsSkillTutorialUI.RestoreSave(data); MapTutorialUI.RestoreSave(data);
            PetCompanionWindowUI.Instance?.RestoreSave(data);
        }
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Dungeon Awake starts a run and clears its defeats, so reinstate them before boss Start.
            if (IsRestoring && loading != null) PersistentBossController.RestoreSave(loading);
            if (ready) requestedSave = Time.unscaledTime + 2f;
        }
        private void Bind()
        {
            player.GetComponent<PlayerInventory>().InventoryChanged += RequestSave;
            player.GetComponent<PlayerSkillSystem>().SkillsChanged += RequestSave;
            player.GetComponent<PlayerSkillSystem>().QuickSlotsChanged += RequestSave;
            if (QuestManager.Instance != null) QuestManager.Instance.QuestsChanged += RequestSave;
        }
        private void Unbind()
        {
            if (player != null)
            {
                player.GetComponent<PlayerInventory>().InventoryChanged -= RequestSave;
                player.GetComponent<PlayerSkillSystem>().SkillsChanged -= RequestSave;
                player.GetComponent<PlayerSkillSystem>().QuickSlotsChanged -= RequestSave;
            }
            if (QuestManager.Instance != null) QuestManager.Instance.QuestsChanged -= RequestSave;
        }
        private void RequestSave() => requestedSave = Time.unscaledTime + 1f;
        private void Update()
        {
            if (!ready || IsRestoring || IsDemoSession || protectedSave) return;
            if (Time.unscaledTime < Mathf.Min(nextSave, requestedSave)) return;
            SaveNow(out _);
            nextSave = Time.unscaledTime + AutosaveSeconds; requestedSave = float.PositiveInfinity;
        }
        public static bool SaveNow(out string message)
        {
            if (IsDemoSession) { message = "시연 중에는 기존 기록을 보호하기 위해 저장하지 않습니다."; return false; }
            var service = Instance;
            if (service == null || !service.ready || IsRestoring || PortalSceneTransition.IsTransitioning
                || service.player == null || service.player.IsDead || !IsGameplayScene(SceneManager.GetActiveScene().name))
            { message = "지금은 저장할 수 없습니다."; return false; }
            if (service.protectedSave)
            { message = "저장 파일을 읽을 수 없어 기존 기록을 보호하고 있습니다."; return false; }
            try
            {
                var data = service.Capture();
                string path = PathFor(service.characterClass);
                Directory.CreateDirectory(StorageDirectory);
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                service.nextSave = Time.unscaledTime + AutosaveSeconds;
                service.requestedSave = float.PositiveInfinity;
                message = "저장했습니다."; return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Character save failed: " + exception.Message);
                message = "저장하지 못했습니다. 저장 공간을 확인해 주세요."; return false;
            }
        }
        internal SaveGameData Capture()
        {
            var progression = player.GetComponent<PlayerProgression>();
            var data = new SaveGameData
            {
                characterClass = (int)characterClass, savedUtc = DateTime.UtcNow.ToString("O"),
                scene = SceneManager.GetActiveScene().name, position = player.transform.position,
                health = player.HP, mana = player.MP, mental = player.Mental,
                energy = player.GetComponent<MonkEnergy>()?.Current ?? 0f,
                level = progression.CurrentLevel, experience = progression.CurrentExperience,
                currency = player.GetComponent<PlayerCurrency>()?.Yeopjeon ?? 0,
                firstDeathGuide = FirstDeathGuideCompleted,
                playedDialogues = DialogueTrigger.CaptureSave(), rewardedDialogues = DialogueManager.CaptureSave(),
                goblinDefeats = MON_Goblin.CaptureSave(), codex = MonsterCodexDiscovery.CaptureSave()
            };
            player.GetComponent<PlayerInventory>().CaptureSave(data);
            player.GetComponent<PlayerSkillSystem>().CaptureSave(data);
            player.GetComponent<PlayerAdvancementSystem>()?.CaptureSave(data);
            QuestManager.Instance?.CaptureSave(data); ToadHuntQuestController.Instance?.CaptureSave(data);
            PersistentBossController.CaptureSave(data); MapRegionDiscovery.CaptureSave(data);
            InventoryEquipmentTutorialUI.CaptureSave(data); StatsSkillTutorialUI.CaptureSave(data); MapTutorialUI.CaptureSave(data);
            PetCompanionWindowUI.Instance?.CaptureSave(data); DungeonRunController.Instance?.CaptureSave(data);
            ShopWindowUI.CaptureSave(data); DungeonWindowUI.CaptureSave(data); EquipmentUpgradeWindowUI.CaptureSave(data);
            return data;
        }
        internal static SaveGameData Read(CharacterClassId id, out bool protectedFile)
        {
            protectedFile = false;
            string path = PathFor(id);
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    var data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(candidate));
                    if (data == null || data.version != 1 || data.characterClass != (int)id || data.level < 1
                        || !IsGameplayScene(data.scene) || !Application.CanStreamedLevelBeLoaded(data.scene)
                        || !Finite(data.position.x) || !Finite(data.position.y) || !Finite(data.position.z)
                        || !Finite(data.health) || !Finite(data.mana) || !Finite(data.mental) || !Finite(data.energy)
                        || data.items == null || data.skills == null || data.quests == null)
                        throw new InvalidDataException("Invalid character save.");
                    protectedFile = false;
                    return data;
                }
                catch (Exception exception) { protectedFile = true; Debug.LogWarning("Could not load " + candidate + ": " + exception.Message); }
            }
            return null;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool IsGameplayScene(string scene) => !string.IsNullOrEmpty(scene) && scene != "MainScreen" && scene != "ChooseCharactor";
        public static void ResetForDemo()
        {
            if (Instance == null || IsRestoring || PortalSceneTransition.IsTransitioning) return;
            Instance.StartCoroutine(Instance.ResetDemo());
        }
        private IEnumerator ResetDemo()
        {
            IsDemoSession = true; ready = false; IsRestoring = true; Unbind();
            GameTimeController.SetPaused(this, true);
            foreach (var settings in FindObjectsByType<SettingsMenuController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) settings.Close();
            InventoryWindowUI.Instance?.SetOpen(false); EquipmentWindowUI.Instance?.CloseFromInventory();
            PlayerStatsWindowUI.Instance?.SetOpen(false); SkillWindowUI.Instance?.SetOpen(false);
            MapWindowUI.Instance?.CloseInstant(); ShopWindowUI.Instance?.Close();
            DungeonWindowUI.Instance?.Close(); EquipmentUpgradeWindowUI.Instance?.Close();
            player.GetComponent<PlayerAdvancementSystem>()?.RestoreSave(new SaveGameData());
            DialogueManager.Instance?.EndDialogue();
            RestoreWorld(new SaveGameData());
            QuestManager.Instance?.RestoreSave(new SaveGameData());
            if (ToadHuntQuestController.Instance != null) Destroy(ToadHuntQuestController.Instance.gameObject);
            if (DungeonRunController.Instance != null) Destroy(DungeonRunController.Instance.gameObject);
            SceneWorldState.ClearForNewCharacter();
            player.gameObject.SetActive(false); Destroy(player.gameObject); player = null;
            yield return null; // Release player singleton ownership before the fresh prefab awakens.
            MapTransferData.Clear();
            SceneManager.LoadScene("MovementPrototype");
        }
        private void OnApplicationPause(bool paused) { if (paused) SaveNow(out _); }
        private void OnApplicationFocus(bool focused) { if (!focused) SaveNow(out _); }
        private void OnApplicationQuit() { SaveNow(out _); quitting = true; }
        private void OnDestroy()
        {
            if (Instance != this) return;
            if (!quitting) Unbind();
            SceneManager.sceneLoaded -= SceneLoaded;
            Instance = null;
        }
    }
}
