using EasternFantasy.Player;
using UnityEngine;

namespace EasternFantasy.Inventory
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public sealed class WorldItemPickup : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField, Min(0.1f)] private float worldIconSize = 0.8f;
        [SerializeField, Min(0.05f)] private float pickupRadius = 0.45f;
        [SerializeField, Min(0f)] private float bobHeight = 0.08f;
        [SerializeField, Min(0f)] private float bobSpeed = 2.5f;

        [Header("Drop Motion")]
        [SerializeField, Min(0.05f)] private float tossDuration = 0.4f;
        [SerializeField, Min(0f)] private float tossHeight = 0.55f;
        [SerializeField, Min(0f)] private float pickupDelay = 0.2f;
        [SerializeField, Min(0f)] private float lifetime = 120f;

        private SpriteRenderer iconRenderer;
        private CircleCollider2D pickupCollider;
        private ItemDefinition item;
        private int quantity;
        private Vector3 startPosition;
        private Vector3 landingPosition;
        private float elapsed;
        private bool initialized;
        private bool collected;

        public ItemDefinition Item => item;
        public int Quantity => quantity;

        private void Awake()
        {
            iconRenderer = GetComponent<SpriteRenderer>();
            pickupCollider = GetComponent<CircleCollider2D>();
            pickupCollider.isTrigger = true;
            pickupCollider.enabled = false;
            pickupRadius = Mathf.Max(0.05f, pickupRadius);
            iconRenderer.sortingOrder = Mathf.Max(iconRenderer.sortingOrder, 8);
        }

        public void Initialize(ItemDefinition definition, int amount, float horizontalScatter)
        {
            if (definition == null || amount <= 0)
            {
                Destroy(gameObject);
                return;
            }

            item = definition;
            quantity = amount;
            name = $"{definition.DisplayName} x{amount}";
            iconRenderer.sprite = definition.Icon;
            FitIconToWorldSize();

            startPosition = transform.position;
            landingPosition = startPosition + Vector3.right * horizontalScatter;
            elapsed = 0f;
            initialized = true;

            if (lifetime > 0f)
                Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (!initialized || collected)
                return;

            elapsed += Time.deltaTime;
            if (elapsed < tossDuration)
            {
                float progress = Mathf.Clamp01(elapsed / tossDuration);
                Vector3 position = Vector3.Lerp(startPosition, landingPosition, progress);
                position.y += Mathf.Sin(progress * Mathf.PI) * tossHeight;
                transform.position = position;
            }
            else
            {
                float bob = Mathf.Sin((elapsed - tossDuration) * bobSpeed) * bobHeight;
                transform.position = landingPosition + Vector3.up * bob;
            }

            if (!pickupCollider.enabled && elapsed >= pickupDelay)
                pickupCollider.enabled = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!initialized || collected || !other.CompareTag("Player"))
                return;

            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null || !inventory.AddItem(item, quantity))
                return;

            collected = true;
            pickupCollider.enabled = false;
            Destroy(gameObject);
        }

        private void FitIconToWorldSize()
        {
            if (iconRenderer.sprite == null)
                return;

            Vector2 spriteSize = iconRenderer.sprite.bounds.size;
            float largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
            if (largestSide <= 0f)
                return;

            float scale = worldIconSize / largestSide;
            transform.localScale = Vector3.one * scale;
            pickupCollider.radius = pickupRadius / scale;
        }

        private void OnValidate()
        {
            worldIconSize = Mathf.Max(0.1f, worldIconSize);
            pickupRadius = Mathf.Max(0.05f, pickupRadius);
            tossDuration = Mathf.Max(0.05f, tossDuration);
            tossHeight = Mathf.Max(0f, tossHeight);
            pickupDelay = Mathf.Max(0f, pickupDelay);
            lifetime = Mathf.Max(0f, lifetime);
        }
    }
}
