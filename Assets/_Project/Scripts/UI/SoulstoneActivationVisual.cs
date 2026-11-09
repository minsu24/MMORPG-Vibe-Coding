using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace EasternFantasy.UI
{
    [RequireComponent(typeof(Animator))]
    public sealed class SoulstoneActivationVisual : MonoBehaviour
    {
        public bool IsActivating { get; private set; }
        public string PlayingClip => current != null ? current.name : "";
        public bool IsDormant { get; private set; }
        private bool temple, initialized;
        private AnimationClip activation, loop, current;
        private PlayableGraph graph;
        private AnimationPlayableOutput output;
        private AnimationClipPlayable playable;
        private float elapsed;

        public void Initialize(bool isTemple, SoulstoneCatalog catalog)
        {
            if (initialized) return;
            initialized = true; temple = isTemple;
            activation = temple ? catalog.templeActivation : catalog.villageActivation;
            loop = temple ? catalog.templeLoop : catalog.villageLoop;
            var animator = GetComponent<Animator>();
            // Explicit clips avoid relying on differently named states in the existing controllers.
            animator.runtimeAnimatorController = null;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            graph = PlayableGraph.Create("Soulstone " + gameObject.scene.name);
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            output = AnimationPlayableOutput.Create(graph, "Stone Sprite", animator);
            SoulstoneProgress.Changed += ApplyProgress;
            ApplyProgress(false);
        }
        private void ApplyProgress(bool newlyActivated)
        {
            if (!SoulstoneProgress.IsActivated) Play(activation, dormant: true);
            else if ((temple && newlyActivated) || (!temple && !SoulstoneProgress.VillageAwakened)) Play(activation, activating: true);
            else Play(loop);
        }
        private void Play(AnimationClip clip, bool dormant = false, bool activating = false)
        {
            if (clip == null) { Debug.LogError("Soulstone animation clip is missing.", this); return; }
            if (playable.IsValid()) graph.DestroyPlayable(playable);
            current = clip; elapsed = 0; IsDormant = dormant; IsActivating = activating;
            playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false); playable.SetSpeed(dormant ? 0 : 1);
            output.SetSourcePlayable(playable); graph.Play(); graph.Evaluate(0);
        }
        private void Update()
        {
            if (!initialized || !graph.IsValid() || IsDormant || current == null) return;
            elapsed += Time.unscaledDeltaTime;
            if (IsActivating && elapsed >= current.length)
            {
                if (!temple) SoulstoneProgress.FinishVillageAwakening();
                Play(loop); return;
            }
            graph.Evaluate(Time.unscaledDeltaTime);
        }
        private void OnDestroy()
        {
            SoulstoneProgress.Changed -= ApplyProgress;
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
