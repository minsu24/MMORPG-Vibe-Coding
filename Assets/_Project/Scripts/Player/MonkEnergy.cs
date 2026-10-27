using System;
using EasternFantasy.Skill;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerEntity))]
    public sealed partial class MonkEnergy : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float baseMaximum = 100f;
        [SerializeField, Min(0f)] private float energyOnHit = 20f;
        [SerializeField, Min(0f)] private float combatTimeout = 10f;
        [SerializeField, Min(0f)] private float decayPerSecond = 10f;
        [SerializeField] private string villageSceneName = "PrologueVilage";

        private PlayerEntity entity;
        private PlayerSkillSystem skills;
        private float energy;
        private float parryEndTime;

        public float CombatTimeout => combatTimeout;
        public float Current => energy;
        public float Maximum => baseMaximum + (skills != null
            ? skills.GetEffectTotal(SkillEffectType.MaximumMana) : 0f);
        public bool IsFull => energy >= Maximum - 0.001f;
        public bool IsParrying => Time.time < parryEndTime;
        public event Action Changed;

        private void Awake()
        {
            entity = GetComponent<PlayerEntity>();
            skills = GetComponent<PlayerSkillSystem>();
        }

        private void OnEnable()
        {
            if (entity == null) entity = GetComponent<PlayerEntity>();
            if (skills == null) skills = GetComponent<PlayerSkillSystem>();
            entity.Damaged += OnDamaged;
            if (skills != null) skills.SkillsChanged += OnSkillsChanged;
        }

        private void OnDisable()
        {
            if (entity != null) entity.Damaged -= OnDamaged;
            if (skills != null) skills.SkillsChanged -= OnSkillsChanged;
            parryEndTime = 0f;
        }

        private void Update()
        {
            if (entity.IsDead) return;
            bool inVillage = SceneManager.GetActiveScene().name == villageSceneName;
            if (!inVillage && entity.IsInCombat) return;
            SetEnergy(energy - decayPerSecond * Time.deltaTime);
        }

        public void RegisterCombat() { if (entity != null) entity.RegisterCombat(); }

        public void GainFromAttackHit(float dealtDamage)
        {
            if (dealtDamage > 0f) Gain(energyOnHit);
        }

        public void Gain(float amount)
        {
            if (amount <= 0f || entity.IsDead) return;
            RegisterCombat();
            SetEnergy(energy + amount);
        }

        public bool TrySpend(float amount)
        {
            if (entity.IsDead || amount < 0f || energy < amount) return false;
            SetEnergy(energy - amount);
            RegisterCombat();
            return true;
        }
        public bool TrySpendFullCharge()
        {
            if (entity.IsDead || !IsFull) return false;
            SetEnergy(0f);
            RegisterCombat();
            return true;
        }

        public void BeginParry(float duration)
        {
            RegisterCombat();
            parryEndTime = Time.time + Mathf.Max(0f, duration);
        }

        public bool TryParry()
        {
            if (!IsParrying || entity.IsDead) return false;
            parryEndTime = 0f;
            RegisterCombat();
            return true;
        }

        public void ResetEnergy()
        {
            parryEndTime = 0f;
            SetEnergy(0f);
        }

        private void OnDamaged(float damage) { if (damage > 0f) RegisterCombat(); }
        private void OnSkillsChanged() { SetEnergy(Mathf.Min(energy, Maximum)); Changed?.Invoke(); }

        private void SetEnergy(float value)
        {
            float next = Mathf.Clamp(value, 0f, Maximum);
            if (Mathf.Approximately(next, energy)) return;
            energy = next;
            Changed?.Invoke();
        }
    }
}
