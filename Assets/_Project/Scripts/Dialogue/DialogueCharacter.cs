using UnityEngine;

namespace EasternFantasy.Dialogue
{
    [CreateAssetMenu(
        fileName = "DialogueCharacter",
        menuName = "Eastern Fantasy/Dialogue/Character")]
    public sealed class DialogueCharacter : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private Sprite portrait;

        public string DisplayName => displayName;
        public Sprite Portrait => portrait;
    }
}
