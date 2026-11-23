using UnityEngine;

namespace EasternFantasy.EquipmentUpgrade
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class BlacksmithAmbientAnimation : MonoBehaviour
    {
        public const string IdleState = "BlacksmithIdle";
        public const string HammerState = "BlacksmithHammer";
        [SerializeField] private Vector2 hammerInterval = new Vector2(6f, 12f);
        [SerializeField, Min(.1f)] private float hammerDuration = 1.1f;
        private Animator animator;
        private AudioSource hammerAudio;
        private LocalAmbientAudio2D hammerDistanceAudio;
        private bool hammerSoundPlayed;
        private float elapsed, nextHammerDelay;
        public bool IsHammering { get; private set; }

        private void OnEnable()
        {
            animator = GetComponent<Animator>();
            ReturnToIdle();
        }

        private void Update() => Advance(Time.deltaTime);

        // Shared by frame updates and the isolated animation validation.
        public void Advance(float deltaTime)
        {
            if (!isActiveAndEnabled || animator == null || animator.runtimeAnimatorController == null) return;
            elapsed += Mathf.Max(0f, deltaTime);
            if (IsHammering)
            {
                if (elapsed >= hammerDuration) ReturnToIdle();
            }
            else if (elapsed >= nextHammerDelay)
            {
                elapsed = 0f;
                IsHammering = true;
                hammerSoundPlayed = false;
                animator.Play(HammerState, 0, 0f);
            }
        }

        // Animation event on the hammer's contact frame.
        public void PlayHammerImpact()
        {
            if (!isActiveAndEnabled || !IsHammering || hammerSoundPlayed) return;
            if (hammerAudio == null)
            {
                var clip = Resources.Load<AudioClip>("Audio/BlacksmithHammer");
                if (clip == null) return;
                var audioObject = new GameObject("Hammer Impact Audio");
                audioObject.transform.SetParent(transform, false);
                hammerAudio = audioObject.AddComponent<AudioSource>();
                hammerAudio.playOnAwake = false;
                hammerAudio.loop = false;
                hammerAudio.spatialBlend = 0f;
                hammerDistanceAudio = audioObject.AddComponent<LocalAmbientAudio2D>();
                hammerAudio.clip = clip;
            }
            hammerSoundPlayed = true;
            hammerDistanceAudio.PrepareOneShot(.18f, 1.5f, 10f);
            hammerAudio.Play();
        }

        private void OnDisable()
        {
            if (hammerAudio != null) hammerAudio.Stop();
            IsHammering = false;
        }

        private void ReturnToIdle()
        {
            elapsed = 0f;
            IsHammering = false;
            float minimum = Mathf.Max(.5f, hammerInterval.x);
            nextHammerDelay = Random.Range(minimum, Mathf.Max(minimum, hammerInterval.y));
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.Play(IdleState, 0, 0f);
        }
    }
}
