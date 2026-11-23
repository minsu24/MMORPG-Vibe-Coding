using DG.Tweening;
using UnityEngine;
using EasternFantasy.Player;

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
        private SpriteRenderer iconRenderer;
        private Rigidbody2D dropBody;
        private Vector3 gatherOrigin, landingPosition;
        private float elapsed;
        private int amount;
        private bool collected;
        private Sequence collectSequence;
        private DroppedItemFloat floating;

        public int Amount => amount;
        public bool IsCollected => collected;
        public Vector3 LandingPosition => landingPosition;
        internal float LandingCenterX => landingPosition.x + iconRenderer.bounds.center.x - transform.position.x;
        public float VisualWidth => visualWidth;

        private void Awake()
        {
            iconRenderer = GetComponent<SpriteRenderer>();
            floating = GetComponent<DroppedItemFloat>();
            if (floating == null) floating = gameObject.AddComponent<DroppedItemFloat>();
            if (iconRenderer.sprite == null)
                iconRenderer.sprite = GetFallbackSprite();
            iconRenderer.sortingOrder = Mathf.Max(iconRenderer.sortingOrder, 8);
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
            landingPosition.x -= iconRenderer.bounds.center.x - transform.position.x;
            landingPosition.y += transform.position.y - iconRenderer.bounds.min.y;
            elapsed = 0f;
            gatherOrigin = transform.position;
            DropPlacement2D.GatherAtDropOrigin(transform, gatherOrigin, landingPosition, 0f);
            if (lifetime > 0f)
                Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (amount <= 0 || collected)
                return;

            elapsed += Time.deltaTime;
            if (dropBody == null)
            {
                DropPlacement2D.GatherAtDropOrigin(transform, gatherOrigin, landingPosition, elapsed);
                if (elapsed < DropPlacement2D.GatherDuration) return;

                dropBody = DropPlacement2D.LaunchPhysicalDrop(gameObject, iconRenderer, landingPosition, tossDuration, tossHeight);
                elapsed = 0f;
            }
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
            PlayerItemAudio audio = currency.GetComponent<PlayerItemAudio>();
            if (audio != null)
                audio.PlayCurrencyPickup();
            else if (collectSound != null)
                AudioSource.PlayClipAtPoint(collectSound, transform.position, collectSoundVolume);
            PlayCollectFeedback(currency.transform);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collected && DropPlacement2D.TrySettleOnLanding(dropBody, iconRenderer, collision,
                VisualWidth * .5f, landingPosition, out Vector3 position))
            {
                landingPosition = position;
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
