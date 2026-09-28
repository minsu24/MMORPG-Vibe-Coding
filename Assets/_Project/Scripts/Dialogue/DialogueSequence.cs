using System;
using EasternFantasy.CharacterSelection;
using UnityEngine;

namespace EasternFantasy.Dialogue
{
    public enum DialogueSpeakerSide
    {
        Player,
        Partner
    }

    [Serializable]
    public sealed class ClassDialogueText
    {
        [SerializeField] private CharacterClassId characterClass;
        [SerializeField, TextArea(2, 6)] private string text;

        public CharacterClassId CharacterClass => characterClass;
        public string Text => text;
    }

    [Serializable]
    public sealed class DialogueLine
    {
        [SerializeField] private DialogueSpeakerSide speaker;
        [Tooltip("Used when the selected class has no matching override below.")]
        [SerializeField, TextArea(2, 6)] private string text;
        [Tooltip("Optional dialogue text used only for the matching player class.")]
        [SerializeField] private ClassDialogueText[] classOverrides =
            Array.Empty<ClassDialogueText>();

        public DialogueSpeakerSide Speaker => speaker;
        public string Text => text;

        public string GetText(CharacterClassId characterClass)
        {
            if (classOverrides == null)
                return text;

            foreach (ClassDialogueText classOverride in classOverrides)
            {
                if (classOverride != null &&
                    classOverride.CharacterClass == characterClass)
                    return classOverride.Text;
            }

            return text;
        }
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
