using System.Collections;
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
        private Vector3 startPosition;
        private Vector3 landingPosition;
        private float elapsed;
        private int amount;
        private bool collected;

        public int Amount => amount;
        public Vector3 LandingPosition => landingPosition;
        public float VisualWidth => visualWidth;

        private void Awake()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
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
            startPosition = transform.position;
            landingPosition = landingPoint;
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer.sprite != null)
                landingPosition.y += renderer.bounds.extents.y;
            if (lifetime > 0f)
                Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (amount <= 0 || collected)
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
                transform.position = landingPosition;
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
            currency.Add(amount);
            pickupCollider.enabled = false;
            if (collectSound != null)
                AudioSource.PlayClipAtPoint(collectSound, transform.position, collectSoundVolume);
            StartCoroutine(CollectFeedbackRoutine(currency.transform));
        }

        private IEnumerator CollectFeedbackRoutine(Transform player)
        {
            Vector3 originalScale = transform.localScale;
            Vector3 popStart = transform.position;
            float progress = 0f;

            while (progress < 1f)
            {
                progress = Mathf.Min(1f, progress + Time.deltaTime / collectPopDuration);
                float arc = Mathf.Sin(progress * Mathf.PI);
                transform.position = popStart + Vector3.up * (arc * collectPopHeight);
                transform.localScale = originalScale * (1f + arc * 0.2f);
                yield return null;
            }

            Vector3 flyStart = transform.position;
            Vector3 flyStartScale = transform.localScale;
            progress = 0f;
            while (progress < 1f)
            {
                progress = Mathf.Min(1f, progress + Time.deltaTime / collectFlyDuration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                Vector3 target = player != null
                    ? player.position + (Vector3)playerTargetOffset
                    : flyStart;
                transform.position = Vector3.LerpUnclamped(flyStart, target, eased);
                transform.localScale = Vector3.LerpUnclamped(
                    flyStartScale, originalScale * collectedScale, eased);
                yield return null;
            }

            Destroy(gameObject);
        }

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
