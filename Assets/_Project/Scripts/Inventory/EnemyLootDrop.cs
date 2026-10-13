using System;
using UnityEngine;

namespace EasternFantasy.Inventory
{
    [Serializable]
    public sealed class EnemyDropEntry
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Range(0f, 1f)] private float dropChance = 1f;
        [SerializeField, Min(1)] private int minimumQuantity = 1;
        [SerializeField, Min(1)] private int maximumQuantity = 1;

        public ItemDefinition Item => item;
        public float DropChance => dropChance;
        public int MinimumQuantity => minimumQuantity;
        public int MaximumQuantity => maximumQuantity;
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyLootDrop : MonoBehaviour
    {
        [SerializeField] private WorldItemPickup pickupPrefab;
        [SerializeField] private Collider2D floorCollider;
        [SerializeField] private EnemyDropEntry[] drops = Array.Empty<EnemyDropEntry>();
        [SerializeField, Min(0f)] private float spawnHeight = 0.35f;
        [SerializeField, Min(0f)] private float horizontalScatter = 0.65f;

        private EnemyController enemy;
        private bool hasDropped;

        private void Awake()
        {
            enemy = GetComponent<EnemyController>();
        }

        private void OnEnable()
        {
            if (enemy == null)
                enemy = GetComponent<EnemyController>();
            enemy.Defeated += HandleDefeated;
        }

        private void OnDisable()
        {
            if (enemy != null)
                enemy.Defeated -= HandleDefeated;
        }

        private void HandleDefeated(EnemyController defeatedEnemy)
        {
            if (hasDropped || pickupPrefab == null)
                return;

            hasDropped = true;
            Collider2D floor = DropPlacement2D.FindSupport(gameObject.scene, transform.position);
            if (floor == null) floor = floorCollider;
            foreach (EnemyDropEntry drop in drops)
            {
                if (drop == null || drop.Item == null ||
                    UnityEngine.Random.value > drop.DropChance)
                    continue;

                int minimum = Mathf.Max(1, drop.MinimumQuantity);
                int maximum = Mathf.Max(minimum, drop.MaximumQuantity);
                int quantity = UnityEngine.Random.Range(minimum, maximum + 1);
                float scatter = UnityEngine.Random.Range(-horizontalScatter, horizontalScatter);
                if (!DropPlacement2D.TryFindLanding(floor, transform.position,
                    transform.position.x + scatter, pickupPrefab.VisualWidth * 0.5f,
                    out Vector3 landing))
                {
                    if (drop.DropChance < 1f)
                    {
                        Debug.LogWarning($"No free Floor landing spot for {drop.Item.DisplayName}.", this);
                        continue;
                    }
                    // Guaranteed loot must still spawn if the landing area is full.
                    // Its physical drop body will settle on the supporting surface.
                    landing = transform.position;
                }

                WorldItemPickup pickup = Instantiate(
                    pickupPrefab,
                    transform.position + Vector3.up * spawnHeight,
                    Quaternion.identity);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(pickup.gameObject, gameObject.scene);
                pickup.Initialize(drop.Item, quantity, landing);
            }
        }

        private void OnValidate()
        {
            spawnHeight = Mathf.Max(0f, spawnHeight);
            horizontalScatter = Mathf.Max(0f, horizontalScatter);
        }
    }
}
