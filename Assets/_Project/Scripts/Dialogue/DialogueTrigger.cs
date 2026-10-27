using System.Collections;
using System.Collections.Generic;
using EasternFantasy.Player;
using UnityEngine;

namespace EasternFantasy.Dialogue
{
    [DisallowMultipleComponent]
    public sealed partial class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueSequence sequence;
        [SerializeField] private bool startOnPlayerEnter;
        [Tooltip("Play this sequence only once per game session, including scene revisits.")]
        [SerializeField] private bool playOnlyOnce;
        [Tooltip("Keep a detected entry queued after leaving the trigger, until dialogue is available.")]
        [SerializeField] private bool finishEntryAfterExit;

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
            if (EasternFantasy.Saving.SaveGameService.IsRestoring) return;
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
            if (!startOnPlayerEnter || !other.CompareTag("Player") || pendingEntryDialogue != null
                || (playOnlyOnce && sequence != null && playedSequences.Contains(sequence)))
                return;
            PlayerMovement2D movement = other.GetComponentInParent<PlayerMovement2D>();
            if (movement == null || !movement.isActiveAndEnabled) return;
            // Start in the trigger callback, including while jumping. Physics resumes after dialogue.
            if (TryStartEntryDialogue()) return;
            pendingEntryDialogue = StartCoroutine(WaitForDialogue(movement));
        }
        private bool TryStartEntryDialogue()
        {
            if (EasternFantasy.Saving.SaveGameService.IsRestoring) return false;
            var manager = DialogueManager.Instance;
            if (EasternFantasy.UI.MapTutorialUI.IsRunning || manager == null || manager.IsDialogueActive)
                return false;
            PlayDialogue();
            return true;
        }
        private IEnumerator WaitForDialogue(PlayerMovement2D movement)
        {
            // Only busy dialogue/guide ownership is deferred. Yield before assigning a queue handle.
            yield return null;
            while (movement != null && movement.isActiveAndEnabled)
            {
                if (playOnlyOnce && sequence != null && playedSequences.Contains(sequence)) break;
                if (TryStartEntryDialogue()) break;
                yield return null;
            }
            pendingEntryDialogue = null;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (finishEntryAfterExit || !other.CompareTag("Player") || pendingEntryDialogue == null) return;
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
