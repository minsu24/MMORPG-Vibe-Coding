using UnityEngine;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerEntity))]
    public sealed class PlayerDeathAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip deathSound;
        [SerializeField, Range(0f, 1f)] private float volume = .45f;
        private PlayerEntity entity;
        private AudioSource deathAudio;
        private bool played;

        private void Awake()
        {
            entity = GetComponent<PlayerEntity>();
            var child = new GameObject("Death Voice Audio");
            child.transform.SetParent(transform, false);
            deathAudio = child.AddComponent<AudioSource>();
            deathAudio.playOnAwake = false;
            deathAudio.loop = false;
            deathAudio.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            if (!entity.IsDead) played = false;
            entity.Died += OnDeath;
            entity.StatsChanged += OnStatsChanged;
        }

        private void OnDeath()
        {
            if (played || deathSound == null) return;
            played = true;
            deathAudio.clip = deathSound;
            deathAudio.volume = volume;
            deathAudio.Play();
        }

        private void OnStatsChanged()
        {
            if (entity.IsDead) return;
            played = false;
            deathAudio.Stop();
        }

        private void OnDisable()
        {
            if (entity != null)
            {
                entity.Died -= OnDeath;
                entity.StatsChanged -= OnStatsChanged;
            }
            if (deathAudio != null) deathAudio.Stop();
        }
    }
}
