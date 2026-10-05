using UnityEngine;

namespace EasternFantasy.Skill
{
    // Follow the authored clip's timeline without changing its sprites or events.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class MonkPunchAnimationEvents : MonoBehaviour
    {
        private static readonly int StabState = Animator.StringToHash("MonkStab");
        private MonkSkillCaster caster;
        private Animator animator;
        private AnimationClip stabClip;
        private int nextStrike = 1;
        private float previousTime = -1f;

        private void Awake()
        {
            caster = GetComponentInParent<MonkSkillCaster>();
            animator = GetComponent<Animator>();
            if (animator.runtimeAnimatorController == null) return;
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == "MonkStab")
                {
                    stabClip = clip;
                    break;
                }
        }

        private void LateUpdate() => ProcessPunchFrames();

        // Existing clip events may be at older times. Use the actual timeline,
        // so those events cannot add another hit or move the hit to a later frame.
        public void StraightPunchHit(int strike) => ProcessPunchFrames();

        private void ProcessPunchFrames()
        {
            if (caster == null || stabClip == null || !animator.isInitialized) return;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash != StabState)
            {
                if (animator.IsInTransition(0))
                    state = animator.GetNextAnimatorStateInfo(0);
                if (state.shortNameHash != StabState)
                {
                    ResetTimeline();
                    return;
                }
            }

            float clipTime = state.normalizedTime * stabClip.length;
            if (clipTime < previousTime) nextStrike = 1;
            previousTime = clipTime;

            // Unity's timeline frames are zero-based: upper punch at 2, lower at 4.
            while (nextStrike <= 2
                && clipTime + 0.00001f >= (nextStrike * 2f) / stabClip.frameRate)
            {
                caster.OnStraightPunchHit(nextStrike);
                nextStrike++;
            }
        }

        private void ResetTimeline()
        {
            nextStrike = 1;
            previousTime = -1f;
        }

        private void OnDisable() => ResetTimeline();
    }
}