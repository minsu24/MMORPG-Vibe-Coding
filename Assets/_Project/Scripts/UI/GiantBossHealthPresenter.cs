using UnityEngine;

namespace EasternFantasy.UI
{
    // Authored on the final farm boss only; its lifetime owns the screen-space HUD.
    [RequireComponent(typeof(MON_Giant))]
    [DisallowMultipleComponent]
    public sealed class GiantBossHealthPresenter : MonoBehaviour
    {
        [SerializeField] private GiantBossHealthUI prefab;
        private MON_Giant boss;
        private GiantBossHealthUI ui;
        private bool engaged;

        public void Configure(GiantBossHealthUI value) => prefab = value;

        private void Start()
        {
            boss = GetComponent<MON_Giant>();
            if (prefab == null) return;
            ui = Instantiate(prefab);
            // Keep the canvas in the boss scene even if a different additive scene is active.
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ui.gameObject, gameObject.scene);
            ui.Bind(boss);
            boss.Defeated += OnDefeated;
        }

        private void LateUpdate()
        {
            if (ui == null || boss == null) return;
            if (boss.IsEncounterEngaged) engaged = true;
            bool modal = Dungeon.DungeonRunController.Instance != null
                && Dungeon.DungeonRunController.Instance.IsExitPromptOpen;
            ui.Refresh(engaged && boss.HasLivingBossTarget && !modal);
        }

        private void OnDefeated(EnemyController _) { if (ui != null) ui.Refresh(false); }
        private void OnDisable() { if (ui != null) ui.Refresh(false); }
        private void OnDestroy()
        {
            if (boss != null) boss.Defeated -= OnDefeated;
            if (ui != null) Destroy(ui.gameObject);
        }
    }
}
