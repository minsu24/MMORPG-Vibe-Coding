using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

namespace EasternFantasy.Skill
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MonkEnergyWave : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(1f)] private float framesPerSecond = 20f;
        [SerializeField] private Vector2 spawnOffset = new Vector2(0.9f, 0.35f);
        [SerializeField, Min(0.01f)] private float beamHeight = 1.1f;
        [SerializeField, Min(0.01f)] private float duration = 0.45f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.15f;

        private readonly Collider2D[] hitBuffer = new Collider2D[64];
        private readonly HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>();
        private PlayerEntity owner;
        private MonkEnergy energy;
        private SpriteRenderer visual;
        private LayerMask enemyLayers;
        private Vector2 hitCenter;
        private float direction;
        private float range;
        private float damage;
        private float elapsed;
        private bool critical;
        private bool launched;

        public static MonkEnergyWave Launch(MonkEnergyWave prefab, PlayerEntity owner,
            MonkEnergy energy, float direction, float range,
            float damageMultiplier, LayerMask enemyLayers)
        {
            if (prefab == null || owner == null) return null;
            float facing = direction < 0f ? -1f : 1f;
            float length = Mathf.Max(0.1f, range);
            Vector3 origin = owner.transform.position
                + new Vector3(prefab.spawnOffset.x * facing, prefab.spawnOffset.y, 0f);
            MonkEnergyWave wave = Instantiate(prefab,
                origin + Vector3.right * (facing * length * 0.5f), Quaternion.identity);
            wave.owner = owner;
            wave.energy = energy;
            wave.visual = wave.GetComponent<SpriteRenderer>();
            wave.direction = facing;
            wave.range = length;
            wave.hitCenter = wave.transform.position;
            wave.enemyLayers = enemyLayers;
            wave.damage = owner.RollAttackDamage(out wave.critical) * damageMultiplier;
            wave.visual.flipX = facing < 0f;
            wave.launched = true;
            wave.UpdateVisual();
            // The entire line hits immediately on the release frame.
            wave.ApplyHits();
            return wave;
        }

        private void Update()
        {
            if (!launched) return;
            if (owner == null || owner.IsDead)
            {
                Destroy(gameObject);
                return;
            }
            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                Destroy(gameObject);
                return;
            }
            UpdateVisual();
            ApplyHits();
        }

        private void UpdateVisual()
        {
            if (frames != null && frames.Length > 0)
            {
                int frame = elapsed >= duration - fadeDuration ? frames.Length - 1
                    : Mathf.Min(Mathf.FloorToInt(elapsed * framesPerSecond),
                        Mathf.Max(0, frames.Length - 2));
                visual.sprite = frames[frame];
            }
            if (visual.sprite != null)
            {
                Vector3 size = visual.sprite.bounds.size;
                // Each source frame has a different width; keep the full beam length constant.
                transform.localScale = new Vector3(range / Mathf.Max(0.001f, size.x),
                    beamHeight / Mathf.Max(0.001f, size.y), 1f);
            }
            Color tint = visual.color;
            tint.a = Mathf.Clamp01((duration - elapsed) / fadeDuration);
            visual.color = tint;
        }

        private void ApplyHits()
        {
            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = enemyLayers,
                useTriggers = true
            };
            int count = Physics2D.OverlapBox(hitCenter, new Vector2(range, beamHeight),
                0f, filter, hitBuffer);
            for (int i = 0; i < count; i++)
            {
                EnemyController enemy = hitBuffer[i] != null
                    ? hitBuffer[i].GetComponentInParent<EnemyController>() : null;
                if (enemy == null || enemy.HP <= 0f || !hitEnemies.Add(enemy)) continue;
                float before = enemy.HP;
                enemy.TakeDamage(damage, critical);
                float dealt = Mathf.Max(0f, before - enemy.HP);
                if (dealt <= 0f) continue;
                owner.ApplyLifeSteal(dealt);
                if (energy != null) energy.RegisterCombat();
                if (enemy.HP > 0f) enemy.ApplyKnockback(direction);
            }
        }
    }
}