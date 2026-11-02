using UnityEngine;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(Animator))]
    public sealed class DeathAnimationAudioEvents : MonoBehaviour
    {
        private PlayerDeathEffectAudio effect;

        private void Awake() => effect = GetComponentInParent<PlayerDeathEffectAudio>();

        public void PlayDeathEffect()
        {
            if (effect != null) effect.PlayDeathEffect();
        }
    }
}
