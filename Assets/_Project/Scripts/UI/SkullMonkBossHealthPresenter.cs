using UnityEngine;

namespace EasternFantasy.UI
{
    [RequireComponent(typeof(MON_SkullMonk))]
    [DisallowMultipleComponent]
    public sealed class SkullMonkBossHealthPresenter : MonoBehaviour
    {
        [SerializeField] private SkullMonkBossHealthUI prefab;
        [SerializeField] private GameObject[] encounterPortals;
        private MON_SkullMonk boss;
        private SkullMonkBossHealthUI ui;
        private bool suppressed;
        private bool[] portalWasActive;

        private void Start()
        {
            boss = GetComponent<MON_SkullMonk>();
            if (prefab != null)
            {
                ui = Instantiate(prefab);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ui.gameObject, gameObject.scene);
                ui.Bind(boss);
            }
            boss.Defeated += OnDefeated;
        }

        private void LateUpdate()
        {
            if (boss == null) return;
            bool fighting = boss.IsEncounterEngaged && boss.isActiveAndEnabled && boss.HP > 0f && boss.HasLivingBossTarget;
            SetSuppressed(fighting);
            if (ui != null) ui.Refresh(fighting && !GameplayHudVisibility.IsMenuScene);
        }

        private void SetSuppressed(bool value)
        {
            if (suppressed == value) return;
            suppressed = value;
            MinimapUI.SetEncounterHidden(this, value);
            if (encounterPortals == null) return;
            if (value)
            {
                portalWasActive = new bool[encounterPortals.Length];
                for (int i = 0; i < encounterPortals.Length; i++)
                {
                    var portal = encounterPortals[i];
                    if (portal == null) continue;
                    portalWasActive[i] = portal.activeSelf;
                    portal.SetActive(false);
                }
            }
            else if (portalWasActive != null)
            {
                for (int i = 0; i < encounterPortals.Length; i++)
                    if (encounterPortals[i] != null) encounterPortals[i].SetActive(portalWasActive[i]);
                portalWasActive = null;
            }
        }

        private void OnDefeated(EnemyController _) => Release();
        private void Release()
        {
            SetSuppressed(false);
            if (ui != null) ui.Refresh(false);
        }
        private void OnDisable() => Release();
        private void OnDestroy()
        {
            MinimapUI.SetEncounterHidden(this, false);
            if (boss != null) boss.Defeated -= OnDefeated;
            if (ui != null) Destroy(ui.gameObject);
        }
    }
}
