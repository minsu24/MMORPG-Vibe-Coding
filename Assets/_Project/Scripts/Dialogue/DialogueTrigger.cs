using System.Collections;
using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

namespace EasternFantasy.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueSequence sequence;
        [SerializeField] private bool startOnPlayerEnter;
        [Tooltip("Play this sequence only once per game session, including scene revisits.")]
        [SerializeField] private bool playOnlyOnce;

        private static readonly HashSet<DialogueSequence> playedSequences = new HashSet<DialogueSequence>();
        private Coroutine pendingEntryDialogue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPlayedSequences()
        {
            // Also reset when entering Play Mode with domain reload disabled.
            playedSequences.Clear();
        }

        // Call this from an NPC interaction script, UnityEvent, button, or cutscene.
        public void PlayDialogue()
        {
            if (playOnlyOnce && sequence != null && playedSequences.Contains(sequence))
                return;

            if (DialogueManager.Instance == null)
            {
                Debug.LogError("The scene needs one DialogueManager.", this);
                return;
            }

            if (sequence == null)
            {
                Debug.LogWarning("DialogueTrigger needs a DialogueSequence.", this);
                return;
            }

            // A busy dialogue manager must not consume this sequence's first playback.
            if (DialogueManager.Instance.StartDialogue(sequence) && playOnlyOnce)
                playedSequences.Add(sequence);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!startOnPlayerEnter || !other.CompareTag("Player") || pendingEntryDialogue != null)
                return;
            PlayerMovement2D movement = other.GetComponentInParent<PlayerMovement2D>();
            pendingEntryDialogue = StartCoroutine(PlayAfterLanding(movement));
        }
        private IEnumerator PlayAfterLanding(PlayerMovement2D movement)
        {
            // Automatic dialogue pauses physics; let the entering player land first.
            yield return null;
            while (movement != null && movement.isActiveAndEnabled && !movement.IsGrounded)
                yield return null;
            pendingEntryDialogue = null;
            if (movement != null && movement.isActiveAndEnabled)
                PlayDialogue();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || pendingEntryDialogue == null) return;
            StopCoroutine(pendingEntryDialogue);
            pendingEntryDialogue = null;
        }

        private void OnDisable()
        {
            if (pendingEntryDialogue != null) StopCoroutine(pendingEntryDialogue);
            pendingEntryDialogue = null;
        }
    }
}
