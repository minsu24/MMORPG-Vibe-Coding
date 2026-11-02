using UnityEngine;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerEntity))]
    public sealed class PlayerHurtAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip hurtSound;
        [SerializeField, Range(0f, 1f)] private float volume = .35f;
        [SerializeField, Min(0f)] private float minimumInterval = .2f;

        private PlayerEntity entity;
        private AudioSource hurtAudio;
        private float nextAllowedAt = float.NegativeInfinity;

        private void Awake()
        {
            entity = GetComponent<PlayerEntity>();
            var child = new GameObject("Hurt Voice Audio");
            child.transform.SetParent(transform, false);
            hurtAudio = child.AddComponent<AudioSource>();
            hurtAudio.playOnAwake = false;
            hurtAudio.loop = false;
            hurtAudio.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            entity.Damaged += OnDamaged;
            entity.Died += StopVoice;
        }

        private void StopVoice() => hurtAudio.Stop();

        private void OnDamaged(float damage)
        {
            if (damage <= 0f || entity.IsDead || hurtSound == null || hurtAudio.isPlaying
                || Time.unscaledTime < nextAllowedAt) return;
            nextAllowedAt = Time.unscaledTime + minimumInterval;
            hurtAudio.PlayOneShot(hurtSound, volume);
        }

        private void OnDisable()
        {
            if (entity != null)
            {
                entity.Damaged -= OnDamaged;
                entity.Died -= StopVoice;
            }
            if (hurtAudio != null) hurtAudio.Stop();
            nextAllowedAt = float.NegativeInfinity;
        }
    }
}
