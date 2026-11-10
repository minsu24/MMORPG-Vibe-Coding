using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasternFantasy.Player;

namespace EasternFantasy.Skill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerEntity), typeof(PlayerMovement2D), typeof(PlayerIdleAnimation))]
    public sealed class SwordswomanDrawingAttack : MonoBehaviour
    {
        [Header("Drawing - Movement and Path Range")]
        [SerializeField, Min(0.01f)] private float drawingDistance = 8f;
        [SerializeField, Min(0.01f)] private float pathHeight = 1.8f;
        [SerializeField] private float pathVerticalOffset = 0.15f;
        [SerializeField, Min(0f), Tooltip("Extra range at each end of the actual movement path.")]
        private float pathEndPadding = 0.25f;
        [SerializeField] private LayerMask enemyLayers;
        [Header("Path Effect")]
        [SerializeField] private SwordDrawingTrailEffect trailPrefab;
        [Header("Drawing Audio")]
        [SerializeField] private AudioClip chargeSound, releaseSound, hitSound;
        [SerializeField, Range(0f, 1f)] private float chargeSoundVolume = .4f;
        [SerializeField, Range(0f, 1f)] private float releaseSoundVolume = .5f;
        [SerializeField, Range(0f, 1f)] private float hitSoundVolume = .35f;

        private PlayerEntity entity;
        private PlayerSkillSystem skills;
        private float damageMultiplier = 5f;
        private PlayerMovement2D movement;
        private PlayerIdleAnimation animationController;
        private CapsuleCollider2D bodyCollider;
        private SpriteRenderer characterRenderer;
        private Collider2D[] hitBuffer = new Collider2D[16];
        private RaycastHit2D[] wallBuffer = new RaycastHit2D[16];
        private readonly HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>();
        private Coroutine drawingRoutine;
        private bool released;
        private AudioSource chargeAudio, releaseAudio;
        private bool chargePaused;
        private float direction;
        public bool IsActive { get; private set; }

        private void Awake()
        {
            entity = GetComponent<PlayerEntity>();
            skills = GetComponent<PlayerSkillSystem>();
            movement = GetComponent<PlayerMovement2D>();
            animationController = GetComponent<PlayerIdleAnimation>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            var animator = GetComponentInChildren<Animator>();
            characterRenderer = animator != null ? animator.GetComponent<SpriteRenderer>() : null;
            if (enemyLayers.value == 0)
            {
                int layer = LayerMask.NameToLayer("Enemy");
                if (layer >= 0) enemyLayers = 1 << layer;
            }
        }

        public bool TryStart(SkillDefinition skill)
        {
            if (!isActiveAndEnabled || IsActive || entity.IsDead || entity.isKnockback
                || skill == null || skills == null || skills.GetSkillLevel(skill) <= 0
                || movement.IsMovementLocked || movement.IsDashing
                || !SkillResourcePayment.CanAfford(entity, skill)) return false;
            if (!animationController.TryRequestSkillAnimation("SwordDrawing")) return false;
            if (!SkillResourcePayment.TrySpend(entity, skill))
            {
                animationController.FinishSkillAnimation();
                return false;
            }
            damageMultiplier = skill.GetDamageMultiplier(skills.GetSkillLevel(skill));
            direction = animationController.FacingDirectionX < 0f ? -1f : 1f;
            IsActive = true;
            released = false;
            movement.SetMovementLocked(true);
            PlayChargeSound();
            drawingRoutine = StartCoroutine(WaitForDrawing());
            return true;
        }

        // Invoked by the ninth frame's animation event. Duplicate events cannot hit twice.
        public void Release()
        {
            if (!IsActive || released || entity.IsDead || entity.isKnockback) return;
            released = true;
            StopChargeSound();
            if (releaseSound != null)
            {
                if (releaseAudio == null) releaseAudio = CreateAudioSource("Drawing Release Audio");
                releaseAudio.PlayOneShot(releaseSound, releaseSoundVolume);
            }
            Physics2D.SyncTransforms();
            Vector2 origin = movement.body.position;
            float distance = GetUnblockedDistance();
            Vector2 destination = origin + Vector2.right * (direction * distance);
            movement.PrepareEnemyPassThrough();
            movement.TeleportTo(destination, transform.rotation);
            if (distance <= 0.001f) return;
            Vector2 center = (origin + destination) * 0.5f + Vector2.up * pathVerticalOffset;
            Vector2 size = new Vector2(distance + pathEndPadding * 2f, pathHeight);
            ApplyPathHit(center, size);
            if (trailPrefab != null)
            {
                var trail = Instantiate(trailPrefab, center, Quaternion.identity);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(trail.gameObject, gameObject.scene);
                trail.Play(distance, pathHeight, direction, characterRenderer);
            }
        }

        private float GetUnblockedDistance()
        {
            float distance = drawingDistance;
            ContactFilter2D filter = new ContactFilter2D
            {
                useTriggers = false, useLayerMask = true, layerMask = movement.PassThroughObstacleMask & ~enemyLayers.value
            };
            int count;
            while (true)
            {
                count = bodyCollider.Cast(Vector2.right * direction, filter, wallBuffer, drawingDistance + 0.02f);
                if (count < wallBuffer.Length) break;
                System.Array.Resize(ref wallBuffer, wallBuffer.Length * 2);
            }
            for (int i = 0; i < count; i++)
            {
                if (wallBuffer[i].collider == null || wallBuffer[i].collider.GetComponentInParent<EnemyController>() != null) continue;
                if (Vector2.Dot(wallBuffer[i].normal, Vector2.right * direction) >= -0.1f) continue;
                distance = Mathf.Min(distance, Mathf.Max(0f, wallBuffer[i].distance - 0.02f));
            }
            return distance;
        }

        private void ApplyPathHit(Vector2 center, Vector2 size)
        {
            PhysicsScene2D physics = gameObject.scene.GetPhysicsScene2D();
            ContactFilter2D filter = new ContactFilter2D
            {
                useTriggers = true, useLayerMask = true, layerMask = enemyLayers
            };
            int count;
            while (true)
            {
                count = physics.OverlapBox(center, size, 0f, filter, hitBuffer);
                if (count < hitBuffer.Length) break;
                System.Array.Resize(ref hitBuffer, hitBuffer.Length * 2);
            }
            hitEnemies.Clear();
            float damage = entity.RollAttackDamage(out bool critical) * damageMultiplier;
            for (int i = 0; i < count; i++)
            {
                var enemy = hitBuffer[i] != null ? hitBuffer[i].GetComponentInParent<EnemyController>() : null;
                if (enemy == null || enemy.HP <= 0f || !hitEnemies.Add(enemy)) continue;
                float before = enemy.HP;
                Vector3 hitPosition = enemy.transform.position;
                entity.NotifyAttackTarget(enemy);
                // Keep damage/visual feedback, and replace the throttled shared sound per target.
                enemy.TakeDamage(damage, critical, true, hitSound, hitSound != null ? 0f : 1f);
                float dealt = Mathf.Max(0f, before - enemy.HP);
                if (dealt > 0f) CombatImpactFeedback.PlayIndividualImpactSound(hitPosition, hitSound, hitSoundVolume, critical);
                entity.ApplyLifeSteal(dealt);
                if (enemy.HP > 0f) enemy.ApplyKnockback(direction);
            }
        }

        private IEnumerator WaitForDrawing()
        {
            yield return null;
            while (animationController.IsAttacking && !entity.IsDead && !entity.isKnockback)
                yield return null;
            FinishDrawing();
        }

        private AudioSource CreateAudioSource(string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);
            var source = root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void PlayChargeSound()
        {
            if (chargeSound == null) return;
            if (chargeAudio == null) chargeAudio = CreateAudioSource("Drawing Charge Audio");
            chargePaused = false;
            chargeAudio.clip = chargeSound;
            chargeAudio.volume = chargeSoundVolume;
            chargeAudio.pitch = Mathf.Clamp(Time.timeScale, .1f, 3f);
            chargeAudio.Play();
        }

        private void StopChargeSound()
        {
            if (chargeAudio != null) chargeAudio.Stop();
            chargePaused = false;
        }

        private void Update()
        {
            if (!IsActive || released || chargeAudio == null) return;
            if (entity.IsDead || entity.isKnockback || !animationController.IsAttacking)
            { StopChargeSound(); return; }
            if (Time.timeScale <= 0f)
            {
                if (!chargePaused) { chargeAudio.Pause(); chargePaused = true; }
            }
            else
            {
                chargeAudio.pitch = Mathf.Clamp(Time.timeScale, .1f, 3f);
                if (chargePaused) { chargeAudio.UnPause(); chargePaused = false; }
            }
        }

        private void FinishDrawing()
        {
            StopChargeSound();
            IsActive = false;
            drawingRoutine = null;
            movement.SetMovementLocked(false);
            animationController.FinishSkillAnimation();
            if (!entity.IsDead) animationController.SetJumping(!movement.IsGrounded);
        }

        private void OnDisable()
        {
            StopChargeSound();
            if (releaseAudio != null) releaseAudio.Stop();
            if (drawingRoutine != null) StopCoroutine(drawingRoutine);
            if (IsActive) FinishDrawing();
            hitEnemies.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            var animation = GetComponent<PlayerIdleAnimation>();
            float facing = animation != null && animation.FacingDirectionX < 0f ? -1f : 1f;
            Gizmos.color = new Color(1f, 0.2f, 0.15f, 0.8f);
            Gizmos.DrawWireCube(transform.position
                + new Vector3(drawingDistance * 0.5f * facing, pathVerticalOffset),
                new Vector3(drawingDistance + pathEndPadding * 2f, pathHeight, 0f));
        }
    }
}
