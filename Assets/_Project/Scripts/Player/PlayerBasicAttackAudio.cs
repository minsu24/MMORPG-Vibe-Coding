using UnityEngine;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class PlayerBasicAttackAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip attackSound;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        public void Play()
        {
            if (isActiveAndEnabled && attackSound != null)
                audioSource.PlayOneShot(attackSound, volume);
        }
    }
}
