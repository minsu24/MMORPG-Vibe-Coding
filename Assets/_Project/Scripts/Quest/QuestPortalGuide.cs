using EasternFantasy.Saving;
using UnityEngine;

namespace EasternFantasy.Quest
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class QuestPortalGuide : MonoBehaviour
    {
        [SerializeField] private QuestDefinition quest;
        [SerializeField] private float bobHeight = .16f;
        [SerializeField, Min(.1f)] private float bobPeriod = 1.35f;

        private SpriteRenderer arrow;
        private Vector3 restPosition;

        private void Awake()
        {
            arrow = GetComponent<SpriteRenderer>();
            restPosition = transform.localPosition;
            arrow.enabled = false;
        }

        private void LateUpdate()
        {
            var progress = QuestManager.Instance?.FindProgress(quest);
            arrow.enabled = !SaveGameService.IsRestoring && !PortalSceneTransition.IsTransitioning
                && progress is { Status: QuestStatus.Active, CurrentAmount: 0 };
            if (!arrow.enabled) return;

            float offset = Mathf.Sin(Time.unscaledTime * (2f * Mathf.PI / bobPeriod)) * bobHeight;
            transform.localPosition = restPosition + Vector3.up * offset;
        }

        private void OnDisable()
        {
            if (arrow != null) arrow.enabled = false;
        }
    }
}
