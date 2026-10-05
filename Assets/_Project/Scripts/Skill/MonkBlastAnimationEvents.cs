using UnityEngine;

namespace EasternFantasy.Skill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class MonkBlastAnimationEvents : MonoBehaviour
    {
        [SerializeField, Min(0)] private int releaseFrame = 7;

        private static readonly int BlastState = Animator.StringToHash("MonkBlast");
        private Animator animator;
        private MonkSkillCaster caster;
        private AnimationClip blastClip;
        private bool released;
        private float previousTime = -1f;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            caster = GetComponentInParent<MonkSkillCaster>();
            if (animator.runtimeAnimatorController == null) return;
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == "MonkBlast")
                {
                    blastClip = clip;
                    break;
                }
        }

        private void LateUpdate() => EnergyWaveRelease();

        public void EnergyWaveRelease()
        {
            if (caster == null || blastClip == null || !animator.isInitialized) return;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash != BlastState)
            {
                if (animator.IsInTransition(0)) state = animator.GetNextAnimatorStateInfo(0);
                if (state.shortNameHash != BlastState)
                {
                    ResetTimeline();
                    return;
                }
            }

            float clipTime = state.normalizedTime * blastClip.length;
            if (clipTime < previousTime) released = false;
            previousTime = clipTime;
            // Release on the first extended-palm pose after charging (zero-based timeline).
            if (!released && clipTime + 0.00001f >= releaseFrame / blastClip.frameRate)
            {
                released = true;
                caster.OnEnergyWaveRelease();
            }
        }

        private void ResetTimeline()
        {
            released = false;
            previousTime = -1f;
        }

        private void OnDisable() => ResetTimeline();
    }
}