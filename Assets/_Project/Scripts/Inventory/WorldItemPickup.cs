using DG.Tweening;
using EasternFantasy.Player;
using UnityEngine;

namespace EasternFantasy.Inventory
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public sealed class WorldItemPickup : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField, Min(0.1f)] private float worldIconSize = 0.95f;
        [SerializeField, Min(1f)] private float slenderWeaponScale = 1.3f;
        [SerializeField, Min(0.05f)] private float pickupRadius = 0.45f;

        [Header("Drop Motion")]
        [SerializeField, Min(0.05f)] private float tossDuration = 0.4f;
        [SerializeField, Min(0f)] private float tossHeight = 0.55f;
        [SerializeField, Min(0f)] private float pickupDelay = 0.2f;
        [SerializeField, Min(0f)] private float lifetime = 120f;

        [Header("Collection Feedback")]
        [SerializeField, Min(0.05f)] private float collectPopDuration = 0.18f;
        [SerializeField, Min(0f)] private float collectPopHeight = 0.35f;
        [SerializeField, Min(0.05f)] private float collectFlyDuration = 0.3f;
        [SerializeField, Range(0f, 1f)] private float collectedScale = 0.1f;
        [SerializeField] private Vector2 playerTargetOffset = new Vector2(0f, 0.45f);
        [SerializeField] private AudioClip collectSound;
        [SerializeField, Range(0f, 1f)] private float collectSoundVolume = 0.8f;

        private SpriteRenderer iconRenderer;
        private CircleCollider2D pickupCollider;
        private ItemDefinition item;
        private int quantity;
        private Rigidbody2D dropBody;
        private Vector3 landingPosition;
        private float elapsed;
        private bool initialized;
        private bool collected;
        private Sequence collectSequence;
        private DroppedItemFloat floating;

        public ItemDefinition Item => item;
        public int Quantity => quantity;
        public bool IsCollected => collected;
        public Vector3 LandingPosition => landingPosition;
        // Before initialization, reserve enough room for the largest possible weapon.
        public float VisualWidth => item == null ? worldIconSize * slenderWeaponScale : DisplaySize;
        private float DisplaySize => worldIconSize * (item != null && item.Category == ItemCategory.Equipment
            && item.EquipmentSlot == EquipmentSlot.Weapon && item.Icon != null
            && item.Icon.bounds.size.y >= item.Icon.bounds.size.x * 2f ? slenderWeaponScale : 1f);

        private void Awake()
        {
            iconRenderer = GetComponent<SpriteRenderer>();
            floating = GetComponent<DroppedItemFloat>();
            if (floating == null) floating = gameObject.AddComponent<DroppedItemFloat>();
            pickupCollider = GetComponent<CircleCollider2D>();
            pickupCollider.isTrigger = true;
            pickupCollider.enabled = false;
            pickupRadius = Mathf.Max(0.05f, pickupRadius);
            iconRenderer.sortingOrder = Mathf.Max(iconRenderer.sortingOrder, 8);
        }

        public void Initialize(ItemDefinition definition, int amount, Vector3 landingPoint)
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
            if (definition.Icon != null) {
                var properties = new MaterialPropertyBlock();
                iconRenderer.GetPropertyBlock(properties);
                properties.SetTexture("_MainTex", definition.Icon.texture);
                iconRenderer.SetPropertyBlock(properties);
            }
            FitIconToWorldSize();


            landingPosition = landingPoint;
            landingPosition.y += iconRenderer.bounds.extents.y - (iconRenderer.bounds.center.y - transform.position.y);
            elapsed = 0f;
            initialized = true;
            dropBody = DropPlacement2D.LaunchPhysicalDrop(gameObject, iconRenderer, landingPosition, tossDuration, tossHeight);

            if (lifetime > 0f)
                Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (!initialized || collected)
                return;

            elapsed += Time.deltaTime;
            if (!pickupCollider.enabled && elapsed >= pickupDelay)
                pickupCollider.enabled = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Collect(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            Collect(other);
        }

        private void Collect(Collider2D other)
        {
            if (!initialized || collected || !other.CompareTag("Player"))
                return;

            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null || !inventory.AddItem(item, quantity))
                return;

            collected = true;
            floating.Stop();
            DropPlacement2D.StopPhysicalDrop(dropBody, gameObject);
            pickupCollider.enabled = false;
            if (collectSound != null)
                AudioSource.PlayClipAtPoint(collectSound, transform.position, collectSoundVolume);
            PlayCollectFeedback(inventory.transform);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collected && DropPlacement2D.StopOnLanding(dropBody, collision))
            {
                landingPosition = transform.position;
                DropPlacement2D.SettlePhysicalDrop(dropBody, gameObject);
                floating.Begin(landingPosition);
            }
        }
        private void PlayCollectFeedback(Transform player)
        {
            collectSequence?.Kill();
            Vector3 originalScale = transform.localScale;
            Vector3 popStart = transform.position;
            Vector3 lastTarget = player != null
                ? player.position + (Vector3)playerTargetOffset : popStart;
            float popHalfDuration = Mathf.Max(0.025f, collectPopDuration * 0.5f);
            float flyDuration = Mathf.Max(0.05f, collectFlyDuration);
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();

            collectSequence = DOTween.Sequence();
            collectSequence.Append(transform.DOMoveY(popStart.y + collectPopHeight, popHalfDuration)
                .SetEase(Ease.OutQuad));
            collectSequence.Join(transform.DOScale(originalScale * 1.2f, popHalfDuration)
                .SetEase(Ease.OutBack));
            collectSequence.Append(transform.DOMoveY(popStart.y, popHalfDuration)
                .SetEase(Ease.InQuad));
            collectSequence.Join(transform.DOScale(originalScale, popHalfDuration)
                .SetEase(Ease.OutQuad));

            // Follow the player's current position throughout the flight, even while moving.
            collectSequence.Append(DOTween.To(() => 0f, progress =>
            {
                if (player != null)
                    lastTarget = player.position + (Vector3)playerTargetOffset;
                transform.position = Vector3.LerpUnclamped(popStart, lastTarget, progress);
            }, 1f, flyDuration).SetEase(Ease.InCubic));
            collectSequence.Join(transform.DOScale(originalScale * collectedScale, flyDuration)
                .SetEase(Ease.InCubic));
            collectSequence.Join(renderer.DOFade(0f, flyDuration).SetEase(Ease.InQuad));
            collectSequence.OnComplete(() =>
            {
                collectSequence = null;
                Destroy(gameObject);
            });
        }

        private void OnEnable() => collectSequence?.Play();
        private void OnDisable() => collectSequence?.Pause();
        private void OnDestroy() => collectSequence?.Kill();

        private void FitIconToWorldSize()
        {
            if (iconRenderer.sprite == null)
                return;

            Vector2 spriteSize = iconRenderer.sprite.bounds.size;
            float largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
            if (largestSide <= 0f)
                return;

            float scale = DisplaySize / largestSide;
            transform.localScale = Vector3.one * scale;
            pickupCollider.radius = pickupRadius / scale;
        }

        private void OnValidate()
        {
            worldIconSize = Mathf.Max(0.1f, worldIconSize);
            slenderWeaponScale = Mathf.Max(1f, slenderWeaponScale);
            pickupRadius = Mathf.Max(0.05f, pickupRadius);
            tossDuration = Mathf.Max(0.05f, tossDuration);
            tossHeight = Mathf.Max(0f, tossHeight);
            pickupDelay = Mathf.Max(0f, pickupDelay);
            lifetime = Mathf.Max(0f, lifetime);
            collectPopDuration = Mathf.Max(0.05f, collectPopDuration);
            collectPopHeight = Mathf.Max(0f, collectPopHeight);
            collectFlyDuration = Mathf.Max(0.05f, collectFlyDuration);
        }
    }
}
