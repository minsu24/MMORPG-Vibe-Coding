using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.Quest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(QuestGiver))]
    public sealed class QuestAvailabilityIndicator : MonoBehaviour
    {
        [SerializeField] private QuestGiver questGiver;
        [SerializeField] private GameObject indicatorRoot;
        [SerializeField] private Image indicatorImage;
        [SerializeField] private Sprite availableSprite;
        [SerializeField] private Sprite activeSprite;
        [SerializeField] private Sprite readyToTurnInSprite;
        [SerializeField, Min(0f)] private float heightOffset = 0.9f;

        private QuestManager subscribedManager;
        private SpriteRenderer npcSprite;
        private Collider2D npcCollider;

        private void Awake()
        {
            if (questGiver == null)
                questGiver = GetComponent<QuestGiver>();
            npcSprite = GetComponentInChildren<SpriteRenderer>();
            npcCollider = GetComponentInChildren<Collider2D>();
        }

        private void OnEnable()
        {
            RebindManager();
        }

        private void Update()
        {
            if (subscribedManager != QuestManager.Instance)
                RebindManager();
        }

        private void LateUpdate()
        {
            if (indicatorRoot == null || !indicatorRoot.activeSelf)
                return;

            float top = npcSprite != null ? npcSprite.bounds.max.y
                : npcCollider != null ? npcCollider.bounds.max.y
                : transform.position.y + 1.3f;
            indicatorRoot.transform.position = new Vector3(
                transform.position.x, top + heightOffset, transform.position.z);
        }

        private void RebindManager()
        {
            if (subscribedManager != null)
                subscribedManager.QuestsChanged -= Refresh;

            subscribedManager = QuestManager.Instance;
            if (subscribedManager != null)
                subscribedManager.QuestsChanged += Refresh;

            Refresh();
        }

        private void Refresh()
        {
            if (indicatorRoot == null || indicatorImage == null)
                return;

            Sprite sprite = null;
            if (questGiver != null && questGiver.isActiveAndEnabled && subscribedManager != null)
            {
                if (questGiver.CanOfferQuest)
                    sprite = availableSprite;
                else
                {
                    QuestProgress progress = subscribedManager.FindProgress(questGiver.Quest);
                    if (progress != null)
                    {
                        if (progress.Status == QuestStatus.Active)
                            sprite = activeSprite;
                        else if (progress.Status == QuestStatus.ReadyToTurnIn)
                            sprite = readyToTurnInSprite;
                    }
                }
            }

            indicatorImage.sprite = sprite;
            indicatorRoot.SetActive(sprite != null);
        }

        private void OnDisable()
        {
            if (subscribedManager != null)
                subscribedManager.QuestsChanged -= Refresh;
            subscribedManager = null;
            if (indicatorRoot != null)
                indicatorRoot.SetActive(false);
        }
    }
}
