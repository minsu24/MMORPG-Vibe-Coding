using UnityEngine;

namespace EasternFantasy.Advancement
{
    [DisallowMultipleComponent]
    public sealed class AdvancementGiver : MonoBehaviour
    {
        [SerializeField] private AdvancementDefinition advancement;

        // Call from an NPC interaction event for an NPC-based advancement.
        public void Interact()
        {
            PlayerAdvancementSystem system =
                FindFirstObjectByType<PlayerAdvancementSystem>();

            if (system == null)
            {
                Debug.LogWarning("AdvancementGiver could not find the player advancement system.", this);
                return;
            }

            system.TryStartAdvancement(advancement);
        }
    }
}
