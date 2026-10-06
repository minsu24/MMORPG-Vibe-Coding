using System;
using EasternFantasy.Player;
using EasternFantasy.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Dungeon
{
    public sealed class DungeonRunController : MonoBehaviour
    {
        public static DungeonRunController Instance { get; private set; }
        public DungeonRunUI ui;
        public float RemainingSeconds { get; private set; }
        public bool IsCleared { get; private set; }
        public bool IsExitPromptOpen => ui != null && ui.promptRoot.activeSelf;
        private DungeonDefinition definition;
        private bool leaving;
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
            var controller = Instantiate(prefab).GetComponent<DungeonRunController>();
            Instance = controller;
            controller.definition = dungeon;
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
            if (!ContainsScene(scene.name)) { Destroy(gameObject); return; }
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
            if (IsCleared || leaving || !ContainsScene(enemy.gameObject.scene.name)) return;
            IsCleared = true;
            RemainingSeconds = 30f;
            ui.SetTimer(RemainingSeconds, true, true);
            ui.ShowPrompt();
            var player = FindFirstObjectByType<PlayerMovement2D>();
            if (player != null) player.ClearInput();
        }

        private void Update()
        {
            if (leaving || !ContainsScene(SceneManager.GetActiveScene().name)) return;
            if (!IsCleared && definition.timeLimitSeconds <= 0) return;
            // Real time keeps the automatic exit running even while a menu is open.
            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.unscaledDeltaTime);
            ui.SetTimer(RemainingSeconds, IsCleared, true);
            if (RemainingSeconds <= 0f) ExitToVillage();
        }

        public void ExitToVillage()
        {
            if (leaving) return;
            const string village = "PrologueVilage";
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
            MapTransferData.SetTarget("In_Vilage_Portal");
            SceneManager.LoadSceneAsync(village);
        }

        private void OnDestroy()
        {
            foreach (var giant in giants) if (giant != null) giant.Defeated -= OnGiantDefeated;
            if (Instance == this) Instance = null;
        }
    }
}
