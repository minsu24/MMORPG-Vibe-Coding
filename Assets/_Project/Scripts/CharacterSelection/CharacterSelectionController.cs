using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.CharacterSelection
{
    [DisallowMultipleComponent]
    public sealed class CharacterSelectionController : MonoBehaviour
    {
        [SerializeField] private CharacterSelectionCard[] cards;
        [SerializeField] private TMP_Text classNameText;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private string gameplaySceneName = "MovementPrototype";
        [SerializeField] private string mainMenuSceneName = "MainScreen";

        private void Start()
        {
            foreach (CharacterSelectionCard card in cards)
                if (card != null)
                    card.Initialize(this);

            if (cards.Length > 0 && cards[0] != null)
                ShowDetails(cards[0].Definition);
        }

        public void ShowDetails(CharacterClassDefinition definition)
        {
            if (definition == null)
                return;

            classNameText.text = definition.DisplayName;
            roleText.text = definition.CombatRole;
            descriptionText.text = definition.Description;
            statsText.text =
                $"공격력  {BuildStat(definition.Attack)}    " +
                $"방어력  {BuildStat(definition.Defense)}    " +
                $"기동성  {BuildStat(definition.Mobility)}    " +
                $"사거리  {BuildStat(definition.Range)}";

            bool created = CharacterSelectionState.HasCharacter(definition.ClassId);
            statusText.text = definition.Playable
                ? created ? "생성된 캐릭터 · 선택하면 바로 입장" : "새 캐릭터 · 선택하면 바로 생성 및 입장"
                : "준비 중 · 캐릭터 전투 프리팹이 아직 구현되지 않았습니다";
        }

        public void SelectClass(CharacterClassDefinition definition)
        {
            if (definition == null)
                return;

            ShowDetails(definition);
            if (!definition.Playable)
                return;

            CharacterSelectionState.CreateOrSelect(definition.ClassId);
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private static string BuildStat(int value)
        {
            value = Mathf.Clamp(value, 1, 5);
            return new string('●', value) + new string('○', 5 - value);
        }
    }
}
