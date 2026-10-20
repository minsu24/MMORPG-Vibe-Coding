using System;
using EasternFantasy.Advancement;
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
        [Tooltip("Optional text that takes priority over class overrides after first advancement.")]
        [SerializeField, TextArea(2, 6)] private string firstAdvancementText;

        public DialogueSpeakerSide Speaker => speaker;
        public string Text => text;

        public string GetText(CharacterClassId? characterClass,
            AdvancementRank advancementRank = AdvancementRank.Base)
        {
            if (advancementRank >= AdvancementRank.First
                && !string.IsNullOrEmpty(firstAdvancementText))
                return firstAdvancementText;

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

        [Tooltip("Experience granted once per game session, after the final line is completed.")]
        [SerializeField, Min(0)] private int completionExperience;

        public int CompletionExperience => completionExperience;
        public DialogueCharacter PlayerCharacter => playerCharacter;
        public DialogueCharacter PartnerCharacter => partnerCharacter;
        public DialogueLine[] Lines => lines;
        public bool HasLines => lines != null && lines.Length > 0;
    }
}
