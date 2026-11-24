using EasternFantasy.Player;
using EasternFantasy.Saving;
using EasternFantasy.UI;
using UnityEngine;

namespace EasternFantasy.Quest
{
    [RequireComponent(typeof(SpriteRenderer), typeof(QuestGiver))]
    public sealed class NoviceMonkHunt : MonoBehaviour
    {
        [SerializeField] private QuestDefinition bellQuest;

        private SpriteRenderer sprite;
        private QuestGiver huntGiver;
        private Collider2D nearbyPlayer;
        private bool offeredDuringOverlap;

        private void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            huntGiver = GetComponent<QuestGiver>();
            sprite.enabled = false;
        }

        private void OnEnable() => MON_WildCat.AnyDefeated += CountWildCat;

        private void OnDisable()
        {
            MON_WildCat.AnyDefeated -= CountWildCat;
            if (huntGiver != null) huntGiver.enabled = false;
            nearbyPlayer = null;
            offeredDuringOverlap = false;
        }

        private void CountWildCat(MON_WildCat cat)
        {
            if (cat.gameObject.scene != gameObject.scene) return;
            var manager = QuestManager.Instance;
            var hunt = huntGiver.Quest;
            if (manager == null || !manager.AddProgress(hunt)) return;
            if (manager.CompleteQuest(hunt))
            {
                SyncBellProgress(manager, manager.FindProgress(bellQuest), QuestStatus.Completed);
                SystemNoticeUI.Show("삵 5마리를 처치했습니다. 산길 끝 포탈로 천년불궁에 들어가세요.", this);
            }
        }

        private void Update()
        {
            var manager = QuestManager.Instance;
            var progress = manager?.FindProgress(bellQuest);
            sprite.enabled = progress != null && progress.Status != QuestStatus.Completed
                && !SaveGameService.IsRestoring;
            huntGiver.enabled = sprite.enabled;
            if (!sprite.enabled) return;

            SyncBellProgress(manager, progress, manager.FindProgress(huntGiver.Quest)?.Status);
            TryOfferHunt();
        }

        private void SyncBellProgress(QuestManager manager, QuestProgress progress, QuestStatus? huntStatus)
        {
            if (progress?.Status != QuestStatus.Active || !huntStatus.HasValue) return;
            int amount = huntStatus == QuestStatus.Completed ? 2 : 1;
            if (progress.CurrentAmount < amount)
                manager.AddProgress(bellQuest, amount - progress.CurrentAmount);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player")) nearbyPlayer = other;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other != nearbyPlayer) return;
            nearbyPlayer = null;
            offeredDuringOverlap = false;
        }

        private void TryOfferHunt()
        {
            // A portal arrival can overlap the trigger before gameplay input is restored.
            if (offeredDuringOverlap || nearbyPlayer == null || !nearbyPlayer.isActiveAndEnabled
                || !huntGiver.CanOfferQuest || MapTutorialUI.IsRunning || Time.timeScale <= 0f)
                return;
            var player = nearbyPlayer.GetComponentInParent<PlayerInputReader>();
            if (player != null && player.CanControlGameplay)
                offeredDuringOverlap = huntGiver.OfferQuest();
        }
    }
}
