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
                animator.Play(HammerState, 0, 0f);
            }
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
