using System.Collections.Generic;
using EasternFantasy.Dialogue;
using UnityEngine;
using EasternFantasy.Shop;

namespace EasternFantasy.Quest
{
    [DisallowMultipleComponent]
    public sealed class PlayerQuestInteraction : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float interactionRadius = 2f;
        [SerializeField] private LayerMask interactionLayers = ~0;

        private readonly List<QuestGiver> nearbyQuestGivers = new List<QuestGiver>();

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
                if (sqrDistance >= nearestSqrDistance)
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
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.75f, 0.25f, 0.65f);
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
