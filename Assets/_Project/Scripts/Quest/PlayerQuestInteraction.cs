using System.Collections.Generic;
using EasternFantasy.Dialogue;
using UnityEngine;
using EasternFantasy.Shop;
using EasternFantasy.Player;
using EasternFantasy.UI;
using EasternFantasy.Dungeon;

namespace EasternFantasy.Quest
{
    [DisallowMultipleComponent]
    public sealed class PlayerQuestInteraction : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float interactionRadius = 2f;
        [SerializeField] private LayerMask interactionLayers = ~0;

        [Header("Interaction Prompt")]
        [Tooltip("World-space UI prefab displayed above the nearest NPC.")]
        [SerializeField] private GameObject interactionPromptPrefab;
        [SerializeField, Min(0f)] private float promptOffset = 0.35f;

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
                || (ShopWindowUI.Instance != null && ShopWindowUI.Instance.IsOpen)
                || (DungeonWindowUI.Instance != null && DungeonWindowUI.Instance.IsOpen))
            {
                currentPromptTarget = null;
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(false);
                return;
            }

            DungeonPortal dungeon = FindNearestDungeonPortal();
            Shopkeeper shopkeeper = dungeon == null ? FindNearestShopkeeper() : null;
            QuestGiver questGiver = shopkeeper == null ? FindNearestQuestGiver() : null;
            Transform target = dungeon != null ? dungeon.transform : shopkeeper != null ? shopkeeper.transform
                : questGiver != null ? questGiver.transform : null;
            if (target == null)
            {
                currentPromptTarget = null;
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(false);
                return;
            }

            if (interactionPrompt == null && !CreateInteractionPrompt())
                return;
            if (currentPromptTarget != target)
            {
                currentPromptTarget = target;
                SpriteRenderer targetSprite = target.GetComponentInChildren<SpriteRenderer>();
                Collider2D targetCollider = target.GetComponentInChildren<Collider2D>();
                float top = targetSprite != null ? targetSprite.bounds.max.y
                    : targetCollider != null ? targetCollider.bounds.max.y
                    : target.position.y + 1.3f;
                promptHeight = top - target.position.y + promptOffset;
            }
            interactionPrompt.transform.position = target.position + Vector3.up * promptHeight;
            interactionPrompt.SetActive(true);
        }

        private bool CreateInteractionPrompt()
        {
            if (interactionPromptPrefab == null)
                return false;

            interactionPrompt = Instantiate(interactionPromptPrefab);
            interactionPrompt.name = "Interact T Prompt";
            return true;
        }

        public bool TryInteract()
        {
            if (DungeonWindowUI.Instance != null && DungeonWindowUI.Instance.IsOpen) return false;
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
                return false;

            DungeonPortal dungeon = FindNearestDungeonPortal();
            if (dungeon != null) return dungeon.Interact(GetComponent<PlayerMovement2D>());

            Shopkeeper nearestShopkeeper = FindNearestShopkeeper();
            if (nearestShopkeeper != null)
            {
                nearestShopkeeper.Interact();
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

        private DungeonPortal FindNearestDungeonPortal()
        {
            DungeonPortal nearest = null;
            float distance = interactionRadius * interactionRadius;
            foreach (Collider2D overlap in Physics2D.OverlapCircleAll(transform.position, interactionRadius, interactionLayers))
            {
                DungeonPortal candidate = overlap.GetComponentInParent<DungeonPortal>();
                if (candidate == null || !candidate.isActiveAndEnabled) continue;
                float sqr = (candidate.transform.position - transform.position).sqrMagnitude;
                if (sqr <= distance) { nearest = candidate; distance = sqr; }
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
