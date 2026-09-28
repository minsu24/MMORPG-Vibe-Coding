using System.Collections.Generic;
using EasternFantasy.Dialogue;
using UnityEngine;
using EasternFantasy.Shop;
using EasternFantasy.Player;
using EasternFantasy.UI;
using TMPro;
using UnityEngine.UI;

namespace EasternFantasy.Quest
{
    [DisallowMultipleComponent]
    public sealed class PlayerQuestInteraction : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float interactionRadius = 2f;
        [SerializeField] private LayerMask interactionLayers = ~0;

        private readonly List<QuestGiver> nearbyQuestGivers = new List<QuestGiver>();
        private GameObject interactionPrompt;
        private Transform currentPromptTarget;
        private float promptHeight;
        private float nextPromptRefresh;

        private void LateUpdate()
        {
            if (interactionPrompt != null && currentPromptTarget != null
                && interactionPrompt.activeSelf)
            {
                interactionPrompt.transform.position = currentPromptTarget.position
                    + Vector3.up * promptHeight;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextPromptRefresh)
                return;
            nextPromptRefresh = Time.unscaledTime + 0.1f;

            PlayerEntity entity = GetComponent<PlayerEntity>();
            if ((entity != null && entity.IsDead)
                || (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
                || (ShopWindowUI.Instance != null && ShopWindowUI.Instance.IsOpen))
            {
                currentPromptTarget = null;
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(false);
                return;
            }

            Shopkeeper shopkeeper = FindNearestShopkeeper();
            QuestGiver questGiver = shopkeeper == null ? FindNearestQuestGiver() : null;
            Transform target = shopkeeper != null ? shopkeeper.transform
                : questGiver != null ? questGiver.transform : null;
            if (target == null)
            {
                currentPromptTarget = null;
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(false);
                return;
            }

            if (interactionPrompt == null)
                CreateInteractionPrompt();
            if (currentPromptTarget != target)
            {
                currentPromptTarget = target;
                SpriteRenderer targetSprite = target.GetComponentInChildren<SpriteRenderer>();
                Collider2D targetCollider = target.GetComponentInChildren<Collider2D>();
                float top = targetSprite != null ? targetSprite.bounds.max.y
                    : targetCollider != null ? targetCollider.bounds.max.y
                    : target.position.y + 1.3f;
                promptHeight = top - target.position.y + 0.35f;
            }
            interactionPrompt.transform.position = target.position + Vector3.up * promptHeight;
            interactionPrompt.SetActive(true);
        }

        private void CreateInteractionPrompt()
        {
            interactionPrompt = new GameObject("Interact T Prompt", typeof(RectTransform),
                typeof(Canvas));
            interactionPrompt.transform.localScale = Vector3.one * 0.008f;
            RectTransform root = interactionPrompt.GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(64f, 64f);
            Canvas canvas = interactionPrompt.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 300;

            GameObject badge = new GameObject("Key Badge", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            badge.transform.SetParent(interactionPrompt.transform, false);
            RectTransform badgeRect = badge.GetComponent<RectTransform>();
            badgeRect.anchorMin = Vector2.zero;
            badgeRect.anchorMax = Vector2.one;
            badgeRect.offsetMin = badgeRect.offsetMax = Vector2.zero;
            Image background = badge.GetComponent<Image>();
            background.sprite = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            background.color = new Color(0.04f, 0.06f, 0.065f, 0.92f);
            background.raycastTarget = false;
            Outline border = badge.GetComponent<Outline>();
            border.effectColor = new Color(1f, 0.77f, 0.34f);
            border.effectDistance = new Vector2(3f, -3f);

            GameObject label = new GameObject("T", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(badge.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
            text.text = "T";
            text.fontSize = 42f;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(1f, 0.9f, 0.6f);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        public bool TryInteract()
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
                return false;

            Shopkeeper nearestShopkeeper = FindNearestShopkeeper();
            if (nearestShopkeeper != null)
            {
                nearestShopkeeper.OpenShop();
                return true;
            }

            QuestGiver nearest = FindNearestQuestGiver();
            if (nearest == null)
                return false;

            nearest.Interact();
            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            QuestGiver questGiver = other.GetComponentInParent<QuestGiver>();
            if (questGiver != null && !nearbyQuestGivers.Contains(questGiver))
                nearbyQuestGivers.Add(questGiver);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            QuestGiver questGiver = other.GetComponentInParent<QuestGiver>();
            if (questGiver != null)
                nearbyQuestGivers.Remove(questGiver);
        }

        private QuestGiver FindNearestQuestGiver()
        {
            QuestGiver nearest = null;
            float nearestSqrDistance = float.PositiveInfinity;

            Collider2D[] overlaps = Physics2D.OverlapCircleAll(
                transform.position,
                interactionRadius,
                interactionLayers);

            foreach (Collider2D overlap in overlaps)
            {
                QuestGiver detected = overlap.GetComponentInParent<QuestGiver>();
                if (detected != null && !nearbyQuestGivers.Contains(detected))
                    nearbyQuestGivers.Add(detected);
            }

            for (int i = nearbyQuestGivers.Count - 1; i >= 0; i--)
            {
                QuestGiver candidate = nearbyQuestGivers[i];
                if (candidate == null)
                {
                    nearbyQuestGivers.RemoveAt(i);
                    continue;
                }

                if (!candidate.isActiveAndEnabled)
                    continue;

                float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance > interactionRadius * interactionRadius
                    || sqrDistance >= nearestSqrDistance)
                    continue;

                nearest = candidate;
                nearestSqrDistance = sqrDistance;
            }

            return nearest;
        }

        private Shopkeeper FindNearestShopkeeper()
        {
            Shopkeeper nearest = null;
            float nearestSqrDistance = float.PositiveInfinity;
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(
                transform.position,
                interactionRadius,
                interactionLayers);

            foreach (Collider2D overlap in overlaps)
            {
                Shopkeeper candidate = overlap.GetComponentInParent<Shopkeeper>();
                if (candidate == null || !candidate.isActiveAndEnabled)
                    continue;

                float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearest = candidate;
                    nearestSqrDistance = sqrDistance;
                }
            }

            return nearest;
        }

        private void OnDisable()
        {
            nearbyQuestGivers.Clear();
            currentPromptTarget = null;
            if (interactionPrompt != null)
                Destroy(interactionPrompt);
            interactionPrompt = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.75f, 0.25f, 0.65f);
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
