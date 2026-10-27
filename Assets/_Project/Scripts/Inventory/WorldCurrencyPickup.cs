using DG.Tweening;
using UnityEngine;

namespace EasternFantasy.Inventory
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public sealed class WorldCurrencyPickup : MonoBehaviour
    {
        private static Sprite fallbackSprite;
        [SerializeField, Min(0.05f)] private float pickupRadius = 0.45f;
        [SerializeField, Min(0f)] private float pickupDelay = 0.2f;
        [SerializeField, Min(0.05f)] private float tossDuration = 0.4f;
        [SerializeField, Min(0f)] private float tossHeight = 0.55f;
        [SerializeField, Min(0f)] private float lifetime = 120f;
        [SerializeField, Min(0.1f)] private float visualWidth = 0.65f;

        [Header("Collection Feedback")]
        [SerializeField, Min(0.05f)] private float collectPopDuration = 0.18f;
        [SerializeField, Min(0f)] private float collectPopHeight = 0.35f;
        [SerializeField, Min(0.05f)] private float collectFlyDuration = 0.3f;
        [SerializeField, Range(0f, 1f)] private float collectedScale = 0.1f;
        [SerializeField] private Vector2 playerTargetOffset = new Vector2(0f, 0.45f);
        [SerializeField] private AudioClip collectSound;
        [SerializeField, Range(0f, 1f)] private float collectSoundVolume = 0.8f;

        private CircleCollider2D pickupCollider;
        private Rigidbody2D dropBody;
        private Vector3 landingPosition;
        private float elapsed;
        private int amount;
        private bool collected;
        private Sequence collectSequence;
        private DroppedItemFloat floating;

        public int Amount => amount;
        public bool IsCollected => collected;
        public Vector3 LandingPosition => landingPosition;
        public float VisualWidth => visualWidth;

        private void Awake()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            floating = GetComponent<DroppedItemFloat>();
            if (floating == null) floating = gameObject.AddComponent<DroppedItemFloat>();
            if (renderer.sprite == null)
                renderer.sprite = GetFallbackSprite();
            renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 8);
            pickupCollider = GetComponent<CircleCollider2D>();
            pickupCollider.isTrigger = true;
            pickupCollider.enabled = false;
            pickupCollider.radius = pickupRadius;
        }

        public void Initialize(int value, Vector3 landingPoint)
        {
            if (value <= 0)
            {
                Destroy(gameObject);
                return;
            }

            amount = value;
            name = $"Yeopjeon x{value}";

            landingPosition = landingPoint;
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer.sprite != null)
                landingPosition.y += renderer.bounds.extents.y;
            elapsed = 0f;
            dropBody = DropPlacement2D.LaunchPhysicalDrop(gameObject, renderer, landingPosition, tossDuration, tossHeight);
            if (lifetime > 0f)
                Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (amount <= 0 || collected)
                return;

            elapsed += Time.deltaTime;
            if (!pickupCollider.enabled && elapsed >= pickupDelay)
                pickupCollider.enabled = true;
        }

        private void OnTriggerEnter2D(Collider2D other) => Collect(other);
        private void OnTriggerStay2D(Collider2D other) => Collect(other);

        private void Collect(Collider2D other)
        {
            if (collected || amount <= 0 || !other.CompareTag("Player"))
                return;

            PlayerCurrency currency = other.GetComponentInParent<PlayerCurrency>();
            if (currency == null)
                return;

            collected = true;
            floating.Stop();
            DropPlacement2D.StopPhysicalDrop(dropBody, gameObject);
            currency.Add(amount);
            pickupCollider.enabled = false;
            if (collectSound != null)
                AudioSource.PlayClipAtPoint(collectSound, transform.position, collectSoundVolume);
            PlayCollectFeedback(currency.transform);
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

        private void OnValidate()
        {
            pickupRadius = Mathf.Max(0.05f, pickupRadius);
            tossDuration = Mathf.Max(0.05f, tossDuration);
            visualWidth = Mathf.Max(0.1f, visualWidth);
            collectPopDuration = Mathf.Max(0.05f, collectPopDuration);
            collectPopHeight = Mathf.Max(0f, collectPopHeight);
            collectFlyDuration = Mathf.Max(0.05f, collectFlyDuration);
        }

        private static Sprite GetFallbackSprite()
        {
            if (fallbackSprite != null)
                return fallbackSprite;

            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    Color color = distance > 14f ? Color.clear
                        : distance > 11f ? new Color(0.45f, 0.24f, 0.05f)
                        : distance < 4f ? new Color(0.42f, 0.22f, 0.04f)
                        : new Color(1f, 0.76f, 0.2f);
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            fallbackSprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 48f);
            fallbackSprite.name = "Yeopjeon Placeholder";
            return fallbackSprite;
        }
    }
}
