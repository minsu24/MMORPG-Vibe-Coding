using System;
using UnityEngine;

namespace EasternFantasy.Dialogue
{
    public enum DialogueSpeakerSide
    {
        Player,
        Partner
    }

    [Serializable]
    public sealed class DialogueLine
    {
        [SerializeField] private DialogueSpeakerSide speaker;
        [SerializeField, TextArea(2, 6)] private string text;

        public DialogueSpeakerSide Speaker => speaker;
        public string Text => text;
    }

    [CreateAssetMenu(
        fileName = "DialogueSequence",
        menuName = "Eastern Fantasy/Dialogue/Sequence")]
    public sealed class DialogueSequence : ScriptableObject
    {
        [Header("Left and Right Characters")]
        [SerializeField] private DialogueCharacter playerCharacter;
        [SerializeField] private DialogueCharacter partnerCharacter;

        [Header("Lines are played from top to bottom")]
        [SerializeField] private DialogueLine[] lines = Array.Empty<DialogueLine>();

        public DialogueCharacter PlayerCharacter => playerCharacter;
        public DialogueCharacter PartnerCharacter => partnerCharacter;
        public DialogueLine[] Lines => lines;
        public bool HasLines => lines != null && lines.Length > 0;
    }
}
