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
        [SerializeField, Min(0.01f)] private float dashDistance = 3f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.18f;
        [SerializeField, Min(0f)] private float parryWindow = 0.35f;
        [SerializeField] private Vector2 punchOffset = new Vector2(1f, 0.2f);
        [SerializeField] private Vector2 punchSize = new Vector2(2.1f, 1.5f);
        [Tooltip("Seconds to keep checking for new targets after each punch frame.")]
        [SerializeField, Min(0f)] private float punchHitWindow = 0.12f;
        [SerializeField, Min(0f)] private float punchDamageMultiplier = 1.25f;
        [SerializeField, Min(0.1f)] private float waveRange = 8f;
        [SerializeField, Min(0f)] private float waveDamageMultiplier = 3.5f;
        [SerializeField] private bool invertFacingDirection = true;
        [SerializeField] private LayerMask enemyLayers;
        [SerializeField] private GameObject punchImpactPrefab;
        [SerializeField] private MonkEnergyWave wavePrefab;

        private readonly Collider2D[] hitBuffer = new Collider2D[32];
        private readonly HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>();
        private readonly Dictionary<SkillDefinition, float> cooldownEnds = new Dictionary<SkillDefinition, float>();
        private PlayerEntity entity;
        private MonkEnergy energy;
        private PlayerIdleAnimation animationController;

        private Coroutine punchRoutine;
        private Coroutine dashRoutine;
        private Coroutine waveRoutine;
        private MonkEnergyWave activeWave;
        private PlayerMovement2D movement;
        private bool wavePending;
        private float waveDirection;
        private bool punchActive;
        private bool punchEnergyGranted;
        private int lastStrike;
        private float punchDirection;
        private float punchHitWindowEnd;
        private float punchDamage;
        private bool punchCritical;
        private readonly List<PunchHit> punchHits = new List<PunchHit>();

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
            animationController = GetComponent<PlayerIdleAnimation>();
            if (enemyLayers.value == 0)
            {
                int layer = LayerMask.NameToLayer("Enemy");
                if (layer >= 0) enemyLayers = 1 << layer;
            }
        }

        public bool TryCast(SkillDefinition skill)
        {
            if (!isActiveAndEnabled || skill == null || entity.IsDead || entity.isKnockback || Time.timeScale <= 0f
                || dashRoutine != null
                || GetCooldownRemaining(skill) > 0f || !SkillResourcePayment.CanAfford(entity, skill)) return false;

            if (skill == straightPunchSkill)
            {
                if (animationController == null || punchActive
                    || !animationController.TryRequestSkillAnimation("MonkStab"))
                    return false;
                punchActive = true;
                punchEnergyGranted = false;
                lastStrike = 0;
                punchHitWindowEnd = 0f;
                punchDirection = GetDirection();
                punchDamage = entity.RollAttackDamage(out punchCritical) * punchDamageMultiplier;
                hitEnemies.Clear();
                punchHits.Clear();
                punchRoutine = StartCoroutine(WaitForPunch());
            }
            else if (skill == dashSkill)
            {
                if (movement == null || !movement.CanDash || animationController == null
                    || !animationController.TryRequestSkillAnimation("MonkDash", dashDuration))
                    return false;
                // Movement direction is independent of the punch sprite's inversion.
                movement.BeginDash(animationController.FacingDirectionX, dashDistance, dashDuration);
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
                wavePending = true;
                waveRoutine = StartCoroutine(WaitForWave());
            }
            else return false;

            if (!SkillResourcePayment.TrySpend(entity, skill)) return false;
            if (skill.CooldownSeconds > 0f)
                cooldownEnds[skill] = Time.time + skill.CooldownSeconds;
            return true;
        }

        public float GetCooldownRemaining(SkillDefinition skill)
        {
            return skill != null && cooldownEnds.TryGetValue(skill, out float end)
                ? Mathf.Max(0f, end - Time.time) : 0f;
        }

        private IEnumerator WaitForWave()
        {
            yield return null;
            while ((animationController.IsAttacking || activeWave != null) && !entity.IsDead)
                yield return null;
            if (movement != null) movement.SetMovementLocked(false);
            wavePending = false;
            waveRoutine = null;
        }

        public void OnEnergyWaveRelease()
        {
            if (!wavePending || entity.IsDead || entity.isKnockback) return;
            wavePending = false;
            activeWave = MonkEnergyWave.Launch(wavePrefab, entity, energy, waveDirection,
                waveRange, waveDamageMultiplier, enemyLayers);
        }
        private IEnumerator WaitForPunch()
        {
            yield return null;
            while (animationController.IsAttacking && !entity.IsDead)
            {
                // Keep the thrust active briefly so moving targets do not need to
                // overlap on one exact animation frame. Each enemy is damaged once.
                if (lastStrike > 0 && Time.time <= punchHitWindowEnd
                    && !entity.isKnockback && Time.timeScale > 0f)
                    ApplyPunchHits(lastStrike);
                yield return null;
            }
            ClearPunch();
            punchRoutine = null;
        }

        public void OnStraightPunchHit(int strike)
        {
            if (!punchActive || entity.IsDead || entity.isKnockback
                || strike != lastStrike + 1 || strike > 2)
                return;
            lastStrike = strike;

            // The second motion repeats feedback, never the damage or hit reaction.
            if (strike == 2)
                foreach (PunchHit hit in punchHits)
                    ShowPunchHit(hit, 2);

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
                enemy.TakeDamage(punchDamage, punchCritical, false);
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
                punchHits.Add(hit);
                ShowPunchHit(hit, 1, strike == 1);
                // An enemy first reached by the second thrust still gets both numbers.
                if (strike == 2) ShowPunchHit(hit, 2);
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
                float first = Mathf.Round(punchDamage * 0.5f);
                text.GetComponent<DamageText>()?.Setup(
                    strike == 1 ? first : punchDamage - first, punchCritical);
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
            punchHits.Clear();
            hitEnemies.Clear();
            System.Array.Clear(hitBuffer, 0, hitBuffer.Length);
        }

        private void OnDisable()
        {
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
