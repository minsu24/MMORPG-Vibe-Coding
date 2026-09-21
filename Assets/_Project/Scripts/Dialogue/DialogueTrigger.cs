using UnityEngine;

namespace EasternFantasy.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueSequence sequence;
        [SerializeField] private bool startOnPlayerEnter;
        [SerializeField] private bool playOnlyOnce;

        private bool hasPlayed;

        // Call this from an NPC interaction script, UnityEvent, button, or cutscene.
        public void PlayDialogue()
        {
            if (playOnlyOnce && hasPlayed)
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

            hasPlayed = DialogueManager.Instance.StartDialogue(sequence);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (startOnPlayerEnter && other.CompareTag("Player"))
                PlayDialogue();
        }
    }
}
