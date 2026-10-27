using UnityEngine;

namespace EasternFantasy.Quest
{
    // Deliberately has no WorldItemPickup: touching this object cannot grant its reward.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class QuestRewardRelic : MonoBehaviour
    {
        [SerializeField] private Transform completionMarker;
        private Vector3 markerRest;
        public ToadHuntQuestController Owner { get; private set; }
        public void Initialize(ToadHuntQuestController owner)
        {
            Owner = owner;
            var floating = GetComponent<EasternFantasy.Inventory.DroppedItemFloat>();
            if (floating == null) floating = gameObject.AddComponent<EasternFantasy.Inventory.DroppedItemFloat>();
            floating.Begin(transform.position);
            if (completionMarker != null) markerRest = completionMarker.localPosition;
        }
        private void Update()
        {
            if (completionMarker != null)
                completionMarker.localPosition = markerRest + Vector3.up * (Mathf.Sin(Time.unscaledTime * 2.5f) * .06f);
        }
        public bool TryInteract() => Owner != null && Owner.TryInspectRelic();
        public static QuestRewardRelic FindNear(Transform player, float radius)
        {
            var relic = ToadHuntQuestController.Instance != null ? ToadHuntQuestController.Instance.Relic : null;
            // The persistent player lives in DontDestroyOnLoad, while the relic belongs to the field.
            return relic != null && relic.isActiveAndEnabled
                && relic.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                && (relic.transform.position - player.position).sqrMagnitude <= radius * radius ? relic : null;
        }
    }
}
