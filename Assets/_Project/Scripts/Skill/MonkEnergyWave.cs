using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

namespace EasternFantasy.Skill
{
    public sealed class MonkEnergyWave : MonoBehaviour
    {
        private static Sprite waveSprite;
        private readonly Collider2D[] hitBuffer = new Collider2D[16];
        private readonly HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>();
        private PlayerEntity owner;
        private MonkEnergy energy;
        private SpriteRenderer visual;
        private LayerMask enemyLayers;
        private float direction;
        private float speed;
        private float remainingRange;
        private float damage;
        private bool critical;

        public static void Launch(PlayerEntity owner, MonkEnergy energy, float direction,
            float range, float speed, float damageMultiplier, LayerMask enemyLayers)
        {
            GameObject obj = new GameObject("Monk Energy Wave");
            obj.transform.position = owner.transform.position
                + new Vector3(direction * 0.8f, 0.2f, 0f);
            obj.transform.localScale = new Vector3(1.8f, 1.15f, 1f);
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = GetWaveSprite();
            renderer.color = new Color(0.4f, 1f, 0.85f, 0.95f);
            renderer.sortingOrder = 40;
            MonkEnergyWave wave = obj.AddComponent<MonkEnergyWave>();
            wave.owner = owner;
            wave.energy = energy;
            wave.visual = renderer;
            wave.direction = direction;
            wave.remainingRange = Mathf.Max(0.1f, range);
            wave.speed = Mathf.Max(0.1f, speed);
            wave.enemyLayers = enemyLayers;
            wave.damage = owner.RollAttackDamage(out wave.critical) * damageMultiplier;
        }

        private void Update()
        {
            if (owner == null || owner.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            float step = Mathf.Min(speed * Time.deltaTime, remainingRange);
            transform.position += Vector3.right * (direction * step);
            remainingRange -= step;
            ApplyHits();

            if (visual != null)
            {
                Color tint = visual.color;
                tint.a = Mathf.Clamp01(remainingRange / 1.5f);
                visual.color = tint;
            }
            if (remainingRange <= 0f)
                Destroy(gameObject);
        }

        private void ApplyHits()
        {
            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = enemyLayers,
                useTriggers = true
            };
            int count = Physics2D.OverlapBox(transform.position,
                new Vector2(1.5f, 1.1f), 0f, filter, hitBuffer);
            for (int i = 0; i < count; i++)
            {
                EnemyController enemy = hitBuffer[i] != null
                    ? hitBuffer[i].GetComponentInParent<EnemyController>() : null;
                if (enemy == null || !hitEnemies.Add(enemy)) continue;
                float before = enemy.HP;
                enemy.TakeDamage(damage, critical);
                float dealt = Mathf.Max(0f, before - enemy.HP);
                if (dealt <= 0f) continue;
                owner.ApplyLifeSteal(dealt);
                energy.RegisterCombat();
                if (enemy.HP > 0f) enemy.ApplyKnockback(direction);
            }
        }

        private static Sprite GetWaveSprite()
        {
            if (waveSprite != null) return waveSprite;
            const int width = 64;
            const int height = 32;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float nx = (x + 0.5f - width * 0.5f) / (width * 0.5f);
                float ny = (y + 0.5f - height * 0.5f) / (height * 0.5f);
                float radius = nx * nx + ny * ny;
                float alpha = Mathf.Clamp01((1f - radius) * 2.5f);
                texture.SetPixel(x, y, new Color(0.6f + 0.4f * (1f - radius),
                    1f, 0.9f, alpha));
            }
            texture.Apply();
            waveSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f), 32f);
            return waveSprite;
        }
    }
}
