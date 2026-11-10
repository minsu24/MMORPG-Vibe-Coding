using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

namespace EasternFantasy.Skill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement2D))]
    [RequireComponent(typeof(PlayerProjectileShooter))]
    [RequireComponent(typeof(PlayerEntity))]
    public sealed class PlayerActiveSkillCaster : MonoBehaviour, IPlayerSkillCaster
    {
        [Header("Skill Definitions")]
        [SerializeField] private SkillDefinition tripleTalismanSkill;
        [SerializeField] private SkillDefinition teleportSkill;
        [SerializeField] private SkillDefinition talismanShieldSkill;

        [Header("Triple Talisman")]
        [SerializeField, Min(0f)] private float projectileVerticalSpacing = 0.28f;
        [SerializeField, Min(0f)] private float damageMultiplier = 0.8f;
        [SerializeField] private AudioClip talismanBurstSound;
        [SerializeField, Range(0f, 1f)] private float talismanSoundVolume = 0.7f;

        [Header("Teleport")]
        [SerializeField] private GameObject teleportEffectPrefab;
        [SerializeField, Min(0f)] private float teleportEffectLifetime = 0.4f;
        [SerializeField] private AudioClip teleportSound;
        [SerializeField, Range(0f, 1f)] private float teleportSoundVolume = 0.75f;

        [Header("Talisman Shield")]
        [SerializeField, Min(0f)] private float shieldDuration = 5f;
        [SerializeField] private TalismanShieldEffect shieldEffect;
        [SerializeField] private AudioClip shieldSound;
        [SerializeField, Range(0f, 1f)] private float shieldSoundVolume = 0.5f;

        private readonly Dictionary<SkillDefinition, float> cooldownEnds =
            new Dictionary<SkillDefinition, float>();
        private PlayerMovement2D movement;
        private PlayerProjectileShooter shooter;
        private PlayerEntity entity;
        private PlayerSkillSystem skillSystem;
        private PlayerIdleAnimation animationController;
        private AudioSource skillAudio;
        private SpriteRenderer bodyRenderer;
        private readonly List<SpriteRenderer> arrivalEffects = new List<SpriteRenderer>();

        private void Awake()
        {
            movement = GetComponent<PlayerMovement2D>();
            shooter = GetComponent<PlayerProjectileShooter>();
            entity = GetComponent<PlayerEntity>();
            skillSystem = GetComponent<PlayerSkillSystem>();
            animationController = GetComponent<PlayerIdleAnimation>();
            Animator bodyAnimator = GetComponentInChildren<Animator>();
            if (bodyAnimator != null)
                bodyRenderer = bodyAnimator.GetComponent<SpriteRenderer>();
        }

        public bool TryCast(SkillDefinition skill)
        {
            if (!isActiveAndEnabled || !animationController.CanPerformCombatAction || Time.timeScale <= 0f
                || skill == null || skill.ActivationType != SkillActivationType.Active
                || GetCooldownRemaining(skill) > 0f)
                return false;
            if (skillSystem == null || skillSystem.GetSkillLevel(skill) <= 0)
                return false;
            if (!SkillResourcePayment.CanAfford(entity, skill))
                return false;
            bool cast;
            if (skill == tripleTalismanSkill)
            {
                cast = shooter.TryFireBurst(3, projectileVerticalSpacing, damageMultiplier,
                    () => PlaySkillSound(talismanBurstSound, talismanSoundVolume));
            }
            else if (skill == teleportSkill)
            {
                if (animationController.IsAttacking && !animationController.CanChainBasicAttack) return false;
                animationController.CancelBasicAttack();
                animationController.RefreshAttackFacing();
                float direction = animationController != null
                    ? animationController.FacingDirectionX
                    : 1f;
                Vector3 origin = transform.position;
                SpawnTeleportEffect(origin);
                movement.TeleportTo(
                    origin + Vector3.right * direction * skill.GetTeleportDistance(skillSystem.GetSkillLevel(skill)),
                    transform.rotation);
                SpawnTeleportEffect(transform.position, true);
                PlaySkillSound(teleportSound, teleportSoundVolume);
                cast = true;
            }
            else if (skill == talismanShieldSkill)
            {
                entity.ActivateDamageReduction(
                    skill.GetDamageReduction(skillSystem.GetSkillLevel(skill)), shieldDuration);
                if (shieldEffect != null)
                    shieldEffect.Show(shieldDuration);
                PlaySkillSound(shieldSound, shieldSoundVolume);
                cast = true;
            }
            else
            {
                return false;
            }

            if (cast)
            {
                if (!SkillResourcePayment.TrySpend(entity, skill))
                    return false;
                cooldownEnds[skill] = Time.time + skill.GetCooldownSeconds(skillSystem.GetSkillLevel(skill));
            }

            return cast;
        }

        private void SpawnTeleportEffect(Vector3 position, bool followBody = false)
        {
            if (teleportEffectPrefab == null)
                return;

            GameObject effect = Instantiate(teleportEffectPrefab, position, Quaternion.identity);
            if (followBody && bodyRenderer != null)
            {
                SpriteRenderer effectRenderer = effect.GetComponent<SpriteRenderer>();
                if (effectRenderer != null)
                {
                    AlignArrivalEffect(effectRenderer);
                    arrivalEffects.Add(effectRenderer);
                }
            }
            Destroy(effect, teleportEffectLifetime);
        }

        private void LateUpdate()
        {
            for (int index = arrivalEffects.Count - 1; index >= 0; index--)
            {
                SpriteRenderer effect = arrivalEffects[index];
                if (effect == null)
                    arrivalEffects.RemoveAt(index);
                else if (bodyRenderer != null)
                    AlignArrivalEffect(effect);
            }
        }

        private void AlignArrivalEffect(SpriteRenderer effect)
        {
            // Recenter each animation frame, including sprites with an offset pivot.
            Vector3 offset = bodyRenderer.bounds.center - effect.bounds.center;
            offset.z = 0f;
            effect.transform.position += offset;
            effect.sortingLayerID = bodyRenderer.sortingLayerID;
            effect.sortingOrder = bodyRenderer.sortingOrder + 2;
        }

        private void PlaySkillSound(AudioClip clip, float volume)
        {
            if (clip == null) return;
            if (skillAudio == null)
            {
                var audioObject = new GameObject("Dosa Skill Audio");
                audioObject.transform.SetParent(transform, false);
                skillAudio = audioObject.AddComponent<AudioSource>();
                skillAudio.playOnAwake = false;
                skillAudio.loop = false;
                skillAudio.spatialBlend = 0f;
                skillAudio.volume = 0.5f;
            }
            skillAudio.PlayOneShot(clip, volume);
        }

        private void OnDisable()
        {
            if (skillAudio != null) skillAudio.Stop();
        }

        public float GetCooldownRemaining(SkillDefinition skill)
        {
            if (skill == null || !cooldownEnds.TryGetValue(skill, out float endTime))
                return 0f;
            return Mathf.Max(0f, endTime - Time.time);
        }
    }
}
