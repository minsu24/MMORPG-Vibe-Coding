using System;
using UnityEngine;

namespace EasternFantasy.Player
{
    [CreateAssetMenu(menuName = "Eastern Fantasy/Companions/Prefab Catalog")]
    public sealed class PetCompanionCatalog : ScriptableObject
    {
        [Tooltip("Register companion prefabs here; the companion window creates one row per prefab.")]
        [SerializeField] private PetCompanionActor[] companionPrefabs = Array.Empty<PetCompanionActor>();
        [SerializeField] private bool showPlaceholder = true;
        public PetCompanionActor[] CompanionPrefabs => companionPrefabs;
        public bool ShowPlaceholder => showPlaceholder;

        private void OnValidate()
        {
            var identifiers = new System.Collections.Generic.HashSet<string>();
            foreach (var prefab in companionPrefabs)
            {
                if (prefab == null) continue;
                if (string.IsNullOrWhiteSpace(prefab.CompanionId) || !identifiers.Add(prefab.CompanionId))
                    Debug.LogWarning("Companion prefabs need distinct, non-empty companion IDs.", this);
            }
        }
    }
}
