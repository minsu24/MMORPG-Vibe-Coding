using UnityEngine;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerEntity))]
    public sealed class PlayerDeathEffectAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip effectSound;
        [SerializeField, Range(0f, 1f)] private float volume = .4f;
        private PlayerEntity entity;
        private AudioSource source;
        private bool played;

        private void Awake()
        {
            entity = GetComponent<PlayerEntity>();
            var child = new GameObject("Death Effect Audio");
            child.transform.SetParent(transform, false);
            source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            if (!entity.IsDead) played = false;
            entity.StatsChanged += ResetAfterRevive;
        }

        // Called by a death animation event, so hit stop and animation speed
        // changes cannot detach the sound from the actual visual frame.
        public void PlayDeathEffect()
        {
            if (played || !entity.IsDead || effectSound == null) return;
            played = true;
            source.clip = effectSound;
            source.volume = volume;
            source.Play();
        }

        private void ResetAfterRevive()
        {
            if (entity.IsDead) return;
            played = false;
            source.Stop();
        }

        private void OnDisable()
        {
            if (entity != null) entity.StatsChanged -= ResetAfterRevive;
            if (source != null) source.Stop();
        }
    }
}
