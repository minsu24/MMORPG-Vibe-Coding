using TMPro;
using UnityEngine;

namespace EasternFantasy.Dialogue
{
    public sealed partial class DialogueManager
    {
        [Header("Dialogue writing sound")]
        [SerializeField] private AudioClip typingSound;
        [SerializeField, Range(0f, 1f)] private float typingSoundVolume = .16f;

        private AudioSource typingAudio;
        private int audibleCharacterCount;
        private int charactersUntilSound;
        private float nextTypingSoundAt;

        private void ResetTypingSound()
        {
            audibleCharacterCount = 0;
            charactersUntilSound = Random.Range(2, 4);
            nextTypingSoundAt = float.NegativeInfinity;
            if (typingSound == null)
                typingSound = Resources.Load<AudioClip>("Audio/DialogueWritingTick");
        }

        private void PlayTypingSound(int from, int to)
        {
            bool play = false;
            for (int i = from; i < to; i++)
            {
                TMP_CharacterInfo character = dialogueText.textInfo.characterInfo[i];
                // Count rendered letters and digits, excluding spaces, punctuation,
                // rich-text tags, and inline reward sprites.
                if (character.elementType != TMP_TextElementType.Character
                    || !char.IsLetterOrDigit(character.character)) continue;
                audibleCharacterCount++;
                if (audibleCharacterCount < charactersUntilSound) continue;
                audibleCharacterCount = 0;
                charactersUntilSound = Random.Range(2, 4);
                play = true;
            }

            // One short stroke per update, even when a slow frame reveals many
            // characters. Completing a line instantly bypasses this method.
            if (!play || typingSound == null || typingSoundVolume <= 0f
                || Time.unscaledTime < nextTypingSoundAt) return;
            if (typingAudio == null)
            {
                var child = new GameObject("Dialogue Writing Audio");
                child.transform.SetParent(transform, false);
                typingAudio = child.AddComponent<AudioSource>();
                typingAudio.playOnAwake = false;
                typingAudio.loop = false;
                typingAudio.spatialBlend = 0f;
            }
            typingAudio.clip = typingSound;
            typingAudio.volume = typingSoundVolume;
            typingAudio.pitch = Random.Range(.96f, 1.04f);
            typingAudio.Play();
            nextTypingSoundAt = Time.unscaledTime + .065f;
        }

        private void StopTypingSound()
        {
            if (typingAudio != null) typingAudio.Stop();
            audibleCharacterCount = 0;
        }
    }
}
