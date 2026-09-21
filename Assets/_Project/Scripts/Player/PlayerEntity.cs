using System;
using UnityEngine;
using System.Collections;
using EasternFantasy.Player;
using EasternFantasy.Skill;
using EasternFantasy.Inventory;
using EasternFantasy.Advancement;


namespace EasternFantasy.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement2D))]
    public sealed class PlayerEntity : Entity
    {
        [Header("Class Growth")]
        [SerializeField] private PlayerStatGrowthTable statGrowth;

        [Header("Maximum Stats")]
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(0f)] private float maximumMana = 100f;
        [SerializeField, Min(0f)] private float maximumMental = 100f;

        [Header("Base Stats")]
        [SerializeField, Min(0f)] private float baseAttackPower = 10f;
        [SerializeField, Min(0f)] private float baseDefensePower = 5f;
        [SerializeField, Range(0f, 1f)] private float lifeStealRate;
        [SerializeField, Range(0f, 1f)] private float criticalChance = 0.05f;
        [SerializeField, Min(1f)] private float criticalDamageMultiplier = 1.5f;
        [SerializeField, Min(0f)] private float baseSpeed = 6f;
        [SerializeField] private float invincibleDuration = 1.0f; // 무적 시간 (초)
        [SerializeField, Min(0f)] private float knockbackPower = 8f;
        [SerializeField, Min(0f)] private float horizontalKnockbackPower = 8f;
        [SerializeField, Min(0f)] private float verticalKnockbackPower = 4f;
        private SpriteRenderer[] spriteRenderers;

        private PlayerMovement2D playerController;
        private PlayerProgression playerProgression;
        private PlayerSkillSystem playerSkillSystem;
        private PlayerInventory playerInventory;
        private PlayerAdvancementSystem advancementSystem;
        private float healthLevelBonus;
        private float manaLevelBonus;
        private float attackLevelBonus;
        private float defenseLevelBonus;
        private float healthSkillBonus;
        private float manaSkillBonus;
        private float speedSkillBonus;
        private float activeDamageReduction;
        private Coroutine damageReductionRoutine;

        

        public bool isInvincible, isKnockback = false;
        public event Action<float> Damaged;
        public event Action Died;
        public event Action StatsChanged;
        private int playerLayer, EnemyLayer;

        public override float maxHP => maximumHealth + healthLevelBonus + healthSkillBonus;
        public override float maxMP => maximumMana + manaLevelBonus + manaSkillBonus;
        public override float maxMental => maximumMental;
        public float Defense => baseDefensePower + defenseLevelBonus +
            (playerInventory != null ? playerInventory.EquippedDefenseBonus : 0f) +
            (advancementSystem != null ? advancementSystem.DefenseBonus : 0f);
        public float LifeStealRate => lifeStealRate;
        public float CriticalChance => criticalChance;
        public float CriticalDamageMultiplier => criticalDamageMultiplier;
        public string ClassName => statGrowth != null ? statGrowth.ClassName : "DOSA";
        public string ResourceName => statGrowth != null ? statGrowth.ResourceName : "MP";
        public bool IsDead => HP <= 0f;
        public float ActiveDamageReduction => activeDamageReduction;

        private void Awake()
        {
            Setup();
            Attack_Power = baseAttackPower;
            Speed = baseSpeed;
            EnemyLayer = LayerMask.NameToLayer("Enemy");
            playerLayer = LayerMask.NameToLayer("Player");
            playerController =   GetComponent<PlayerMovement2D>();
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void Start()
        {
            playerProgression = GetComponent<PlayerProgression>();
            playerSkillSystem = GetComponent<PlayerSkillSystem>();
            playerInventory = GetComponent<PlayerInventory>();
            advancementSystem = GetComponent<PlayerAdvancementSystem>();
            if (playerProgression != null)
            {
                playerProgression.LevelChanged += ApplyLevelStats;
            }

            if (playerSkillSystem != null)
                playerSkillSystem.SkillsChanged += ApplySkillStats;
            if (playerInventory != null)
                playerInventory.InventoryChanged += RecalculateStats;
            if (advancementSystem != null)
                advancementSystem.Advanced += RecalculateStats;

            RecalculateStats();
        }

        private void ApplyLevelStats(int level)
        {
            RecalculateStats();
        }

        private void ApplySkillStats()
        {
            RecalculateStats();
        }

        private void RecalculateStats()
        {
            float previousMaxHealth = maxHP;
            float previousMaxMana = maxMP;
            int level = playerProgression != null ? playerProgression.CurrentLevel : 1;

            healthLevelBonus = statGrowth != null ? statGrowth.GetHealthBonus(level) : 0f;
            manaLevelBonus = statGrowth != null ? statGrowth.GetResourceBonus(level) : 0f;
            attackLevelBonus = statGrowth != null ? statGrowth.GetAttackBonus(level) : 0f;
            defenseLevelBonus = statGrowth != null ? statGrowth.GetDefenseBonus(level) : 0f;
            healthSkillBonus = playerSkillSystem != null
                ? playerSkillSystem.GetEffectTotal(SkillEffectType.MaximumHealth)
                : 0f;
            manaSkillBonus = playerSkillSystem != null
                ? playerSkillSystem.GetEffectTotal(SkillEffectType.MaximumMana)
                : 0f;
            speedSkillBonus = playerSkillSystem != null
                ? playerSkillSystem.GetEffectTotal(SkillEffectType.MoveSpeed)
                : 0f;

            float equipmentAttackBonus = playerInventory != null
                ? playerInventory.EquippedAttackBonus
                : 0f;
            float advancementAttackBonus = advancementSystem != null
                ? advancementSystem.AttackBonus
                : 0f;
            Attack_Power = baseAttackPower + attackLevelBonus + equipmentAttackBonus
                + advancementAttackBonus;
            Speed = baseSpeed + speedSkillBonus;

            // Keep the current resource and grant only the newly gained capacity.
            HP += Mathf.Max(0f, maxHP - previousMaxHealth);
            MP += Mathf.Max(0f, maxMP - previousMaxMana);
            StatsChanged?.Invoke();
        }

        public float RollAttackDamage(out bool isCritical)
        {
            isCritical = UnityEngine.Random.value < criticalChance;
            return Attack_Power * (isCritical ? criticalDamageMultiplier : 1f);
        }

        public void ApplyLifeSteal(float dealtDamage)
        {
            if (dealtDamage > 0f && lifeStealRate > 0f && !IsDead)
                HP += dealtDamage * lifeStealRate;
        }

        public bool RestoreHealth(float amount)
        {
            if (amount <= 0f || IsDead || HP >= maxHP)
                return false;

            HP += amount;
            StatsChanged?.Invoke();
            return true;
        }

        public bool RestoreMana(float amount)
        {
            if (amount <= 0f || MP >= maxMP)
                return false;

            MP += amount;
            StatsChanged?.Invoke();
            return true;
        }

        public override void TakeDamage(float damage)
        {
            if (IsDead || isInvincible || damage <= 0f)
                return;

            float mitigatedDamage = damage
                * (1f - Mathf.Clamp01(activeDamageReduction))
                * (100f / (100f + Mathf.Max(0f, Defense)));
            float appliedDamage = Mathf.Min(Mathf.Max(1f, mitigatedDamage), HP);
            HP -= appliedDamage;
            Damaged?.Invoke(appliedDamage);

            if (IsDead)
                Died?.Invoke();
        }

        public void ActivateDamageReduction(float reductionRate, float duration)
        {
            if (damageReductionRoutine != null)
                StopCoroutine(damageReductionRoutine);
            damageReductionRoutine = StartCoroutine(DamageReductionRoutine(reductionRate, duration));
        }

        private IEnumerator DamageReductionRoutine(float reductionRate, float duration)
        {
            activeDamageReduction = Mathf.Clamp01(reductionRate);
            StatsChanged?.Invoke();
            yield return new WaitForSeconds(Mathf.Max(0f, duration));
            activeDamageReduction = 0f;
            damageReductionRoutine = null;
            StatsChanged?.Invoke();
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.CompareTag("Monster"))
            {
                EnemyController enemyController = collision.collider.GetComponent<EnemyController>();
                if(enemyController != null)
                {
                    Debug.Log("적이랑 충돌");
                    TakeDamage(enemyController.Attack_Power);
                    float direction = transform.position.x > collision.transform.position.x ? 1f : -1f;
                    ApplyKnockback(direction);
                    Debug.Log(enemyController.Attack_Power);
                    //StartCoroutine(InvincibleCoroutine());    
                }
            }
        }

        public void ApplyKnockback(float direction)
        {
            StartCoroutine(KnockBackTRoutine()); // 이동 제어권 강탈
            playerController.body.linearVelocity = Vector2.zero;
            Vector2 knockbackForce = new Vector2(direction * horizontalKnockbackPower, verticalKnockbackPower);
            playerController.body.AddForce(knockbackForce, ForceMode2D.Impulse);
            StartCoroutine(InvincibleCoroutine());
        }
        private IEnumerator InvincibleCoroutine()
        {
            isInvincible = true;
            Physics2D.IgnoreLayerCollision(EnemyLayer, playerLayer, true);
            SetVisualAlpha(0.5f);
            yield return new WaitForSeconds(invincibleDuration);
            Physics2D.IgnoreLayerCollision(EnemyLayer, playerLayer, false);
            SetVisualAlpha(1.0f);
            isInvincible = false;
        }
        private void SetVisualAlpha(float alpha)
        {
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                Color color = renderer.color;
                color.a = alpha;
                renderer.color = color;
            }
        }

        private IEnumerator KnockBackTRoutine()
        {
            isKnockback = true;
            yield return new WaitForSeconds(0.5f);
            isKnockback = false;
        }

        private void OnDestroy()
        {
            if (playerProgression != null)
                playerProgression.LevelChanged -= ApplyLevelStats;
            if (playerSkillSystem != null)
                playerSkillSystem.SkillsChanged -= ApplySkillStats;
            if (playerInventory != null)
                playerInventory.InventoryChanged -= RecalculateStats;
            if (advancementSystem != null)
                advancementSystem.Advanced -= RecalculateStats;
        }
    }
}
