using System;
using EasternFantasy.Player;
using EasternFantasy.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Dungeon
{
    public sealed partial class DungeonRunController : MonoBehaviour
    {
        public static DungeonRunController Instance { get; private set; }
        public DungeonRunUI ui;
        public float RemainingSeconds { get; private set; }
        public bool IsCleared { get; private set; }
        public bool IsExitPromptOpen => ui != null && ui.promptRoot.activeSelf;
        private DungeonDefinition definition;
        private bool leaving;
        private string runId;
        private bool rewardsQueued;
        private MON_Giant[] giants = Array.Empty<MON_Giant>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        public static void Begin(DungeonDefinition dungeon)
        {
            if (dungeon == null) return;
            if (Instance != null && Instance.definition == dungeon) return;
            if (Instance != null) Destroy(Instance.gameObject);
            var prefab = Resources.Load<GameObject>("DungeonRunUI");
            if (prefab == null) { Debug.LogError("DungeonRunUI resource prefab is missing."); return; }
            PersistentBossController.BeginDungeonRun();
            SceneWorldState.Invalidate(dungeon.sceneName);
            foreach (string sceneName in dungeon.continuationScenes ?? Array.Empty<string>())
                SceneWorldState.Invalidate(sceneName);
            var controller = Instantiate(prefab).GetComponent<DungeonRunController>();
            Instance = controller;
            controller.definition = dungeon;
            controller.runId = Guid.NewGuid().ToString("N");
            controller.RemainingSeconds = dungeon.timeLimitSeconds;
            controller.ui.HidePrompt();
            controller.ui.SetTimer(controller.RemainingSeconds, false, dungeon.timeLimitSeconds > 0);
            DontDestroyOnLoad(controller.gameObject);
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private bool ContainsScene(string scene) => definition != null &&
            (scene == definition.sceneName || Array.IndexOf(definition.continuationScenes ?? Array.Empty<string>(), scene) >= 0);

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            if (!ContainsScene(scene.name))
            {
                if (IsCleared) QueueClearRewards();
                Destroy(gameObject);
                return;
            }
            BindGiants();
        }

        private void Start() => BindGiants();
        private void BindGiants()
        {
            foreach (var giant in giants) if (giant != null) giant.Defeated -= OnGiantDefeated;
            giants = FindObjectsByType<MON_Giant>(FindObjectsSortMode.None);
            foreach (var giant in giants) giant.Defeated += OnGiantDefeated;
        }

        private void OnGiantDefeated(EnemyController enemy)
        {
            if (IsCleared || hasFailed || leaving || !ContainsScene(enemy.gameObject.scene.name)) return;
            IsCleared = true;
            RemainingSeconds = 30f;
            QueueClearRewards();
            ui.SetTimer(RemainingSeconds, true, true);
            ui.ShowPrompt();
            PlayResultAudio(true);
            var player = FindFirstObjectByType<PlayerMovement2D>();
            if (player != null) player.ClearInput();
        }

        private void Update()
        {
            if (EasternFantasy.Saving.SaveGameService.IsRestoring) return;
            if (leaving || !ContainsScene(SceneManager.GetActiveScene().name)) return;
            if (hasFailed) return;
            if (!IsCleared && definition.timeLimitSeconds <= 0) return;
            // Real time keeps the automatic exit running even while a menu is open.
            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.unscaledDeltaTime);
            ui.SetTimer(RemainingSeconds, IsCleared, true);
            if (RemainingSeconds <= 0f)
            {
                if (IsCleared) ExitToVillage();
                else StartCoroutine(ShowTimeoutFailure());
            }
        }

        public void ExitToVillage()
        {
            if (leaving || PortalSceneTransition.IsTransitioning) return;
            string village = string.IsNullOrEmpty(definition.returnSceneName) ? "PrologueVilage" : definition.returnSceneName;
            if (!Application.CanStreamedLevelBeLoaded(village))
            { Debug.LogError("PrologueVilage must be enabled in Build Settings."); return; }
            leaving = true;
            ui.HidePrompt();
            var player = FindFirstObjectByType<PlayerMovement2D>();
            if (player != null)
            {
                player.ClearInput();
                var entity = player.GetComponent<PlayerEntity>();
                var death = player.GetComponent<PlayerDeathController>();
                if (entity != null && entity.IsDead && death != null)
                { death.Respawn(); return; }
                if (!player.TryGetComponent<PlayerLocationSetter>(out _)) player.gameObject.AddComponent<PlayerLocationSetter>();
            }
            if (!PortalSceneTransition.TryLoad(village, definition.returnSpawnPointName,
                success => { if (this != null) leaving = false; }, playPortalAudio: true)) leaving = false;
        }

        private void OnDestroy()
        {
            RestoreResultMusic();
            foreach (var giant in giants) if (giant != null) giant.Defeated -= OnGiantDefeated;
            if (Instance == this) Instance = null;
        }

        private void QueueClearRewards()
        {
            if (!IsCleared || rewardsQueued) return;
            if (string.IsNullOrEmpty(runId)) runId = Guid.NewGuid().ToString("N");
            rewardsQueued = true;
            DungeonRewardProgress.Queue(runId, definition);
            EasternFantasy.Saving.SaveGameService.SaveNow(out _);
        }
    }
}
