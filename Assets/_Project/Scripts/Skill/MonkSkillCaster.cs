using System.Collections;
using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

namespace EasternFantasy.Skill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MonkEnergy))]
    [RequireComponent(typeof(PlayerEntity))]
    public sealed class MonkSkillCaster : MonoBehaviour, IPlayerSkillCaster
    {
        [SerializeField] private SkillDefinition straightPunchSkill;
        [SerializeField] private SkillDefinition parrySkill;
        [SerializeField] private SkillDefinition dashSkill;
        [SerializeField] private SkillDefinition energyWaveSkill;
        [SerializeField, Min(0f)] private float punchEnergyOnHit = 20f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.18f;
        [SerializeField, Min(0f)] private float parryWindow = 0.35f;
        [SerializeField] private Vector2 punchOffset = new Vector2(1f, 0.2f);
        [SerializeField] private Vector2 punchSize = new Vector2(2.1f, 1.5f);
        [Tooltip("Seconds to keep checking for new targets after each punch frame.")]
        [SerializeField, Min(0f)] private float punchHitWindow = 0.12f;
        [SerializeField, Min(0.1f)] private float waveRange = 8f;
        [SerializeField] private bool invertFacingDirection = true;
        [SerializeField] private LayerMask enemyLayers;
        [SerializeField] private GameObject punchImpactPrefab;
        [SerializeField] private MonkEnergyWave wavePrefab;
        [Header("Geumgangbo Audio")]
        [SerializeField] private AudioClip dashSound;
        [SerializeField, Range(0f, 1f)] private float dashSoundVolume = 0.55f;
        [Header("Energy Wave Charging Audio")]
        [SerializeField] private AudioClip waveChargeSound;
        [SerializeField, Range(0f, 1f)] private float waveChargeVolume = 0.45f;
        [Header("Straight Punch Impact Audio")]
        [SerializeField] private AudioClip punchHitSound;
        [SerializeField, Range(0f, 1f)] private float punchHitSoundVolume = 0.65f;

        private readonly Collider2D[] hitBuffer = new Collider2D[32];
        private readonly HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>();
        private readonly Dictionary<SkillDefinition, float> cooldownEnds = new Dictionary<SkillDefinition, float>();
        private PlayerEntity entity;
        private MonkEnergy energy;
        private PlayerSkillSystem skillSystem;
        private PlayerIdleAnimation animationController;
        private AudioSource dashAudio;
        private AudioSource waveChargeAudio;
        private Animator characterAnimator;
        private Coroutine waveChargeFade;
        private bool waveChargePaused;

        private Coroutine punchRoutine;
        private Coroutine dashRoutine;
        private Coroutine waveRoutine;
        private MonkEnergyWave activeWave;
        private PlayerMovement2D movement;
        private bool wavePending;
        private float waveDirection;
        private float waveDamageMultiplier;
        private bool punchActive;
        private bool punchEnergyGranted;
        private int lastStrike;
        private int lastSwing;
        private float punchDirection;
        private float punchHitWindowEnd;
        private float punchDamage;
        private bool punchCritical;

        private struct PunchHit
        {
            public EnemyController Enemy;
            public GameObject TextPrefab;
            public Vector3 TextPosition;
            public Vector3 ImpactPosition;
            public Vector3 ImpactOffset;
        }

        private void Awake()
        {
            entity = GetComponent<PlayerEntity>();
            movement = GetComponent<PlayerMovement2D>();
            energy = GetComponent<MonkEnergy>();
            skillSystem = GetComponent<PlayerSkillSystem>();
            animationController = GetComponent<PlayerIdleAnimation>();
            characterAnimator = GetComponentInChildren<Animator>();
            if (enemyLayers.value == 0)
            {
                int layer = LayerMask.NameToLayer("Enemy");
                if (layer >= 0) enemyLayers = 1 << layer;
            }
        }

        public bool TryCast(SkillDefinition skill)
        {
            if (!isActiveAndEnabled || !animationController.CanPerformCombatAction
                || skill == null || entity.IsDead || entity.isKnockback || Time.timeScale <= 0f
                || skillSystem == null || skillSystem.GetSkillLevel(skill) <= 0
                || dashRoutine != null
                || GetCooldownRemaining(skill) > 0f || !SkillResourcePayment.CanAfford(entity, skill)) return false;

            if (skill == straightPunchSkill)
            {
                if (animationController == null || punchActive
                    || !animationController.TryRequestSkillAnimation("MonkStab", stopGroundedMovement: true))
                    return false;
                punchActive = true;
                punchEnergyGranted = false;
                lastStrike = 0;
                lastSwing = 0;
                punchHitWindowEnd = 0f;
                punchDirection = GetDirection();
                punchDamage = entity.RollAttackDamage(out punchCritical)
                    * skill.GetDamageMultiplier(skillSystem.GetSkillLevel(skill));
                hitEnemies.Clear();
                punchRoutine = StartCoroutine(WaitForPunch());
            }
            else if (skill == dashSkill)
            {
                if (movement == null || !movement.CanDash || animationController == null
                    || !animationController.TryRequestSkillAnimation("MonkDash", dashDuration))
                    return false;
                // Movement direction is independent of the punch sprite's inversion.
                if (!movement.BeginDash(animationController.FacingDirectionX,
                    skill.GetDashDistance(skillSystem.GetSkillLevel(skill)), dashDuration))
                {
                    animationController.FinishSkillAnimation();
                    return false;
                }
                dashRoutine = StartCoroutine(WaitForDash());
            }
            else if (skill == parrySkill)
            {
                energy.BeginParry(parryWindow);
            }
            else if (skill == energyWaveSkill)
            {
                if (wavePrefab == null || waveRoutine != null
                    || animationController == null
                    || !animationController.TryRequestSkillAnimation("MonkBlast"))
                    return false;

                if (movement != null) movement.SetMovementLocked(true);
                waveDirection = GetDirection();
                waveDamageMultiplier = skill.GetDamageMultiplier(skillSystem.GetSkillLevel(skill));
                wavePending = true;
                waveRoutine = StartCoroutine(WaitForWave());
            }
            else return false;

            if (!SkillResourcePayment.TrySpend(entity, skill)) return false;
            if (skill == dashSkill) PlayDashSound();
            if (skill == energyWaveSkill) PlayWaveChargeSound();
            float cooldown = skill.GetCooldownSeconds(skillSystem.GetSkillLevel(skill));
            if (cooldown > 0f)
                cooldownEnds[skill] = Time.time + cooldown;
            return true;
        }

        public float GetCooldownRemaining(SkillDefinition skill)
        {
            return skill != null && cooldownEnds.TryGetValue(skill, out float end)
                ? Mathf.Max(0f, end - Time.time) : 0f;
        }

        private void PlayDashSound()
        {
            if (dashSound == null) return;
            if (dashAudio == null)
            {
                var audioObject = new GameObject("Geumgangbo Audio");
                audioObject.transform.SetParent(transform, false);
                dashAudio = audioObject.AddComponent<AudioSource>();
                dashAudio.playOnAwake = false;
                dashAudio.loop = false;
                dashAudio.spatialBlend = 0f;
            }
            dashAudio.enabled = true;
            dashAudio.PlayOneShot(dashSound, dashSoundVolume);
        }

        private void PlayWaveChargeSound()
        {
            if (waveChargeSound == null) return;
            StopWaveChargeSound(true);
            if (waveChargeAudio == null)
            {
                var audioObject = new GameObject("Energy Wave Charge Audio");
                audioObject.transform.SetParent(transform, false);
                waveChargeAudio = audioObject.AddComponent<AudioSource>();
                waveChargeAudio.playOnAwake = false;
                waveChargeAudio.loop = false;
                waveChargeAudio.spatialBlend = 0f;
            }
            waveChargeAudio.enabled = true;
            waveChargeAudio.clip = waveChargeSound;
            waveChargeAudio.volume = waveChargeVolume;
            waveChargeAudio.pitch = Mathf.Clamp((characterAnimator != null ? characterAnimator.speed : 1f) * Time.timeScale, 0.1f, 3f);
            waveChargePaused = false;
            waveChargeAudio.Play();
        }

        private void Update()
        {
            if (!wavePending || waveChargeAudio == null) return;
            if (entity.IsDead || entity.isKnockback || !animationController.IsAttacking)
            {
                wavePending = false;
                StopWaveChargeSound(false);
                return;
            }
            if (Time.timeScale <= 0f)
            {
                if (!waveChargePaused) { waveChargeAudio.Pause(); waveChargePaused = true; }
            }
            else
            {
                waveChargeAudio.pitch = Mathf.Clamp((characterAnimator != null ? characterAnimator.speed : 1f) * Time.timeScale, 0.1f, 3f);
                if (waveChargePaused) { waveChargeAudio.UnPause(); waveChargePaused = false; }
            }
        }

        private void StopWaveChargeSound(bool immediately)
        {
            if (waveChargeAudio == null) return;
            if (!immediately && waveChargeFade != null) return;
            if (waveChargeFade != null) StopCoroutine(waveChargeFade);
            waveChargeFade = null;
            if (immediately || waveChargePaused || !isActiveAndEnabled)
            {
                waveChargeAudio.Stop();
                waveChargePaused = false;
            }
            else if (waveChargeAudio.isPlaying)
                waveChargeFade = StartCoroutine(FadeWaveChargeSound());
        }

        private IEnumerator FadeWaveChargeSound()
        {
            float initialVolume = waveChargeAudio.volume;
            for (float elapsed = 0f; elapsed < 0.05f; elapsed += Time.unscaledDeltaTime)
            {
                waveChargeAudio.volume = initialVolume * (1f - Mathf.Clamp01(elapsed / 0.05f));
                yield return null;
            }
            waveChargeAudio.Stop();
            waveChargeFade = null;
        }

        private IEnumerator WaitForWave()
        {
            yield return null;
            while ((animationController.IsAttacking || activeWave != null) && !entity.IsDead)
                yield return null;
            if (movement != null) movement.SetMovementLocked(false);
            wavePending = false;
            StopWaveChargeSound(false);
            waveRoutine = null;
        }

        public void OnEnergyWaveRelease()
        {
            if (!wavePending || entity.IsDead || entity.isKnockback) return;
            wavePending = false;
            StopWaveChargeSound(false);
            activeWave = MonkEnergyWave.Launch(wavePrefab, entity, energy, waveDirection,
                waveRange, waveDamageMultiplier, enemyLayers);
        }
        private IEnumerator WaitForPunch()
        {
            yield return null;
            while (animationController.IsAttacking && !entity.IsDead)
            {
                // Keep the thrust active briefly so moving targets do not need to
                // overlap on one exact animation frame. Each enemy is damaged once per strike.
                if (lastStrike > 0 && Time.time <= punchHitWindowEnd
                    && !entity.isKnockback && Time.timeScale > 0f)
                    ApplyPunchHits(lastStrike);
                yield return null;
            }
            ClearPunch();
            punchRoutine = null;
        }

        public void OnStraightPunchSwing(int strike)
        {
            if (!punchActive || entity.IsDead || entity.isKnockback || !animationController.IsAttacking
                || strike != lastSwing + 1 || strike > straightPunchSkill.DamageHitCount)
                return;
            lastSwing = strike;
            GetComponent<PlayerBasicAttackAudio>()?.Play();
        }

        public void OnStraightPunchHit(int strike)
        {
            if (!punchActive || entity.IsDead || entity.isKnockback
                || strike != lastStrike + 1 || strike > straightPunchSkill.DamageHitCount)
                return;
            lastStrike = strike;

            // Each thrust has its own hit set, so both strikes deal their full damage.
            hitEnemies.Clear();

            punchHitWindowEnd = Time.time + punchHitWindow;
            ApplyPunchHits(strike);
        }

        private IEnumerator WaitForDash()
        {
            while (movement != null && movement.IsDashing && !entity.IsDead)
                yield return null;
            if (movement != null) movement.EndDash();
            if (animationController != null) animationController.FinishSkillAnimation();
            dashRoutine = null;
        }

        private void ApplyPunchHits(int strike)
        {
            Vector2 center = (Vector2)transform.position
                + new Vector2(punchOffset.x * punchDirection, punchOffset.y);
            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = enemyLayers,
                useTriggers = true
            };
            int count = Physics2D.OverlapBox(center, punchSize, 0f, filter, hitBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider2D collider = hitBuffer[i];
                EnemyController enemy = collider != null
                    ? collider.GetComponentInParent<EnemyController>() : null;
                if (enemy == null || enemy.HP <= 0f || !hitEnemies.Add(enemy)) continue;
                PunchHit hit = new PunchHit
                {
                    Enemy = enemy,
                    TextPrefab = enemy.damageTextPrefab,
                    TextPosition = enemy.textSpawnPoint != null
                        ? enemy.textSpawnPoint.position : enemy.transform.position,
                    ImpactPosition = collider.ClosestPoint(center),
                    ImpactOffset = (Vector3)collider.ClosestPoint(center) - enemy.transform.position
                };
                float before = enemy.HP;
                entity.NotifyAttackTarget(enemy);
                enemy.TakeDamage(punchDamage, punchCritical, false, punchHitSound, punchHitSoundVolume);
                float dealt = Mathf.Max(0f, before - enemy.HP);
                if (dealt <= 0f) continue;
                entity.ApplyLifeSteal(dealt);
                energy.RegisterCombat();
                if (!punchEnergyGranted)
                {
                    energy.Gain(punchEnergyOnHit);
                    punchEnergyGranted = true;
                }
                if (enemy.HP > 0f) enemy.ApplyKnockback(punchDirection);
                ShowPunchHit(hit, strike);
            }
        }

        private void ShowPunchHit(PunchHit hit, int strike, bool showImpact = true)
        {
            Vector3 origin = hit.Enemy != null && hit.Enemy.textSpawnPoint != null
                ? hit.Enemy.textSpawnPoint.position : hit.TextPosition;
            if (hit.TextPrefab != null)
            {
                Vector3 offset = new Vector3(strike == 1 ? -0.18f : 0.18f,
                    strike == 1 ? 0f : 0.2f, 0f);
                GameObject text = Instantiate(hit.TextPrefab, origin + offset, Quaternion.identity);
                text.GetComponent<DamageText>()?.Setup(punchDamage, punchCritical);
            }
            if (showImpact && punchImpactPrefab != null)
            {
                GameObject impact = Instantiate(punchImpactPrefab,
                    hit.Enemy != null ? hit.Enemy.transform.position + hit.ImpactOffset
                        : hit.ImpactPosition, Quaternion.identity);
                Vector3 scale = impact.transform.localScale;
                scale.x = Mathf.Abs(scale.x) * punchDirection;
                impact.transform.localScale = scale;
            }
        }

        private void ClearPunch()
        {
            punchActive = false;
            punchHitWindowEnd = 0f;
            hitEnemies.Clear();
            System.Array.Clear(hitBuffer, 0, hitBuffer.Length);
        }

        private void OnDisable()
        {
            StopWaveChargeSound(true);
            if (dashAudio != null) dashAudio.Stop();
            if (dashRoutine != null)
            {
                StopCoroutine(dashRoutine);
                dashRoutine = null;
                if (movement != null) movement.EndDash();
                if (animationController != null) animationController.FinishSkillAnimation();
            }
            if (punchRoutine != null) StopCoroutine(punchRoutine);
            punchRoutine = null;
            ClearPunch();
            if (waveRoutine != null) StopCoroutine(waveRoutine);
            waveRoutine = null;
            wavePending = false;
            if (activeWave != null) Destroy(activeWave.gameObject);
            activeWave = null;
            if (movement != null) movement.SetMovementLocked(false);
        }
        private float GetDirection()
        {
            float direction = animationController != null ? animationController.FacingDirectionX : 1f;
            if (Mathf.Approximately(direction, 0f)) direction = 1f;
            return invertFacingDirection ? -direction : direction;
        }

        private void OnDrawGizmosSelected()
        {
            PlayerIdleAnimation facing = animationController != null
                ? animationController : GetComponent<PlayerIdleAnimation>();
            float direction = facing != null ? facing.FacingDirectionX : 1f;
            if (Mathf.Approximately(direction, 0f)) direction = 1f;
            direction = punchActive ? punchDirection
                : (invertFacingDirection ? -direction : direction);
            Vector2 center = (Vector2)transform.position
                + new Vector2(punchOffset.x * direction, punchOffset.y);
            Gizmos.color = new Color(1f, 0.75f, 0.15f, 0.6f);
            Gizmos.DrawWireCube(center, punchSize);
        }

        private void OnValidate()
        {
            punchSize.x = Mathf.Max(0.01f, punchSize.x);
            punchSize.y = Mathf.Max(0.01f, punchSize.y);
            punchHitWindow = Mathf.Max(0f, punchHitWindow);
        }
    }
}
