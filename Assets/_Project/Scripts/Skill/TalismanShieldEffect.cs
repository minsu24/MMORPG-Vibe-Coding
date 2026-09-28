using System.Collections;
using EasternFantasy.Player;
using UnityEngine;

namespace EasternFantasy.Skill
{
    [DisallowMultipleComponent]
    public sealed class TalismanShieldEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer playerRenderer;
        [SerializeField] private SpriteRenderer[] talismans;
        [SerializeField] private SpriteRenderer[] hitEffects;

        [Header("Orbit")]
        [SerializeField, Min(0f)] private float radiusX = 1f;
        [SerializeField, Min(0f)] private float radiusY = 0.45f;
        [SerializeField] private Vector2 centerOffset = new Vector2(0f, 0.25f);
        [SerializeField] private float degreesPerSecond = 110f;
        [SerializeField, Min(0f)] private float talismanScale = 0.28f;

        [Header("Hit Reaction")]
        [SerializeField, Min(0.01f)] private float pulseDuration = 0.35f;
        [SerializeField, Min(0f)] private float pulseExpansion = 0.2f;
        [SerializeField, Min(0f)] private float hitEffectScale = 0.6f;

        private const int RingSegments = 48;
        private PlayerEntity player;
        private LineRenderer barrierRing;
        private Material ringMaterial;
        private Coroutine lifetimeRoutine;
        private float orbitAngle;
        private float hitPulse;

        private void Awake()
        {
            player = GetComponentInParent<PlayerEntity>();
            if (playerRenderer == null && player != null)
                playerRenderer = player.GetComponentInChildren<SpriteRenderer>();
            CreateBarrierRing();
        }

        private void OnEnable()
        {
            if (player != null)
            {
                player.DamageReduced += OnDamageReduced;
                player.Died += Hide;
            }
        }

        private void OnDisable()
        {
            if (player != null)
            {
                player.DamageReduced -= OnDamageReduced;
                player.Died -= Hide;
            }
            lifetimeRoutine = null;
            hitPulse = 0f;
            if (hitEffects != null)
            {
                foreach (SpriteRenderer effect in hitEffects)
                {
                    if (effect != null)
                        effect.enabled = false;
                }
            }
        }

        private void OnDestroy()
        {
            if (ringMaterial != null)
                Destroy(ringMaterial);
        }

        public void Show(float duration)
        {
            if (talismans == null || talismans.Length == 0)
                return;

            gameObject.SetActive(true);
            if (lifetimeRoutine != null)
                StopCoroutine(lifetimeRoutine);
            hitPulse = 0f;
            UpdateVisuals();
            lifetimeRoutine = StartCoroutine(HideAfter(duration));
        }

        private IEnumerator HideAfter(float duration)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, duration));
            Hide();
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnDamageReduced(float amount)
        {
            if (amount > 0f)
                hitPulse = 1f;
        }

        private void LateUpdate()
        {
            orbitAngle = Mathf.Repeat(orbitAngle + degreesPerSecond * Time.deltaTime, 360f);
            hitPulse = Mathf.MoveTowards(hitPulse, 0f, Time.deltaTime / pulseDuration);
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (talismans == null || talismans.Length == 0)
                return;

            int playerOrder = playerRenderer != null ? playerRenderer.sortingOrder : 0;
            int sortingLayerId = playerRenderer != null ? playerRenderer.sortingLayerID : 0;
            float expansion = hitPulse * pulseExpansion;

            for (int index = 0; index < talismans.Length; index++)
            {
                SpriteRenderer talisman = talismans[index];
                if (talisman == null || talisman.sprite == null)
                    continue;

                float angle = (orbitAngle + index * 360f / talismans.Length) * Mathf.Deg2Rad;
                float depth = Mathf.Sin(angle);
                bool behindPlayer = depth > 0f;
                float size = talismanScale * (behindPlayer ? 0.8f : 1f)
                    * (1f + hitPulse * 0.12f);
                Vector3 orbit = new Vector3(
                    Mathf.Cos(angle) * (radiusX + expansion) + centerOffset.x,
                    depth * (radiusY + expansion * 0.5f) + centerOffset.y,
                    0f);

                // The imported sprites use an offset pivot; keep the visible art centered on the orbit.
                talisman.transform.localScale = Vector3.one * size;
                talisman.transform.localPosition = orbit - talisman.sprite.bounds.center * size;
                talisman.transform.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Sin(Time.time * 4f + index) * 7f);
                talisman.sortingLayerID = sortingLayerId;
                talisman.sortingOrder = playerOrder + (behindPlayer ? -1 : 1);
                float alpha = behindPlayer ? 0.65f : 0.95f;
                alpha += Mathf.Sin(Time.time * 5f + index) * 0.04f;
                talisman.color = Color.Lerp(
                    new Color(1f, behindPlayer ? 0.72f : 0.88f, 0.55f, alpha),
                    Color.white,
                    hitPulse * 0.8f);
            }

            UpdateHitEffects(playerOrder, sortingLayerId);

            if (barrierRing != null)
            {
                barrierRing.sortingLayerID = sortingLayerId;
                barrierRing.sortingOrder = playerOrder - 2;
                for (int index = 0; index < RingSegments; index++)
                {
                    float angle = index * Mathf.PI * 2f / RingSegments;
                    barrierRing.SetPosition(index, new Vector3(
                        Mathf.Cos(angle) * radiusX * 0.83f + centerOffset.x,
                        Mathf.Sin(angle) * radiusY * 1.05f + centerOffset.y,
                        0f));
                }
                Color ringColor = new Color(1f, 0.75f, 0.28f, 0.12f);
                barrierRing.startColor = ringColor;
                barrierRing.endColor = ringColor;
            }
        }

        private void UpdateHitEffects(int playerOrder, int sortingLayerId)
        {
            if (hitEffects == null)
                return;

            float progress = 1f - hitPulse;
            for (int index = 0; index < hitEffects.Length; index++)
            {
                SpriteRenderer effect = hitEffects[index];
                if (effect == null || effect.sprite == null)
                    continue;

                float start = index == 0 ? 0f : 0.33f;
                float end = index == 0 ? 0.64f : 1f;
                bool visible = hitPulse > 0f && progress >= start && progress < end;
                effect.enabled = visible;
                if (!visible)
                    continue;

                float phase = Mathf.InverseLerp(start, end, progress);
                float scale = hitEffectScale * (index == 0 ? 0.85f : 1f)
                    * Mathf.Lerp(0.8f, 1.15f, phase);
                effect.transform.localScale = Vector3.one * scale;
                effect.transform.localPosition = (Vector3)centerOffset
                    - effect.sprite.bounds.center * scale;
                effect.sortingLayerID = sortingLayerId;
                effect.sortingOrder = playerOrder + 2 + index;
                effect.color = new Color(1f, 1f, 1f, Mathf.Sin(phase * Mathf.PI));
            }
        }

        private void CreateBarrierRing()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
                return;

            GameObject ring = new GameObject("BarrierEffect", typeof(LineRenderer));
            ring.transform.SetParent(transform, false);
            barrierRing = ring.GetComponent<LineRenderer>();
            ringMaterial = new Material(shader);
            barrierRing.sharedMaterial = ringMaterial;
            barrierRing.useWorldSpace = false;
            barrierRing.loop = true;
            barrierRing.positionCount = RingSegments;
            barrierRing.widthMultiplier = 0.018f;
            barrierRing.numCornerVertices = 2;
        }
    }
}
